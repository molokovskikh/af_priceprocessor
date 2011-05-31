using System;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	class ImperiaFarmaSpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			DocumentReceiveLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(74u); // код поставщика Империя-Фарма
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\KZ000130.dbf", documentLog) is ImperiaFarmaSpecialParser);

			var doc = WaybillParser.Parse("KZ000130.dbf", documentLog);
			Assert.That(doc.Lines.Count, Is.EqualTo(19));
			
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("КЗ000130"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("24.05.2011"));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("121701944"));
			Assert.That(line.Product, Is.EqualTo("Алмагель А 170мл сусп"));
			Assert.That(line.Producer, Is.EqualTo("Балканфарма Троян АД"));
			Assert.That(line.Country, Is.EqualTo("Болгария"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.ProducerCost, Is.EqualTo(75.03));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(83.21));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCost, Is.EqualTo(91.53));
			Assert.That(line.NdsAmount, Is.EqualTo(24.96));
			Assert.That(line.Amount, Is.EqualTo(274.59));
			Assert.That(line.SerialNumber, Is.EqualTo("040111"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.BG.ФМ09.Д15233"));
			Assert.That(line.Period, Is.EqualTo("01.01.2013"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(ImperiaFarmaSpecialParser.CheckFileFormat(ImperiaFarmaSpecialParser.Load(@"..\..\Data\Waybills\KZ000130.dbf")));
		}
	}
}
