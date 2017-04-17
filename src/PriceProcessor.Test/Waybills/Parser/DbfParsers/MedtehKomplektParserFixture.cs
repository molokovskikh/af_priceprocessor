using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class MedtehKomplektParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(MedtehKomplektParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\n11169.dbf")));
			var document = WaybillParser.Parse("n11169.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(20));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Е11169"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("16.04.2012"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.BuyerName, Is.EqualTo("Фарма-555 ООО"));
			Assert.That(invoice.Amount, Is.EqualTo(1298.95));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("1906"));
			Assert.That(line.Product, Is.EqualTo("АЛЛОХОЛ табл. п/о №50"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(40.61));
			Assert.That(line.ProducerCost, Is.EqualTo(44.67));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(42.64));
			Assert.That(line.SupplierCost, Is.EqualTo(46.9));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Amount, Is.EqualTo(127.91));
			Assert.That(line.NdsAmount, Is.EqualTo(12.79));
			Assert.That(line.Producer, Is.EqualTo("Белмедпрепараты РУП Республика Беларусь"));
			Assert.That(line.Country, Is.EqualTo("Беларусь"));
			Assert.That(line.Period, Is.EqualTo("01.01.2016"));
			Assert.That(line.SerialNumber, Is.EqualTo("651211"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BY.ФМ05.Д79221"));
			Assert.That(line.CertificatesDate, Is.EqualTo("27.12.2011"));
			Assert.That(line.EAN13, Is.EqualTo("4810133003801"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.IsNull(line.BillOfEntryNumber);
		}
	}
}