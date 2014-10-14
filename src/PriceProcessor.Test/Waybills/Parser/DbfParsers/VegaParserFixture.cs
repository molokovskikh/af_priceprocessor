using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	class VegaParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("Р-143129.dbf");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("143129"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("07.10.2014")));

			Assert.That(document.Lines.Count, Is.EqualTo(15));
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("34697"));
			Assert.That(line.Quantity, Is.EqualTo(20));
			Assert.That(line.SupplierCost, Is.EqualTo(16.99));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(30.89));
			Assert.That(line.Amount, Is.EqualTo(339.8));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(16.43));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-6));
			Assert.That(line.Period, Is.EqualTo("01.07.2016"));
			Assert.That(line.SerialNumber, Is.EqualTo("660614"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ10.Д98295"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("10 Сибирский ЦДиС"));
			Assert.That(line.Product, Is.EqualTo("Амоксициллин таб. 500мг №10 (Барнаульский ЗМП)"));
			Assert.That(line.Producer, Is.EqualTo("Барнаульский з-д мед.преп.ООО"));
			Assert.That(line.EAN13, Is.Null);
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.RegistryCost, Is.EqualTo(19.73));
		}
	}
}
