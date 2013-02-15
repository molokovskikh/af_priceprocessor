using System;
using Common.MySql;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Threading;
using System.IO;
using Inforoom.PriceProcessor;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture, Ignore]
	public class BasePriceParserFixture
	{
		private const int priceItemId = 688;

		[Test, Ignore("Не работает на пустой базе")]
		public void Double_synonim_test()
		{
			With.Connection(c => {
				var command = new MySqlCommand("delete FROM farm.SynonymFirmCr where Synonym = 'Merckle GmbH для Ratiopha';", c);
				command.ExecuteNonQuery();
			});
			var file = @"..\..\Data\590.dbf";
			var formalizer = PricesValidator.Validate(file, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), 590u);
			formalizer.Formalize();
			With.Connection(c => {
			var command = new MySqlCommand(
@"SELECT count(*) FROM farm.SynonymFirmCr S
where Synonym = 'Merckle GmbH для Ratiopha';", c);
			Assert.That(Convert.ToInt32(command.ExecuteScalar()), Is.EqualTo(2));
			});
		}

		[Test, Ignore, Description("Тест для проверки вставки значений в поле ProducerCost. Правила формализации берутся из таблицы FormRules")]
		public void FormalizeProducerCost2()
		{
			var sqlFormRules = @"
delete from usersettings.PriceItems where Id = ?PriceItemId;

insert into farm.FormRules(PriceFormatId, MaxOld, Delimiter, FCode, FName1, FFirmCr, FVolume, FQuantity, FPeriod, FProducerCost)
values(11, 5, ';', 'F1', 'F2', 'F3', 'F4', 'F5', 'F7', 'F8');

insert into usersettings.PriceItems(
	Id,
	FormRuleId, 
	SourceId, 
	RowCount, UnformCount,
	PriceDate, 
	LastDownload, 
	LastFormalization, 
	LastRetrans, 
	WaitingDownloadInterval)
values(
	?PriceItemId,
	Last_Insert_ID(), 
	(select Id from farm.Sources limit 1),
	0, 0, 
	(date_sub(now(), interval 1 day)),
	(date_sub(now(), interval 1 day)),
	(date_sub(now(), interval 1 day)),
	NULL,
	24
);

insert into usersettings.PricesCosts(PriceCode, PriceItemId, Enabled, AgencyEnabled, CostName, BaseCost) 
values(?PriceCode, ?PriceItemId, 1, 1, 'TestCost', 1);";
			With.Connection(connection => {
				var command = new MySqlCommand(sqlFormRules, connection);
				command.Parameters.AddWithValue("?PriceCode", GetPriceCode(priceItemId));
				command.Parameters.AddWithValue("?PriceItemId", priceItemId);
				command.ExecuteNonQuery();
			});

			// Копируем файл в Inbound и запускаем формализацию
			var fileName = @"formalize-producer-cost";
			//var fileName = @"producer-cost-with-empty";
			var file = String.Format(@"..\..\Data\{0}-{1}.txt", priceItemId, fileName);
			TestHelper.InitDirs(Settings.Default.InboundPath, Settings.Default.BasePath, Settings.Default.ErrorFilesPath);
			File.Copy(file, String.Format("{0}\\{1}.txt", Settings.Default.InboundPath, priceItemId));

			var handler = new FormalizeHandler();
			handler.StartWork();
			Thread.Sleep(10000);
			handler.HardStop();
			CheckProducerCostInCore(GetPriceCode(priceItemId));
			// В Base должен лежать файл
			Assert.That(Directory.GetFiles(Settings.Default.BasePath).Length, Is.EqualTo(1));
		}

		private ulong GetPriceCode(uint priceItemId)
		{
			// Получаем код прайса по priceItemId
			var cost = TestHelper.Fill(String.Format(@"select * from usersettings.pricescosts pc where pc.PriceItemId = {0}", priceItemId));
			var corePriceCode = Convert.ToUInt64(cost.Tables[0].Rows[0]["PriceCode"]);
			return corePriceCode;
		}

		private void CheckProducerCostInCore(ulong corePriceCode)
		{
			// Считаем кол-во позиций в core0 для данного прайс-листа, где цена производителя существует и она не нулевая
			// (она также должна быть меньше всех остальных цен)
			With.Connection(connection => {
				var command = new MySqlCommand(@"
select count(*) from farm.Core0 core
  join farm.CoreCosts cc on core.Id = cc.Core_id and cc.Cost > core.ProducerCost
where PriceCode = ?PriceCode and ProducerCost is not null;", connection);
				command.Parameters.AddWithValue("?PriceCode", corePriceCode);
				var countCorePositions = Convert.ToUInt32(command.ExecuteScalar());
				Assert.That(countCorePositions, Is.EqualTo(14));
			});
		}

		private void CheckNdsInCore(ulong corePriceCode)
		{
			// Считаем кол-во позиций в core0 для данного прайс-листа, где цена производителя существует и она не нулевая
			// (она также должна быть меньше всех остальных цен)
			With.Connection(connection => {
				var command = new MySqlCommand(@"
select count(*) from farm.Core0 core
  join farm.CoreCosts cc on core.Id = cc.Core_id
where PriceCode = ?PriceCode and Nds is not null;", connection);
				command.Parameters.AddWithValue("?PriceCode", corePriceCode);
				var countCorePositions = Convert.ToUInt32(command.ExecuteScalar());
				Assert.That(countCorePositions, Is.EqualTo(13));
			});
		}

		[Test, Ignore, Description("Тест для проверки вставки значений в поле Nds. Правила формализации берутся из таблицы FormRules")]
		public void FormalizeNds2()
		{
			var sqlFormRules = @"
delete from usersettings.PriceItems where Id = ?PriceItemId;

insert into farm.FormRules(PriceFormatId, MaxOld, Delimiter, FCode, FName1, FFirmCr, FVolume, FQuantity, FPeriod, FProducerCost, FNds)
values(11, 5, ';', 'F1', 'F2', 'F3', 'F4', 'F5', 'F7', 'F8', 'F9');

insert into usersettings.PriceItems(
	Id,
	FormRuleId, 
	SourceId, 
	RowCount, UnformCount,
	PriceDate, 
	LastDownload, 
	LastFormalization, 
	LastRetrans, 
	WaitingDownloadInterval)
values(
	?PriceItemId,
	Last_Insert_ID(), 
	(select Id from farm.Sources limit 1),
	0, 0, 
	(date_sub(now(), interval 1 day)),
	(date_sub(now(), interval 1 day)),
	(date_sub(now(), interval 1 day)),
	NULL,
	24
);

insert into usersettings.PricesCosts(PriceCode, PriceItemId, Enabled, AgencyEnabled, CostName, BaseCost) 
values(?PriceCode, ?PriceItemId, 1, 1, 'TestCost', 1);";
			With.Connection(connection => {
				var command = new MySqlCommand(sqlFormRules, connection);
				command.Parameters.AddWithValue("?PriceCode", GetPriceCode(priceItemId));
				command.Parameters.AddWithValue("?PriceItemId", priceItemId);
				command.ExecuteNonQuery();
			});

			// Копируем файл в Inbound и запускаем формализацию
			var fileName = @"formalize-nds";
			//var fileName = @"producer-cost-with-empty";
			var file = String.Format(@"..\..\Data\{0}-{1}.txt", priceItemId, fileName);
			TestHelper.InitDirs(Settings.Default.InboundPath, Settings.Default.BasePath, Settings.Default.ErrorFilesPath);
			File.Copy(file, String.Format("{0}\\{1}.txt", Settings.Default.InboundPath, priceItemId));

			var handler = new FormalizeHandler();
			handler.StartWork();
			Thread.Sleep(10000);
			handler.HardStop();
			CheckNdsInCore(GetPriceCode(priceItemId));
			// В Base должен лежать файл
			Assert.That(Directory.GetFiles(Settings.Default.BasePath).Length, Is.EqualTo(1));
		}
	}
}
