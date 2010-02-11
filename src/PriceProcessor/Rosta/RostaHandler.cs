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

		public Plan(uint priceItemId, string key)
		{
			PriceItemId = priceItemId;
			Key = key;
			
			PlanedOn = SystemTime.Now() + new Random().NextDouble().Hour();
		}

		public void PlanNextUpdate()
		{
			var date =  SystemTime.Today() + new TimeSpan(1, 6, 10, 0);
			date += (new Random().NextDouble()*1.5).Hour();
			if (date.DayOfWeek == DayOfWeek.Sunday)
				date += 1.Day();
			else if (date.DayOfWeek == DayOfWeek.Saturday)
				date += 2.Day();
			PlanedOn =  date;
		}
	}

	public class ToConfigure
	{
		public uint ClientId { get; set; }
		public string Key;
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

			if (plan == null)
				return;

			if (SystemTime.Now() < plan.PlanedOn)
				return;

			Process(plan.PriceItemId, plan.Key);

			plan.PlanNextUpdate();
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
			clients.Each(c => InitNewPlan(c, GetKey(c)));

			var partialConfigured = GetPartialConfiguredClients();
			partialConfigured.Each(p => InitNewPlan(p.ClientId, p.Key));

			_lastSync = SystemTime.Now();
		}

		private void InitNewPlan(uint clientId, string key)
		{
			if (String.IsNullOrEmpty(key))
				return;

			var existPlan = _plans.FirstOrDefault(p => p.Key == key);
			if (existPlan != null)
				MySqlUtils.InTransaction(c => UpdateClient(c, clientId, existPlan.PriceItemId, existPlan.Key));
			else
				_plans.Add(CreateNewPlan(clientId, key));
		}

		private void UpdateClient(MySqlConnection connection, uint clientId, uint priceItemId, string key)
		{
			var command = new MySqlCommand(@"
select CostCode
into @costCode
from usersettings.PricesCosts
where PriceItemId = ?PriceItemId
limit 1;

update Usersettings.Intersection i
set i.FirmClientCode = ?Key, i.CostCode = @costCode
where PriceCode = ?PriceId and (i.ClientCode = ?ClientId or i.clientcode in (
select IncludeClientCode
from Usersettings.IncludeRegulation ir
where ir.PrimaryClientCode = ?ClientId));", connection);
			command.Parameters.AddWithValue("?ClientId", clientId);
			command.Parameters.AddWithValue("?PriceItemId", priceItemId);
			command.Parameters.AddWithValue("?PriceId", _priceId);
			command.Parameters.AddWithValue("?Key", key);
			command.ExecuteNonQuery();
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

		private Plan CreateNewPlan(uint clientId, string key)
		{
			uint priceItemId = 0;
			MySqlUtils.InTransaction(c => {
				var readCommand = new MySqlCommand(@"
select pi.FormRuleId, pi.SourceId
from Usersettings.PriceItems pi
	join Usersettings.PricesCosts pc on pc.PriceItemId = pi.Id
where pc.BaseCost = 1 and pc.PriceCode = ?PriceCode", c);
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

				var command = new MySqlCommand(@"
insert into Usersettings.PriceItems(FormRuleId, SourceId) values (?FormRuleId, ?SourceId);
set @priceItemId = LAST_INSERT_ID();

insert into Usersettings.PricesCosts(PriceCode, PriceItemId, Enabled, AgencyEnabled, CostName) values (?PriceCode, @priceItemId, 1, 1, ?Key);
set @costCode = LAST_INSERT_ID();

insert into Farm.CostFormRules(CostCode, FieldName) values (@CostCode, 'F3');
select @priceItemId;", c);
				command.Parameters.AddWithValue("?FormRuleId", formRuleId);
				command.Parameters.AddWithValue("?SourceId", sourceId);
				command.Parameters.AddWithValue("?PriceCode", _priceId);
				command.Parameters.AddWithValue("?ClientId", clientId);
				command.Parameters.AddWithValue("?Key", key);
				priceItemId = Convert.ToUInt32(command.ExecuteScalar());
				UpdateClient(c, clientId, priceItemId, key);
			});
			return new Plan(priceItemId, key);
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
where i.pricecode = ?PriceId
	and i.InvisibleOnClient = 0
	and rcs.ServiceClient = 0
	and ir.Id is null
	and i.FirmClientCode is not null
	and i.FirmClientCode <> ''
	and pc.BaseCost = 0
group by pc.PriceItemId;", connection);
				adapter.SelectCommand.Parameters.AddWithValue("?PriceId", _priceId);
				var data = new DataSet();
				adapter.Fill(data);
				foreach (DataRow row in data.Tables[0].Rows)
				{
					plans.Add(new Plan(
						Convert.ToUInt32(row["PriceItemId"]),
						Convert.ToString(row["CostName"])
					));
				}
			}
			return plans;
		}

		private List<ToConfigure> GetPartialConfiguredClients()
		{
			var toConfigure = new List<ToConfigure>();
			using(var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				var adapter = new MySqlDataAdapter(@"
SELECT i.ClientCode, i.FirmClientCode
FROM usersettings.Intersection I
	join Usersettings.RetClientsSet rcs on rcs.ClientCode = i.ClientCode
	join Usersettings.PricesCosts pc on pc.CostCode = i.CostCode
	left join Usersettings.IncludeRegulation ir on ir.IncludeClientCode = i.ClientCode
where i.pricecode = ?PriceId
	and i.InvisibleOnClient = 0
	and rcs.ServiceClient = 0
	and ir.Id is null
	and i.FirmClientCode is not null
	and i.FirmClientCode <> ''
	and pc.BaseCost = 1
group by i.ClientCode", connection);
				adapter.SelectCommand.Parameters.AddWithValue("?PriceId", _priceId);
				var data = new DataSet();
				adapter.Fill(data);
				foreach (DataRow row in data.Tables[0].Rows)
				{
					toConfigure.Add(new ToConfigure {
						ClientId = Convert.ToUInt32(row["ClientCode"]),
						Key = Convert.ToString(row["FirmClientCode"]),
					});
				}
			}
			return toConfigure;
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
where i.pricecode = ?PriceId 
	and i.InvisibleOnClient = 0 
	and rcs.ServiceClient = 0 
	and ir.Id is null 
	and (i.FirmClientCode is null or i.FirmClientCode = '')
group by i.ClientCode;", connection);
				adapter.SelectCommand.Parameters.AddWithValue("?PriceId", _priceId);
				var data = new DataSet();
				adapter.Fill(data);
				foreach (DataRow row in data.Tables[0].Rows)
					clients.Add(Convert.ToUInt32(row[0]));
			}
			return clients;
		}

		private Plan GetNextPlannedUpdate()
		{
			var minPlannedOn = _plans.Min(p => p.PlanedOn);
			return _plans.Where(p => p.PlanedOn == minPlannedOn).FirstOrDefault();
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