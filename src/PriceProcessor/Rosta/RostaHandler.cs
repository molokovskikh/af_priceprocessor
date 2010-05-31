﻿using System;
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
		public string Hwinfo { get; set; }

		public Plan(uint priceItemId, string key, string hwinfo)
		{
			PriceItemId = priceItemId;
			Key = key;
			Hwinfo = hwinfo;
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
		public string Hwinfo;
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

			Process(plan);

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

/*			временно не работает тк в SpyInfo нет информации о процессоре и мамке может быть не заработает никогда
			var clients = GetNotConfiguredClients();
			clients.Each(c => InitNewPlan(c, GetKey(c)));
*/

			var partialConfigured = GetPartialConfiguredClients();
			partialConfigured.Each(p => InitNewPlan(p));

			_lastSync = SystemTime.Now();
		}

		private void InitNewPlan(ToConfigure configure)
		{
			if (String.IsNullOrEmpty(configure.Key) || String.IsNullOrEmpty(configure.Hwinfo))
				return;

			var existPlan = _plans.FirstOrDefault(p => p.Key == configure.Key);
			if (existPlan != null)
				MySqlUtils.InTransaction(c => UpdateClient(c, configure.ClientId, existPlan.PriceItemId, existPlan.Key, existPlan.Hwinfo));
			else
				_plans.Add(CreateNewPlan(configure));
		}

		private void UpdateClient(MySqlConnection connection, uint clientId, uint priceItemId, string key, string hwinfo)
		{
			var motherboard = "";
			var parts = hwinfo.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			var cpuid = parts[0];
			if (parts.Length > 1)
				motherboard = parts[1];

			var command = new MySqlCommand(@"
select CostCode
into @costCode
from usersettings.PricesCosts
where PriceItemId = ?PriceItemId
limit 1;

update Usersettings.Intersection i
set i.FirmClientCode = ?Key, i.FirmClientCode2 = ?cpuid, i.FirmClientCode3 = ?Motherboard, i.CostCode = @costCode
where PriceCode = ?PriceId and (i.ClientCode = ?ClientId or i.clientcode in (
select IncludeClientCode
from Usersettings.IncludeRegulation ir
where ir.PrimaryClientCode = ?ClientId));", connection);
			command.Parameters.AddWithValue("?ClientId", clientId);
			command.Parameters.AddWithValue("?PriceItemId", priceItemId);
			command.Parameters.AddWithValue("?PriceId", _priceId);
			command.Parameters.AddWithValue("?Key", key);
			command.Parameters.AddWithValue("?CpuId", cpuid);
			command.Parameters.AddWithValue("?Motherboard", motherboard);
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

		private Plan CreateNewPlan(ToConfigure configure)
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
				command.Parameters.AddWithValue("?ClientId", configure.ClientId);
				command.Parameters.AddWithValue("?Key", configure.Key);
				priceItemId = Convert.ToUInt32(command.ExecuteScalar());
				UpdateClient(c, configure.ClientId, priceItemId, configure.Key, configure.Hwinfo);
			});
			return new Plan(priceItemId, configure.Key, configure.Hwinfo);
		}

		private List<Plan> GetExistsPlans()
		{
			var plans = new List<Plan>();
			using(var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				var adapter = new MySqlDataAdapter(@"
SELECT pc.PriceItemId, pc.CostName, i.FirmClientCode, i.FirmClientCode, i.FirmClientCode2, i.FirmClientCode3
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
	and i.FirmClientCode2 is not null
	and i.FirmClientCode2 <> ''
	and pc.BaseCost = 0
group by pc.PriceItemId;", connection);
				adapter.SelectCommand.Parameters.AddWithValue("?PriceId", _priceId);
				var data = new DataSet();
				adapter.Fill(data);
				foreach (DataRow row in data.Tables[0].Rows)
				{
					plans.Add(new Plan(
						Convert.ToUInt32(row["PriceItemId"]),
						Convert.ToString(row["FirmClientCode"]),
						Convert.ToString(row["FirmClientCode2"]) + "\r\n" + Convert.ToString(row["FirmClientCode3"])
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
SELECT i.ClientCode, i.FirmClientCode, i.FirmClientCode2, i.FirmClientCode3
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
	and i.FirmClientCode2 is not null
	and i.FirmClientCode2 <> ''
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
						Hwinfo = Convert.ToString(row["FirmClientCode2"]) + "\r\n" + row["FirmClientCode3"]
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
			if (_plans.Count == 0)
				return null;
			var minPlannedOn = _plans.Min(p => p.PlanedOn);
			return _plans.Where(p => p.PlanedOn == minPlannedOn).FirstOrDefault();
		}

		public void Process(Plan plan)
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

				_downloader.DownloadPrice(plan.Key, plan.Hwinfo, price, producers, ex);
				using (var connection = new MySqlConnection(Literals.ConnectionString()))
				{
					var data = PricesValidator.LoadFormRules(plan.PriceItemId, connection);
					var parser = new FakeRostaParser(price, producers, ex, connection, data);
					connection.Close();
					parser.Formalize();
					if (connection.State == ConnectionState.Closed)
						connection.Open();
					var command = new MySqlCommand("update usersettings.priceitems set PriceDate = now() where id = ?id", connection);
					command.Parameters.AddWithValue("?id", plan.PriceItemId);
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