using System;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.Formalizer;
using Test.Support;
using NUnit.Framework;
using Test.Support.Suppliers;


namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceProcessThreadTest : IntegrationFixture
	{
		[Test(Description = "проверка корректности логирования при возникновении WarningFormalizeException")]
		public void CatchWarningFormalizeExceptionTest()
		{
			var priceItemId = CatchWarningFormalizeExceptionTestPrepareData();
			var priceProcessItem = new PriceProcessItem(false, 0, null, priceItemId, @"Data\781.dbf", null);
			var priceProcessThread = new PriceProcessThread(priceProcessItem, String.Empty, false);
			var outPriceFileName = Path.Combine(Settings.Default.BasePath, priceProcessItem.PriceItemId + Path.GetExtension(priceProcessItem.FilePath));
			File.Delete(outPriceFileName);
			FlushAndCommit();
			priceProcessThread.ThreadWork();
			Assert.False(String.IsNullOrEmpty(priceProcessThread.CurrentErrorMessage), "Отсутствует информация о произошедшем исключении");
			Assert.True(priceProcessThread.FormalizeOK, "Формализация закончилась с ошибкой");

			var logs = Inforoom.PriceProcessor.Models.FormLog.Queryable
				.Where(l => l.PriceItemId == priceItemId
					&& l.ResultId == (int?)FormResults.Error)
				.ToList();
			Assert.That(logs.Implode(x => x.Addition), Does.Contain("Прайс отключен по причине : FirmStatus"),
				"Информация о предупреждении отсутствует в БД");
			//Проверяем, что копирование файла прошло успешно
			Assert.IsTrue(File.Exists(outPriceFileName));
		}

		private uint CatchWarningFormalizeExceptionTestPrepareData(PriceFormatType priceFormatId = PriceFormatType.NativeDbf, CostType priceCostType = CostType.MultiColumn)
		{
			var supplier = TestSupplier.Create(session);
			supplier.Disabled = true;
			var price = supplier.Prices[0];
			price.CostType = priceCostType;

			var item = price.Costs.First().PriceItem;
			var format = price.Costs.Single().PriceItem.Format;
			format.PriceFormat = priceFormatId;

			session.Save(supplier);
			price.Save();
			return item.Id;
		}
	}
}