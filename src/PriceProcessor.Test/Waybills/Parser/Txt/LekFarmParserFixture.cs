using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class LekFarmParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\B094192.sst");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Б094192"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("22.08.2016")));
			Assert.That(doc.Invoice.Amount, Is.EqualTo(3801.75));
			Assert.That(doc.Lines.Count, Is.EqualTo(13));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("6008"));
			Assert.That(line.Product, Is.EqualTo("Аспаркам тб №60 (Фармапол-Волга)"));
			Assert.That(line.Producer, Is.EqualTo("Фармапол-Волга / Россия"));
			Assert.That(line.Country, Is.EqualTo("Фармапол-Волга / Россия"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.SupplierCost, Is.EqualTo(44.5));
			Assert.That(line.Amount, Is.EqualTo(222.5));
			Assert.That(line.Period, Is.EqualTo("01.11.2018"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("ГТД"));
			Assert.That(line.SerialNumber, Is.EqualTo("171115"));
			Assert.That(line.Certificates, Is.Null);
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОРГАН"));
			Assert.That(line.CertificatesDate, Is.EqualTo("ДатаВыдачи"));
			Assert.That(line.CertificatesEndDate, Is.Null); // ДатаОконч
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.RegistryCost, Is.EqualTo(82));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(59.56));
		}
	}
}