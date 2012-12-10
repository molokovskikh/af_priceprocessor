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
			Assert.IsTrue(KatrenVolgogradParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\194149.dbf")));

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\194149.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(78));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("194149"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("06.12.2012")));

			Assert.That(document.Invoice.InvoiceNumber, Is.EqualTo("194149"));
			Assert.That(document.Invoice.InvoiceDate, Is.EqualTo(Convert.ToDateTime("06.12.2012")));
			Assert.That(document.Invoice.RecipientId, Is.EqualTo(5710506));
			Assert.That(document.Invoice.RecipientAddress, Is.EqualTo("414057, Астраханская обл., г. Астрахань, ул. Кубанская, 64"));
			Assert.That(document.Invoice.NDSAmount, Is.EqualTo(4521.92));
			Assert.That(document.Invoice.Amount, Is.EqualTo(50823.93));
			Assert.That(document.Invoice.NDSAmount10, Is.EqualTo(4275.05));
			Assert.That(document.Invoice.NDSAmount18, Is.EqualTo(246.87));
			Assert.That(document.Invoice.AmountWithoutNDS10, Is.EqualTo(42750.5));
			Assert.That(document.Invoice.AmountWithoutNDS18, Is.EqualTo(1371.5));
			Assert.That(document.Invoice.AmountWithoutNDS0, Is.EqualTo(2180.01));

			Assert.That(document.Lines[0].Code, Is.EqualTo("2300461"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("ГЛЮКОМЕТР ACCU-CHEK ACTIV /НАБОР/"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Roche Diagnostics GmbH"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Германия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(830));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(854.78));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(854.78));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(2.99));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(0));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(854.78));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(0));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("4015630057184"));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo("10130032/020812/0004916/39"));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(false));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("23449861"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("31.08.2013"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС DE.ИМ28.Д00607"));
			Assert.That(document.Lines[0].CertificateAuthority, Is.EqualTo("ИМ28"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo(null));
			Assert.That(document.Lines[0].OrderId, Is.EqualTo(null));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(null));
			Assert.That(document.Lines[0].RegistryDate, Is.EqualTo(null));
		}
	}
}