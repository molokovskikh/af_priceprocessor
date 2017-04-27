using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KatrenOneMoreParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("294393-03_22052016.kbf");
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2016, 05, 22)));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("294393-03"));
			Assert.AreEqual(doc.Lines.Count, 32);

			var line = doc.Lines[0];
			Assert.That(line.Amount, Is.EqualTo(871.86));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("RU Д-RU.АЯ42.В01996"));
			Assert.That(line.CertificatesDate, Is.EqualTo("18.06.2015"));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(new DateTime(2016, 12, 23)));
			Assert.That(line.CertificateAuthority, Is.EqualTo("АЯ42"));
			Assert.That(line.Code, Is.EqualTo("56334405"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.EAN13, Is.EqualTo(5029053543192));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(79.26));
			Assert.That(line.OrderId, Is.EqualTo(304747));
			Assert.That(line.Period, Is.EqualTo("01.03.2019"));
			Assert.That(line.CodeCr, Is.EqualTo("38439315"));
			Assert.That(line.Producer, Is.EqualTo("Кимберли-Кларк, ООО"));
			Assert.That(line.ProducerCost, Is.EqualTo(871.86));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(792.60));
			Assert.That(line.Product, Is.EqualTo("HUGGIES ПОДГУЗНИКИ CLASSIC 5/11-25КГ N58"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.RegistryDate, Is.Null);
			Assert.That(line.SerialNumber, Is.EqualTo("032016"));
			Assert.That(line.SupplierCost, Is.EqualTo(871.86));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(792.60));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
		}
	}
}
