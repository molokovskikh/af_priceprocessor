using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class FarmSet_VoronezhFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Му067781.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("ФК000067781"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("23.01.2012"));
			Assert.That(doc.OrderId, Is.EqualTo(25632871));
			var line = doc.Lines[0];            
			Assert.That(line.Code, Is.EqualTo("19614"));
			Assert.That(line.Product, Is.EqualTo("Аквалор Софт 125мл спрей фл."));
			Assert.That(line.Producer, Is.EqualTo("YS LAB Le Forum"));
			Assert.That(line.Country, Is.EqualTo("ФРАНЦИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(202.94));
			Assert.That(line.SupplierCost, Is.EqualTo(223.23));
			Assert.That(line.SerialNumber, Is.EqualTo("11N60B"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС FR.ИМ25.А03912"));
			Assert.That(line.Period, Is.EqualTo("01.08.2014"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(206.82));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.VitallyImportant, !Is.True);
			Assert.That(line.EAN13, Is.EqualTo("3582910130031"));
			Assert.That(line.CertificateFilename, Is.EqualTo(@"КОСМЕТИКА\АКВАЛОР_СОФТ_11N_60В_125МЛ.TIF"));
		}
	}
}

