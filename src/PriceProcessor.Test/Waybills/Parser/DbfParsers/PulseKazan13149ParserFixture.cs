using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class PulseKazan13149ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00014_29.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00000029"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("21.08.2012"));
			Assert.That(doc.Invoice.BuyerName, Is.EqualTo("ООО \"Мир Здоровья\""));
			var line = doc.Lines[0];

			Assert.That(line.Code, Is.EqualTo("20891"));
			Assert.That(line.Product, Is.EqualTo("5 дней ванна д/ног дезодорирующая 25 г. х10"));
			Assert.That(line.Producer, Is.EqualTo("Санкт-Петербургская фарм. фабрика"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(86.42));
			Assert.That(line.SupplierCost, Is.EqualTo(101.98));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.NdsAmount, Is.EqualTo(15.56));
			Assert.That(line.Amount, Is.EqualTo(101.98));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(line.SerialNumber, Is.EqualTo("30212"));
			Assert.That(line.Period, Is.EqualTo("01.03.2015"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АЯ61.Д28644"));
			Assert.That(line.CertificatesDate, Is.EqualTo("12.10.2011"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОРГАН ПО СЕРТИФИКАЦИИ ПРОДУКЦИ"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.EAN13, Is.EqualTo(4605059012989));
		}
	}
}