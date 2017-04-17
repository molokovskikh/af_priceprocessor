﻿using Common.Tools;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class ASTIFarmacevtika12799ParserFixture : DocumentFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\61487.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(36));
			Assert.That(document.ProviderDocumentId, Is.EqualTo(" 61487")); // Откуда пробел?
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("02.05.2012"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.BuyerAddress, Is.EqualTo(null));

			var line = document.Lines[0];

			Assert.That(line.Code, Is.EqualTo("1Y2Z-34"));
			Assert.That(line.Product, Is.EqualTo("Атровент Н аэрозоль 200 доз 10мл"));
			Assert.That(line.Producer, Is.EqualTo("BOEHRINGER INGELHEIM"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Unit, Is.EqualTo("уп."));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(273.45));
			Assert.That(line.SupplierCost, Is.EqualTo(300.80));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(282.55));
			Assert.That(line.ProducerCost, Is.EqualTo(310.81));
			Assert.That(line.Nds, Is.EqualTo(10));

			Assert.That(line.NdsAmount, Is.EqualTo(54.69));
			Assert.That(line.Amount, Is.EqualTo(601.59));
			Assert.That(line.Period, Is.EqualTo("31.08.2014"));
			Assert.That(line.SerialNumber, Is.EqualTo("106177"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130130/281111/0026488/18"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ08.Д52854"));
			Assert.That(line.CertificatesDate, Is.EqualTo("31.08.2014"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО\"Окружной центр контроля и качества\""));

			Assert.That(line.VitallyImportant, Is.EqualTo(true));
			Assert.That(line.EAN13, Is.EqualTo("9006968003115"));

			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-3.33));
			Assert.That(line.RegistryCost, Is.EqualTo(283.27));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(ASTIFarmacevtika12799Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\61487.dbf")));
		}

		[Test]
		public void Parse_settings()
		{
			var parser = new Inforoom.PriceProcessor.Models.Parser("ASTIFarmacevtika12799Parser", appSupplier);
			parser.Add("NOMDOC", "Header_ProviderDocumentId");
			parser.Add("DATDOC", "Header_DocumentDate");
			parser.Add("TO", "Invoice_BuyerAddress");
			parser.Add("CodeTov", "Code");
			parser.Add("TovName", "Product");
			parser.Add("PrName", "Producer");
			parser.Add("PrStrana", "Country");
			parser.Add("EdIzm", "Unit");
			parser.Add("Kol", "Quantity");
			parser.Add("CwoNDS", "SupplierCostWithoutNDS");
			parser.Add("CwNDS", "SupplierCost");
			parser.Add("CPwoNDS", "ProducerCostWithoutNDS");
			parser.Add("CPwNDS", "ProducerCost");
			parser.Add("StNDS", "Nds");
			parser.Add("SumNDS", "NdsAmount");
			parser.Add("Vsego", "Amount");
			parser.Add("SrokGodn", "Period");
			parser.Add("Seriya", "SerialNumber");
			parser.Add("GTD", "BillOfEntryNumber");
			parser.Add("SertNom", "Certificates");
			parser.Add("SertData", "CertificatesDate");
			parser.Add("SertOrg", "CertificateAuthority");
			parser.Add("Proc", "SupplierPriceMarkup");
			parser.Add("Creestr", "RegistryCost");
			parser.Add("GN2", "VitallyImportant");
			parser.Add("EAN", "EAN13");
			session.Save(parser);

			var ids = new WaybillService().ParseWaybill(new[] { CreateTestLog("61487.dbf").Id });
			var document = session.Load<Document>(ids[0]);
			Assert.That(document.Lines.Count, Is.EqualTo(36));
			Assert.That(document.ProviderDocumentId, Is.EqualTo(" 61487")); // Откуда пробел?
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("02.05.2012"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.BuyerAddress, Is.EqualTo(null));

			var line = document.Lines[0];

			Assert.That(line.Code, Is.EqualTo("1Y2Z-34"));
			Assert.That(line.Product, Is.EqualTo("Атровент Н аэрозоль 200 доз 10мл"));
			Assert.That(line.Producer, Is.EqualTo("BOEHRINGER INGELHEIM"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Unit, Is.EqualTo("уп."));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(273.45));
			Assert.That(line.SupplierCost, Is.EqualTo(300.80));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(282.55));
			Assert.That(line.ProducerCost, Is.EqualTo(310.81));
			Assert.That(line.Nds, Is.EqualTo(10));

			Assert.That(line.NdsAmount, Is.EqualTo(54.69));
			Assert.That(line.Amount, Is.EqualTo(601.59));
			Assert.That(line.Period, Is.EqualTo("31.08.2014"));
			Assert.That(line.SerialNumber, Is.EqualTo("106177"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130130/281111/0026488/18"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ08.Д52854"));
			Assert.That(line.CertificatesDate, Is.EqualTo("31.08.2014"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО\"Окружной центр контроля и качества\""));

			Assert.That(line.VitallyImportant, Is.EqualTo(true));
			Assert.That(line.EAN13, Is.EqualTo("9006968003115"));

			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-3.33));
			Assert.That(line.RegistryCost, Is.EqualTo(283.27));
		}
	}
}