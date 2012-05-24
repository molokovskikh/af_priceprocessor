using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Lekomed12223ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\рн011046.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(9));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("ЛК000011046"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("24.05.2012"));

			Assert.That(document.Invoice.SellerName, Is.EqualTo(null));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("ЛК000000241"));
			Assert.That(line.Product, Is.EqualTo("Биопарокс аэр. 400 доз 10мл"));
			Assert.That(line.EAN13, Is.EqualTo(null));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС HU ФМ11 Д76073"));
			Assert.That(line.CertificatesDate, Is.EqualTo("14.12.2011"));
			Assert.That(line.Country, Is.EqualTo("ВЕНГРИЯ"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130110/040412/0002238/1"));
			Assert.That(line.Producer, Is.EqualTo("Эгис Фармацевтический завод ОАО"));
			Assert.That(line.SerialNumber, Is.EqualTo("5201011"));
			Assert.That(line.Period, Is.EqualTo("01.10.2013"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(null));
			Assert.That(line.ProducerCost, Is.EqualTo(null));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(310.91));
			Assert.That(line.SupplierCost, Is.EqualTo(342));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Amount, Is.EqualTo(1026));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.RegistryCost, Is.EqualTo(null));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(Lekomed12223Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\рн011046.dbf")));
		}
	}
}
