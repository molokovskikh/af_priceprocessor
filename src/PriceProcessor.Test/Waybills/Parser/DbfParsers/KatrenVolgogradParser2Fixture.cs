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
	public class KatrenVolgogradParser2Fixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(KatrenVolgogradParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\197818.dbf")));

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\197818.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(2));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("197818"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("12.12.2012")));

			Assert.That(document.Invoice.InvoiceNumber, Is.EqualTo("197818"));
			Assert.That(document.Invoice.InvoiceDate, Is.EqualTo(Convert.ToDateTime("12.12.2012")));
			Assert.That(document.Invoice.RecipientId, Is.EqualTo(14761309));
			Assert.That(document.Invoice.RecipientAddress, Is.EqualTo("414000, Астраханская Область, г Астрахань, Кировский район, ул. Чалабяна/Ногина/Свердлова, 19/7/98."));
			Assert.That(document.Invoice.NDSAmount, Is.EqualTo(286.56));
			Assert.That(document.Invoice.Amount, Is.EqualTo(2940.57));
			Assert.That(document.Invoice.NDSAmount10, Is.EqualTo(238.95));
			Assert.That(document.Invoice.NDSAmount18, Is.EqualTo(47.61));
			Assert.That(document.Invoice.AmountWithoutNDS10, Is.EqualTo(2389.5));
			Assert.That(document.Invoice.AmountWithoutNDS18, Is.EqualTo(264.51));
			Assert.That(document.Invoice.AmountWithoutNDS0, Is.EqualTo(0));

			Assert.That(document.Lines[0].Code, Is.EqualTo("14876028"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("ГРОПРИНОСИН 0,5 N50 ТАБЛ"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Гедеон Рихтер Польша, ООО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Польша"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(870.22));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(796.5));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(876.15));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(2628.45));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(238.95));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("5997001302903"));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo("10112040/230512/0003450/1"));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(false));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("H23022A"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.03.2015"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС PL.ФМ01.Д62859"));
			Assert.That(document.Lines[0].CertificateAuthority, Is.EqualTo("ФГБУ \"ЦЭККМП\"Росздравнадзора"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo(null));
			Assert.That(document.Lines[0].OrderId, Is.EqualTo(null));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(null));
			Assert.That(document.Lines[0].RegistryDate, Is.EqualTo(null));
			Assert.That(document.Lines[0].CodeCr, Is.EqualTo("8741688"));
		}
	}
}