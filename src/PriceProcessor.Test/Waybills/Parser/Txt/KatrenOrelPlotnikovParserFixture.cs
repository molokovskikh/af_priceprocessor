using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class KatrenOrelPlotnikovParserFixture
	{

		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\359308.txt");
			Assert.That(KatrenOrelPlotnikovParser.CheckFileFormat(@"..\..\Data\Waybills\359308.txt"),Is.True);
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("359308"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("13.12.2012")));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("27494733"));
			Assert.That(line.Product, Is.EqualTo("АМОКСИЦИЛЛИН 0,5 N16 КАПС"));
			Assert.That(line.Producer, Is.EqualTo("Барнаульский завод медпрепаратов,ООО"));
			Assert.That(line.Country, Is.EqualTo("россия"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SupplierCost, Is.EqualTo(35.42));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(32.20));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[2].BillOfEntryNumber, Is.EqualTo("10130032/101012/0006634/1"));
			Assert.That(line.Certificates, Is.EqualTo("Б/Н"));
			Assert.That(line.CertificateAuthority, Is.Null);
			Assert.That(line.CertificatesDate, Is.Null);
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.SerialNumber, Is.EqualTo("180912"));
			Assert.That(line.Period, Is.EqualTo("01.10.2014"));
			Assert.That(line.RegistryCost, Is.EqualTo(48.19));
		}
	}
}
