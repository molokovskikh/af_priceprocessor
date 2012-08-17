using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KatrenVolgogradParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\Kat23445.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(47));

			Assert.That(document.ProviderDocumentId, Is.EqualTo("23445"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("14.02.2012")));

			Assert.That(document.Invoice.AmountWithoutNDS10, Is.EqualTo(19328.4));
			Assert.That(document.Invoice.AmountWithoutNDS18, Is.EqualTo(908.6));
			Assert.That(document.Invoice.AmountWithoutNDS0, Is.EqualTo(0));

			Assert.That(document.Lines[0].Code, Is.EqualTo("4845409"));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("4607027761356"));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(13.24));

			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(16.28));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(14.8));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));

			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("091111"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.12.2014"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АМЛОДИПИН 0,005 N30 ТАБЛ"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("ОЗОН,ООО"));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(true));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(91.70));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo(null));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ05.Д62302"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("01.12.2014"));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(48.84));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(4.44));
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsTrue(KatrenVolgogradParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\Kat23445.dbf")));
		}
	}
}