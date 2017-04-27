using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SiaAstrahanParser2Fixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(SiaAstrahanParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\P-829079.dbf")));

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\P-829079.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(7));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-829079"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("07.08.2012")));

			Assert.That(document.Invoice.InvoiceNumber, Is.EqualTo("Р-829079"));
			Assert.That(document.Invoice.InvoiceDate, Is.EqualTo(Convert.ToDateTime("07.08.2012")));
			Assert.That(document.Invoice.Amount, Is.EqualTo(3393.21));
			Assert.That(document.Invoice.AmountWithoutNDS, Is.EqualTo(3084.73));
			Assert.That(document.Invoice.NDSAmount, Is.EqualTo(308.48));
			Assert.That(document.Invoice.RecipientAddress, Is.EqualTo("Ларионова З.А."));

			Assert.That(document.Lines[0].Code, Is.EqualTo("7937"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Бускопан 10мг Таб. п/о Х20"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Дельфарм Реймс"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("ФРАНЦИЯ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(null));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(246.60));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(224.18));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(null));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(246.60));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(22.42));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo(9006968001692));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo("10130030/221211/0005683/5"));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(false));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("119539"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.09.2016"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("POCC FR.ФМ01.Д76377"));
			Assert.That(document.Lines[0].CertificateAuthority, Is.EqualTo("ФГУ \"ЦС МЗ РФ\" г. Москва"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("22.12.2011"));
			Assert.That(document.Lines[0].OrderId, Is.EqualTo(31899656));
			Assert.That(document.Lines[5].RegistryCost, Is.EqualTo(1292));
		}
	}
}