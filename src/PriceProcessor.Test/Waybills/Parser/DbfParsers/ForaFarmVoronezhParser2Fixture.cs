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
	public class ForaFarmVoronezhParser2Fixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\249137.dbf")));

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\249137.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(7));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("249137"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("26.07.2012")));

			Assert.That(document.Invoice.InvoiceNumber, Is.EqualTo("249137"));
			Assert.That(document.Invoice.InvoiceDate, Is.EqualTo(Convert.ToDateTime("26.07.2012")));
			Assert.That(document.Invoice.AmountWithoutNDS, Is.EqualTo(1535.62));
			Assert.That(document.Invoice.RecipientAddress, Is.EqualTo("42667"));

			Assert.That(document.Lines[0].Code, Is.EqualTo("11187"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("911 Венолгон гель д/ног 100мл"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Твинс Тэк"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("РОССИЯ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(36.56));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(43.14));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(18));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(129.42));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(19.74));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("4607010242558"));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo(null));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(false));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("0612"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.12.2013"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("POCC  RU.АГ50.Д00016"));
			Assert.That(document.Lines[0].CertificateAuthority, Is.EqualTo("ОС ООО \"ЕВРОСТРОЙ\""));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("17.11.2011"));
			Assert.That(document.Lines[0].OrderId, Is.EqualTo(31539501));
		}
	}
}