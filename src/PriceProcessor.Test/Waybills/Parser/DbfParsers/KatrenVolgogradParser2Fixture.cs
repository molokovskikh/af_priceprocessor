using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KatrenVolgogradParser2Fixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(KatrenVolgogradParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\118509.dbf")));

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\118509.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(7));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("118509"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("31.07.2012")));

			Assert.That(document.Invoice.InvoiceNumber, Is.EqualTo("118509"));
			Assert.That(document.Invoice.InvoiceDate, Is.EqualTo(Convert.ToDateTime("31.07.2012")));
			Assert.That(document.Invoice.Amount, Is.EqualTo(464.20));
			Assert.That(document.Invoice.AmountWithoutNDS, Is.EqualTo(422));
			Assert.That(document.Invoice.NDSAmount, Is.EqualTo(42.20));
			Assert.That(document.Invoice.RecipientAddress, Is.EqualTo("414038, Астраханская обл., г. Астрахань, Трусовский район, ул. Хибинская, 4"));

			Assert.That(document.Lines[0].Code, Is.EqualTo("4128915"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("ГЛИЦИН 0,1 N50 ТАБЛ ПОДЪЯЗЫЧ"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("БИОТИКИ МНПК,ООО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(20));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(19.04));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(21.10));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(10.82));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(464.20));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(42.20));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("4601687000015"));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo(null));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(true));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("1250512"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.06.2015"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д49720"));
			Assert.That(document.Lines[0].CertificateAuthority, Is.EqualTo("ФГБУ \"ЦЭККМП\"Росздравнадзора"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo(null));
			Assert.That(document.Lines[0].OrderId, Is.EqualTo(null));
		}
	}
}
