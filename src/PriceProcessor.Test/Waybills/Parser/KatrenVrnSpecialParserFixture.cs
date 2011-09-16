using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	class KatrenVrnSpecialParserFixture
	{
		[Test, Ignore]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 4138u } }; // код Катрен Воронеж			
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\275748.dbf", documentLog) is KatrenVrnSpecialParser);

			var doc = WaybillParser.Parse("275748.dbf", documentLog);
			Assert.That(doc.Lines.Count, Is.EqualTo(14));

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
			Assert.That(line.VitallyImportant, Is.EqualTo(false));

			Assert.That(doc.Lines[1].VitallyImportant, Is.EqualTo(true));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(ImperiaFarmaSpecialParser.CheckFileFormat(ImperiaFarmaSpecialParser.Load(@"..\..\Data\Waybills\KZ000130.dbf")));
		}
	}
}
