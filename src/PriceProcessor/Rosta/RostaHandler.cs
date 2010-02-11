using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Properties;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Rosta
{
	public class Plan
	{
		public uint PriceItemId { get; set; }
		public DateTime PlanedOn { get; set; }
		public string Key { get; set; }
	}

	public class RostaHandler : AbstractHandler
	{
		private DateTime _lastSync;

		private readonly List<Plan> _plans = new List<Plan>();
		private readonly uint _priceId;
		private readonly IDownloader _downloader;

		public RostaHandler(uint priceId, IDownloader downloader)
		{
			_priceId = priceId;
			_downloader = downloader;
		}

		public new int SleepTime
		{
			get { return base.SleepTime; }
			set { base.SleepTime = value; }
		}

		protected override void ProcessData()
		{
			DoSyncIfNeeded();
			var plan = GetNextPlannedUpdate();

			if (SystemTime.Now() < plan.PlanedOn)
				return;


			Process(plan.PriceItemId, plan.Key);

			MovePlan(plan);
		}

		private void DoSyncIfNeeded()
		{
			if (_lastSync + 5.Minute() > SystemTime.Now())
				return;

			var exists = GetExistsPlans();

			_plans
				.Where(p => exists.All(id => p.PriceItemId != id.PriceItemId))
				.ToList()
				.Each(p => _plans.Remove(p));

			exists
				.Where(e => _plans.All(p => p.PriceItemId != e.PriceItemId))
				.ToList()
				.Each(e => _plans.Add(e));

			var clients = GetNotConfiguredClients();
			clients.Each(InitNewPlan);

			_lastSync = SystemTime.Now();
		}

		private void InitNewPlan(uint clientId)
		{
			var key = GetKey(clientId);
			if (String.IsNullOrEmpty(key))
				return;
			var plan = new Plan {
				Key = key,
				PlanedOn = SystemTime.Now() + new Random().NextDouble().Hour()
			};
			CreatePriceItemForNewPlan(clientId, plan);
			_plans.Add(plan);
		}

		private string GetKey(uint clientId)
		{
			using(var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				connection.Open();
				var command = new MySqlCommand(@"SELECT RostaUin
FROM logs.SpyInfo S
  join Usersettings.OsUserAccessRight ouar on ouar.RowId = s.UserId
where RostaUin is not null and ouar.ClientCode = ?ClientCode
order by logtime desc
limit 1;", connection);
				command.Parameters.AddWithValue("?ClientCode", clientId);
				var result = command.ExecuteScalar();
				if (result is DBNull)
					return null;
				return RostaDecoder.GetKey(Convert.ToString(result));
			}
		}

		private void CreatePriceItemForNewPlan(uint clientId, Plan plan)
		{
			using (var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				connection.Open();
				var readCommand = new MySqlCommand(@"
select pi.FormRuleId, pi.SourceId
from Usersettings.PriceItems pi
	join Usersettings.PricesCosts pc on pc.PriceItemId = pi.Id
where pc.BaseCost = 1 and pc.PriceCode = ?PriceCode
", connection);
				readCommand.Parameters.AddWithValue("?PriceCode", _priceId);
				uint formRuleId = 0;
				uint sourceId = 0;
				using(var reader = readCommand.ExecuteReader())
				{
					if (reader.Read())
					{
						formRuleId = reader.GetUInt32("FormRuleId");
						sourceId = reader.GetUInt32("SourceId");
					}
				}

				var transaction = connection.BeginTransaction();
				try
				{
					var command = new MySqlCommand(@"

insert into Usersettings.PriceItems(FormRuleId, SourceId) values (?FormRuleId, ?SourceId);
set @priceItemId = LAST_INSERT_ID();
insert into Usersettings.PricesCosts(PriceCode, PriceItemId, Enabled, AgencyEnabled, CostName) values (?PriceCode, @priceItemId, 1, 1, ?Key);
set @costCode = LAST_INSERT_ID();
insert into Farm.CostFormRules(CostCode, FieldName) values (@CostCode, 'F3');

update Usersettings.Intersection i
set i.FirmClientCode = ?Key, i.CostCode = @costCode
where PriceCode = ?PriceCode and (i.ClientCode = ?ClientId or i.clientcode in (
select IncludeClientCode
from Usersettings.IncludeRegulation ir
where ir.PrimaryClientCode = ?ClientId));

select @priceItemId;", connection);
					command.Parameters.AddWithValue("?FormRuleId", formRuleId);
					command.Parameters.AddWithValue("?SourceId", sourceId);
					command.Parameters.AddWithValue("?PriceCode", _priceId);
					command.Parameters.AddWithValue("?ClientId", clientId);
					command.Parameters.AddWithValue("?Key", plan.Key);
					plan.PriceItemId = Convert.ToUInt32(command.ExecuteScalar());
					transaction.Commit();
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		private List<Plan> GetExistsPlans()
		{
			var plans = new List<Plan>();
			using(var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				var adapter = new MySqlDataAdapter(@"
SELECT pc.PriceItemId, pc.CostName
FROM usersettings.Intersection I
	join Usersettings.RetClientsSet rcs on rcs.ClientCode = i.ClientCode
	join Usersettings.PricesCosts pc on pc.CostCode = i.CostCode
	left join Usersettings.IncludeRegulation ir on ir.IncludeClientCode = i.ClientCode
where i.pricecode = ?PriceCode and i.InvisibleOnClient = 0 and rcs.ServiceClient = 0 and ir.Id is null and i.FirmClientCode is not null and i.FirmClientCode <> ''
group by pc.PriceItemId;", connection);
				adapter.SelectCommand.Parameters.AddWithValue("?PriceCode", _priceId);
				var data = new DataSet();
				adapter.Fill(data);
				foreach (DataRow row in data.Tables[0].Rows)
				{
					plans.Add(new Plan {
						PriceItemId = Convert.ToUInt32(row["PriceItemId"]),
						Key = Convert.ToString(row["CostName"]),
					});
				}
			}
			return plans;
		}

		private List<uint> GetNotConfiguredClients()
		{
			var clients = new List<uint>();
			using(var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				var adapter = new MySqlDataAdapter(@"
SELECT i.ClientCode
FROM usersettings.Intersection I
	join Usersettings.RetClientsSet rcs on rcs.ClientCode = i.ClientCode
	left join Usersettings.IncludeRegulation ir on ir.IncludeClientCode = i.ClientCode
where i.pricecode = ?PriceId and i.InvisibleOnClient = 0 and rcs.ServiceClient = 0 and ir.Id is null and (i.FirmClientCode is null or i.FirmClientCode = '')
group by i.ClientCode;", connection);
				adapter.SelectCommand.Parameters.AddWithValue("?PriceId", _priceId);
				var data = new DataSet();
				adapter.Fill(data);
				foreach (DataRow row in data.Tables[0].Rows)
					clients.Add(Convert.ToUInt32(row[0]));
			}
			return clients;
		}

		private void MovePlan(Plan plan)
		{
			plan.PlanedOn = SystemTime.Now() + 2.Hour() + new Random().NextDouble().Hour();
		}

		private Plan GetNextPlannedUpdate()
		{
			var minPlannedOn = _plans.Min(p => p.PlanedOn);
			return _plans.Where(p => p.PlanedOn == minPlannedOn).First();
		}

		public void Process(uint priceItemId, string key)
		{
			var price = Path.Combine(Settings.Default.TempPath, "price");
			var producers = Path.Combine(Settings.Default.TempPath, "producers");
			var ex = Path.Combine(Settings.Default.TempPath, "ex");
			try
			{
				if (!Directory.Exists(Settings.Default.TempPath))
					Directory.CreateDirectory(Settings.Default.TempPath);

				if (File.Exists(price))
					File.Delete(price);

				if (File.Exists(producers))
					File.Delete(producers);

				if (File.Exists(ex))
					File.Delete(ex);

				_downloader.DownloadPrice(key, price, producers, ex);
				using (var connection = new MySqlConnection(Literals.ConnectionString()))
				{
					var data = PricesValidator.LoadFormRules(priceItemId, connection);
					var parser = new FakeRostaParser(price, producers, ex, connection, data);
					connection.Close();
					parser.Formalize();
					if (connection.State == ConnectionState.Closed)
						connection.Open();
					var command = new MySqlCommand("update usersettings.priceitems set PriceDate = now() where id = ?id", connection);
					command.Parameters.AddWithValue("?id", priceItemId);
					command.ExecuteNonQuery();
				}
			}
			finally
			{
				if (File.Exists(price))
					File.Delete(price);
				if (File.Exists(producers))
					File.Delete(producers);
				if (File.Exists(ex))
					File.Delete(ex);
			}
		}
	}
}