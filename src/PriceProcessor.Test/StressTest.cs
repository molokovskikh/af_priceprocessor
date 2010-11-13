using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Properties;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	public class SimpleLayout1 : LayoutSkeleton
	{
		// Methods
		public SimpleLayout1()
		{
			this.IgnoresException = true;
		}

		public override void ActivateOptions()
		{
		}

		public override void Format(TextWriter writer, LoggingEvent loggingEvent)
		{
			if (loggingEvent == null)
			{
				throw new ArgumentNullException("loggingEvent");
			}
			writer.Write(DateTime.Now.ToString("mm:ss.fff") + ":");
			writer.Write(loggingEvent.Level.DisplayName);
			writer.Write(" - ");
			loggingEvent.WriteRenderedMessage(writer);
			writer.WriteLine();
		}
	}


	[TestFixture, Ignore("Стресс тест")]
	public class StressTest
	{
		private ILog _logger = LogManager.GetLogger(typeof (BasePriceParser));

		[Test]
		public void t1()
		{
			var fileContent = @"

";
			var result = String.Empty;
			var i = 0;
			var decodedContent = "013";

			while (i < decodedContent.Length-2)
			{
				result += Convert.ToChar(
					Convert.ToByte(
						String.Format(
							"{0}{1}{2}",
							decodedContent[i],
							decodedContent[i+1],
							decodedContent[i+2]
						)
					)
				);
				i += 3;
			}
			//return encodedValue;

/*			string decodedContent;
			var result = String.Empty;
			var i = 0;

			while (i < decodedContent.Length-2)
			{
				result += Convert.ToChar(
					Convert.ToByte(
						String.Format(
							"{0}{1}{2}",
							decodedContent[i],
							decodedContent[i+1],
							decodedContent[i+2]
						)
					)
				);
				i += 3;
			}

			return result;*/

		}

		[Test]
		public void t()
		{
			Console.WriteLine("end {0}", DateTime.Now);
			Prepare();
			Console.WriteLine("end {0}", DateTime.Now);
		}

		[Test]
		public void Load_data()
		{
			Console.WriteLine("begin {0}", DateTime.Now);
			using (var connection = new MySqlConnection("server=sql.analit.net;username=Kvasov; password=ghjgtkkth;port=3308;database=farm; pooling=true; Convert Zero Datetime=true; Allow User Variables=true;"))
			{
				var data = new DataSet();

				var adapter = new MySqlDataAdapter("SELECT * FROM farm.CoreCosts LIMIT 0", connection);
				adapter.Fill(data, "CoreCosts");
				var costs = data.Tables["CoreCosts"];

				adapter.SelectCommand.CommandText = "SELECT * FROM farm.Core0 WHERE PriceCode=3779 order by Id";
				adapter.Fill(data, "ExistsCore");
				var core = data.Tables["ExistsCore"];

				var  costsCopy = costs.Clone();
				connection.Open();
				var command = new MySqlCommand(@"
SELECT
  CoreCosts.* 
FROM 
  farm.Core0, 
  farm.CoreCosts,
  usersettings.pricescosts
WHERE 
    Core0.PriceCode = 3779
and pricescosts.PriceCode = 3779
and CoreCosts.Core_Id = Core0.id
and CoreCosts.PC_CostCode = pricescosts.CostCode
order by Core0.Id;", connection);
				var dataAdapter = new MySqlDataAdapter(command);
				
				costsCopy.TableName = "ExistsCoreCosts";
				costsCopy.Columns["PC_CostCode"].DataType = typeof(long);
				data.Tables.Add(costsCopy);

				var relation = new DataRelation("ExistsCoreToCosts", core.Columns["Id"], costsCopy.Columns["Core_Id"]);
				data.Relations.Add(relation);


				dataAdapter.Fill(costsCopy);
			}
			Console.WriteLine("end {0}", DateTime.Now);
		}

		public class Cost
		{
			public ulong CoreId;
			public uint CostCode;
			public float CostValue;
		}

		[Test]
		public void Load_self()
		{
			var stopwatch = Stopwatch.StartNew();
			Console.WriteLine("begin {0}", DateTime.Now);
			using (var connection = new MySqlConnection("server=sql.analit.net;username=Kvasov; password=ghjgtkkth;port=3308;database=farm; pooling=true; Convert Zero Datetime=true; Allow User Variables=true;Logging=true;Use Usage Advisor=true"))
			{
				connection.Open();
				Console.WriteLine("open connection");
				var command = new MySqlCommand(@"
SELECT
  Core_id,
PC_COstCode,
Cost
FROM 
  farm.Core0, 
  farm.CoreCosts,
  usersettings.pricescosts
WHERE 
    Core0.PriceCode = 3779
and pricescosts.PriceCode = 3779
and CoreCosts.Core_Id = Core0.id
and CoreCosts.PC_CostCode = pricescosts.CostCode
order by Core0.Id;", connection);

				var costs = new System.Collections.Generic.List<Cost>(/*1834973*/);
				using(var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
				{
					Console.WriteLine("reader opened {0}", DateTime.Now);
					while (reader.Read())
					{
						costs.Add(new Cost {
							CoreId = reader.GetUInt64(0),
							CostCode = reader.GetUInt32(1),
							CostValue = reader.GetFloat(2),
						});
					}
				}
				Console.WriteLine(costs.Count);
			}
			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
			Console.WriteLine("end {0}", DateTime.Now);
		}

		public void Prepare()
		{
			var priceItemId = 599;
			var MyConn =
				new MySqlConnection(
					"server=sql.analit.net;username=Kvasov; password=ghjgtkkth;port=3306;database=farm; pooling=true; Convert Zero Datetime=true; Allow User Variables=true;");
			MyConn.Open();

			var dsMyDB = new DataSet();
			_logger.Debug("начало Prepare");
			var daForbidden = new MySqlDataAdapter(
				String.Format("SELECT PriceCode, LOWER(Forbidden) AS Forbidden FROM farm.Forbidden WHERE PriceCode={0}", 3779), MyConn);
			daForbidden.Fill(dsMyDB, "Forbidden");
			var dtForbidden = dsMyDB.Tables["Forbidden"];
			_logger.Debug("загрузили Forbidden");

			var daSynonym = new MySqlDataAdapter(
				String.Format(@"
SELECT 
  Synonym.SynonymCode, 
  LOWER(Synonym.Synonym) AS Synonym, 
  Synonym.ProductId, 
  Synonym.Junk,
  products.CatalogId
FROM 
  farm.Synonym, 
  catalogs.products 
WHERE 
    (Synonym.PriceCode={0}) 
and (products.Id = Synonym.ProductId)
"
				, 
				2446), MyConn);
			daSynonym.Fill(dsMyDB, "Synonym");
			var dtSynonym = dsMyDB.Tables["Synonym"];
			_logger.Debug("загрузили Synonym");

			var daAssortment = new MySqlDataAdapter("SELECT Id, CatalogId, ProducerId, Checked FROM catalogs.Assortment ", MyConn);
			var excludesBuilder  = new MySqlCommandBuilder(daAssortment);
			daAssortment.InsertCommand = excludesBuilder.GetInsertCommand();
			daAssortment.InsertCommand.CommandTimeout = 0;
			daAssortment.Fill(dsMyDB, "Assortment");
			var dtAssortment = dsMyDB.Tables["Assortment"];
			_logger.Debug("загрузили Assortment");
			dtAssortment.PrimaryKey = new[] { dtAssortment.Columns["CatalogId"], dtAssortment.Columns["ProducerId"] };
			_logger.Debug("построили индекс по Assortment");

			var daExcludes = new MySqlDataAdapter(
				String.Format("SELECT Id, CatalogId, ProducerSynonymId, PriceCode, OriginalSynonymId FROM farm.Excludes where PriceCode = {0}", 2446), MyConn);
			var cbExcludes = new MySqlCommandBuilder(daExcludes);
			daExcludes.InsertCommand = cbExcludes.GetInsertCommand();
			daExcludes.InsertCommand.CommandTimeout = 0;
			daExcludes.Fill(dsMyDB, "Excludes");
			var dtExcludes = dsMyDB.Tables["Excludes"];
			_logger.Debug("загрузили Excludes");
			dtExcludes.Constraints.Add("ProducerSynonymKey", new[] { dtExcludes.Columns["CatalogId"], dtExcludes.Columns["ProducerSynonymId"] }, false);
			_logger.Debug("построили индекс по Excludes");

			var assortmentSearchWatch = new Stopwatch();
			var assortmentSearchCount = 0;
			var excludesSearchWatch = new Stopwatch();
			var excludesSearchCount = 0;


			var daSynonymFirmCr = new MySqlDataAdapter(
				String.Format(@"
SELECT
  SynonymFirmCrCode,
  CodeFirmCr,
  LOWER(Synonym) AS Synonym,
  (aps.ProducerSynonymId is not null) as IsAutomatic
FROM
  farm.SynonymFirmCr
  left join farm.AutomaticProducerSynonyms aps on aps.ProducerSynonymId = SynonymFirmCr.SynonymFirmCrCode
WHERE SynonymFirmCr.PriceCode={0}
"
				, 
				2446), MyConn);
			daSynonymFirmCr.Fill(dsMyDB, "SynonymFirmCr");
			daSynonymFirmCr.InsertCommand = new MySqlCommand(@"
insert into farm.SynonymFirmCr (PriceCode, CodeFirmCr, Synonym) values (?PriceCode, null, ?OriginalSynonym);
set @LastSynonymFirmCrCode = last_insert_id();
insert farm.UsedSynonymFirmCrLogs (SynonymFirmCrCode) values (@LastSynonymFirmCrCode);
insert into farm.AutomaticProducerSynonyms (ProducerSynonymId) values (@LastSynonymFirmCrCode);
select @LastSynonymFirmCrCode;");
			daSynonymFirmCr.InsertCommand.Parameters.Add("?PriceCode", MySqlDbType.Int64);
			daSynonymFirmCr.InsertCommand.Parameters.Add("?OriginalSynonym", MySqlDbType.String);
			daSynonymFirmCr.InsertCommand.Connection = MyConn;
			var dtSynonymFirmCr = dsMyDB.Tables["SynonymFirmCr"];
			dtSynonymFirmCr.Columns.Add("OriginalSynonym", typeof(string));
			dtSynonymFirmCr.Columns.Add("InternalProducerSynonymId", typeof(long));
			dtSynonymFirmCr.Columns["InternalProducerSynonymId"].AutoIncrement = true;
			_logger.Debug("загрузили SynonymFirmCr");

			var daCore = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Core0 WHERE PriceCode={0} LIMIT 0", 3779), MyConn);
			daCore.Fill(dsMyDB, "Core");
			var dtCore = dsMyDB.Tables["Core"];
			dtCore.Columns.Add("InternalProducerSynonymId", typeof(long));
			dtCore.Columns.Add("CatalogId", typeof(long));
			_logger.Debug("загрузили Core");

			var daUnrecExp = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.UnrecExp WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
			var cbUnrecExp = new MySqlCommandBuilder(daUnrecExp);
			daUnrecExp.AcceptChangesDuringUpdate = false;
			daUnrecExp.InsertCommand = cbUnrecExp.GetInsertCommand();
			daUnrecExp.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
			daUnrecExp.InsertCommand.CommandTimeout = 0;
			daUnrecExp.Fill(dsMyDB, "UnrecExp");
			var dtUnrecExp = dsMyDB.Tables["UnrecExp"];
			dtUnrecExp.Columns["AddDate"].DataType = typeof(DateTime);
			dtUnrecExp.Columns.Add("InternalProducerSynonymId", typeof(long));
			_logger.Debug("загрузили UnrecExp");

			var daZero = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Zero WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
			var cbZero = new MySqlCommandBuilder(daZero);
			daZero.InsertCommand = cbZero.GetInsertCommand();
			daZero.InsertCommand.CommandTimeout = 0;
			daZero.Fill(dsMyDB, "Zero");
			var dtZero = dsMyDB.Tables["Zero"];
			_logger.Debug("загрузили Zero");

			var daForb = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Forb WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
			var cbForb = new MySqlCommandBuilder(daForb);
			daForb.InsertCommand = cbForb.GetInsertCommand();
			daForb.InsertCommand.CommandTimeout = 0;
			daForb.Fill(dsMyDB, "Forb");
			var dtForb = dsMyDB.Tables["Forb"];
			dtForb.Constraints.Add("ForbName", new[] {dtForb.Columns["Forb"]}, false);
			_logger.Debug("загрузили Forb");

			var daCoreCosts = new MySqlDataAdapter("SELECT * FROM farm.CoreCosts LIMIT 0", MyConn);
			daCoreCosts.Fill(dsMyDB, "CoreCosts");
			var dtCoreCosts = dsMyDB.Tables["CoreCosts"];
			_logger.Debug("загрузили CoreCosts");

				Stopwatch LoadExistsWatch = Stopwatch.StartNew();

				string existsCoreSQL;
				existsCoreSQL = String.Format("SELECT * FROM farm.Core0 WHERE PriceCode={0} order by Id", 3779);

				var daExistsCore = new MySqlDataAdapter(existsCoreSQL, MyConn);
				daExistsCore.Fill(dsMyDB, "ExistsCore");
				var dtExistsCore = dsMyDB.Tables["ExistsCore"];
				
				_logger.Debug("загрузили ExistsCore");

				string existsCoreCostsSQL;
				existsCoreCostsSQL = String.Format(@"
SELECT 
  CoreCosts.* 
FROM 
  farm.Core0, 
  farm.CoreCosts,
  usersettings.pricescosts
WHERE 
    Core0.PriceCode = {0} 
and pricescosts.PriceCode = {0}
and CoreCosts.Core_Id = Core0.id
and CoreCosts.PC_CostCode = pricescosts.CostCode 
order by Core0.Id", 3779);
				var daExistsCoreCosts = new MySqlDataAdapter(existsCoreCostsSQL, MyConn);
				var dtExistsCoreCosts = dtCoreCosts.Clone();
				dtExistsCoreCosts.TableName = "ExistsCoreCosts";
				dtExistsCoreCosts.Columns["PC_CostCode"].DataType = typeof(long);
				dsMyDB.Tables.Add(dtExistsCoreCosts);
				daExistsCoreCosts.Fill(dtExistsCoreCosts);
				_logger.Debug("загрузили ExistsCoreCosts");

				Stopwatch ModifyCoreCostsWatch = Stopwatch.StartNew();
				var relationExistsCoreToCosts = new DataRelation("ExistsCoreToCosts", dtExistsCore.Columns["Id"], dtExistsCoreCosts.Columns["Core_Id"]);
				dsMyDB.Relations.Add(relationExistsCoreToCosts);
				ModifyCoreCostsWatch.Stop();

				LoadExistsWatch.Stop();

				_logger.InfoFormat("Загрузка и подготовка существующего прайса : {0}", LoadExistsWatch.Elapsed);
				_logger.InfoFormat("Изменить CoreCosts : {0}", ModifyCoreCostsWatch.Elapsed);

			_logger.Debug("конец Prepare");
		}

		[Test]
		public void Formilize_test()
		{
			//@"..\..\Data\StressTest\517.dbf"
			//@"..\..\Data\StressTest\599.dbf"
			var appender = new ConsoleAppender {
				Layout = new SimpleLayout1()
			};
			appender.ActivateOptions();
			BasicConfigurator.Configure(appender);

			Settings.Default.SyncPriceCodes.Clear();
			Settings.Default.SyncPriceCodes.Add("3779");
			Settings.Default.SyncPriceCodes.Add("2819");

			Console.WriteLine("being {0}", DateTime.Now);
			TestHelper.Formalize(Path.GetFullPath(@"..\..\Data\StressTest\old_599.dbf"), 599);
			Console.WriteLine("being {0}", DateTime.Now);
		}

		[Test]
		public void Mass_load()
		{
/*			var commandText = new StringBuilder();
			for(var i = 0; i < 10000; i++)
			{
				commandText.AppendLine(String.Format("insert into Logs.StressTest Values('{0}');", i));
			}

			File.WriteAllText("command.sql", commandText.ToString());*/

			using(var connection = new MySqlConnection("server=testsql.analit.net;user=system;password=newpass"))
			{
				var stopWatch = Stopwatch.StartNew();
				connection.Open();
				var command = new MySqlCommand("", connection);
				for(var i = 0; i < 10000; i++)
				{
					command.CommandText = String.Format("insert into Logs.StressTest Values('{0}');", i);
					command.ExecuteNonQuery();
				}
				stopWatch.Stop();
				Console.WriteLine(stopWatch.Elapsed.TotalSeconds);

/*				var command = new MySqlCommand(commandText.ToString(), connection);
				command.ExecuteNonQuery();*/
			}
		}

		[Test]
		public void butcher()	
		{
			using (var connection = new MySqlConnection("server=testsql.analit.net;user=system;password=newpass;Allow User Variables=true"))
			{
				connection.Open();
/*				Console.WriteLine("1");
				var command = new MySqlCommand(File.ReadAllText("dump_0.sql"), connection);
				command.ExecuteNonQuery();

				Console.WriteLine("2");
				var command1 = new MySqlCommand(File.ReadAllText("dump_1.sql"), connection);
				command1.ExecuteNonQuery();

				Console.WriteLine("3");
				var command2 = new MySqlCommand(File.ReadAllText("dump_2.sql"), connection);
				command2.ExecuteNonQuery();*/

/*				var butcher = new Batcher(connection);
				var data = File.ReadAllBytes("dump_0.sql");
				var data1 = File.ReadAllBytes("dump_1.sql");
				var data2 = File.ReadAllBytes("dump_2.sql");
				butcher.Send(data);
				Console.WriteLine("send 1");
				butcher.Send(data1);
				Console.WriteLine("send 2");
				butcher.Send(data2);
				Console.WriteLine("send 3");*/
			}
		}

		[Test]
		public void compare()
		{
			var etalogn = File.ReadAllBytes(@"good\stream_dump_8");
			var bad = File.ReadAllBytes(@"stream_dump_8");
			Assert.That(etalogn, Is.EqualTo(bad));
		}
	}
}
