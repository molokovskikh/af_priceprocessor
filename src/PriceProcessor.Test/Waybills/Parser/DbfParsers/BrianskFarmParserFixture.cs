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
	public class BrianskFarmParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(BrianskFarmParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\77277071.DBF")));
			var doc = WaybillParser.Parse("77277071.DBF");
			var line = doc.Lines[0];
			var invoice = doc.Invoice;
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("6_77277071"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("07.06.2013"));
			Assert.That(line.Code, Is.EqualTo("4160004270"));
			Assert.That(line.Product, Is.EqualTo("Диклофенак гель 5% 30г туба (инд уп)"));
			Assert.That(line.Producer, Is.EqualTo("Синтез"));
			Assert.That(doc.Lines[1].BillOfEntryNumber, Is.EqualTo("10113080/250213/0003055/1"));
			Assert.That(line.SerialNumber, Is.EqualTo("220313"));
			Assert.That(line.Period, Is.EqualTo("01.04.2015"));
			Assert.That(line.Quantity, Is.EqualTo(4));
			Assert.That(line.SupplierCost, Is.EqualTo(31.35));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(28.5));
			Assert.That(line.NdsAmount, Is.EqualTo(11.4));
			Assert.That(line.Amount, Is.EqualTo(125.4));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(25.91));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д50228"));
			Assert.That(line.CertificatesDate, Is.EqualTo(null));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО Окружной ЦКК , г.Москва"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(10));
			Assert.That(doc.Lines[1].BillOfEntryNumber, Is.EqualTo("10113080/250213/0003055/1"));
			Assert.That(line.Unit, Is.EqualTo("шт."));
			Assert.That(line.EAN13, Is.EqualTo("4602565013509"));
			Assert.That(line.Nds, Is.EqualTo(11));
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("6_77277071"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("07.06.2013"));
			Assert.That(invoice.SellerName, Is.EqualTo("ООО \" Брянскфарм\""));
			Assert.That(invoice.SellerAddress, Is.EqualTo("242620, Брянская обл, Дятьковский р-н, п. Любохна, ул. Сидорова, д,2"));
			Assert.That(invoice.SellerINN, Is.EqualTo("3202009890"));
			Assert.That(invoice.SellerKPP, Is.EqualTo("324501001"));
			Assert.That(invoice.BuyerName, Is.EqualTo("ИП Буренок С, Я,"));
			Assert.That(invoice.BuyerAddress, Is.EqualTo("г.Брянск, ул.Авиационная, 5-а"));
			Assert.That(invoice.BuyerINN, Is.EqualTo("320700042889"));
			Assert.That(invoice.BuyerKPP, Is.EqualTo(null));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(2425.1));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(242.5));
			Assert.That(invoice.Amount10, Is.EqualTo(2667.6));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(0));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(0));
			Assert.That(invoice.Amount18, Is.EqualTo(0));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(2425.1));
			Assert.That(invoice.NDSAmount, Is.EqualTo(242.5));
			Assert.That(invoice.Amount, Is.EqualTo(2667.6));
		}
	}
}
