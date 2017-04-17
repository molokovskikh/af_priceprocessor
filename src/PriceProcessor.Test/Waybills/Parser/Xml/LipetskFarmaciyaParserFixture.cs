using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Xml
{
	[TestFixture]
	public class LipetskFarmaciyaParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\00УT-057181.xml");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00УT-057181"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2016,10,12)));

			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("L-Лизина эсцинат, конц. д/пригот. р-ра для в/в введ. 1 мг/мл 5 мл №10"));
			Assert.That(line.Producer, Is.EqualTo("ГАЛИЧФАРМ ПАО"));
			Assert.That(line.Country, Is.EqualTo("УКРАИНА"));
			Assert.That(line.SerialNumber,  Is.EqualTo("810815"));
			Assert.That(line.Period, Is.EqualTo("2017-08-01"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Certificates, Is.EqualTo("РОССUAФМ08Д98874"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО ОЦКК МОСКВА"));
			Assert.That(line.CertificatesDate, Is.EqualTo("2015-11-19"));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(new DateTime(2017, 08, 01)));
			Assert.That(line.EAN13, Is.EqualTo(4823000800724));


			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(1155.6));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(1155.6));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.Nds, Is.EqualTo(10));
		}
	}
}
