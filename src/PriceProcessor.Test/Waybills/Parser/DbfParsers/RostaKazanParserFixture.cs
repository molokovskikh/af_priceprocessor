using NUnit.Framework;
namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class RostaKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("140952_21.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(44));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("140952_21"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("04.09.2012"));
			Assert.That(doc.Invoice.BuyerName, Is.EqualTo("[15] №2, г. Н. Челны, 42/21, р-н ж-д"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("680012450"));
			Assert.That(line.Product, Is.EqualTo("911 Окопник гель-бальзам д/сустав 100мл"));
			Assert.That(line.Producer, Is.EqualTo("Твинс Тэк ЗАО - Россия"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(35.01));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(39.63));
			Assert.That(line.SupplierCost, Is.EqualTo(46.76));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(13.19));
			Assert.That(line.NdsAmount, Is.EqualTo(7.13));
			Assert.That(line.Amount, Is.EqualTo(39.63));
			Assert.That(line.SerialNumber, Is.EqualTo("072012"));
			Assert.That(line.Period, Is.EqualTo("01.01.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU АГ50 Д00015"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОС \" ЕВРОСТРОЙ\""));
			Assert.That(line.CertificatesDate, Is.EqualTo("17.11.2011"));
		}
	}
}
