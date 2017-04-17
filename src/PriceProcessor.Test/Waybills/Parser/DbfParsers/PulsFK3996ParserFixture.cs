using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class PulsFK3996ParserFixture : DocumentFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00627149.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00627149"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("13.08.2012"));
			Assert.That(doc.Invoice.ShipperInfo, Is.EqualTo("ООО ФК ПУЛЬС"));
			var line = doc.Lines[0];

			Assert.That(line.Code, Is.EqualTo("05583"));
			Assert.That(line.Product, Is.EqualTo("Ампициллина т/г табл. 250 мг х20"));
			Assert.That(line.SerialNumber, Is.EqualTo("430612"));
			Assert.That(line.Period, Is.EqualTo("01.07.2015"));
			Assert.That(line.SupplierCost, Is.EqualTo(9.46));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BY.ФМ05.Д87322"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО \"ОЦС\" г. Екатеринбург"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(14.23));
			Assert.That(line.Producer, Is.EqualTo("Белмедпрепараты"));
			Assert.That(line.Country, Is.EqualTo("БЕЛАРУСЬ"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(8.6));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(8.8));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.ProducerCost, Is.EqualTo(9.68));
			Assert.That(line.EAN13, Is.EqualTo("4810133000169"));
			Assert.That(line.Amount, Is.EqualTo(94.6));
			Assert.That(line.NdsAmount, Is.EqualTo(8.6));
		}

		[Test]
		public void Parse_settings()
		{
			var parser = new Inforoom.PriceProcessor.Models.Parser("PulsFK3996Parser", appSupplier);
			parser.Add("DocName", "Header_ProviderDocumentId");
			parser.Add("DateDoc", "Header_DocumentDate");
			parser.Add("Vendor", "Invoice_ShipperInfo");
			parser.Add("Code", "Code");
			parser.Add("Good", "Product");
			parser.Add("Enterp", "Producer");
			parser.Add("Country", "Country");
			parser.Add("Price", "SupplierCost");
			parser.Add("Quant", "Quantity");
			parser.Add("Priceent", "ProducerCostWithoutNDS");
			parser.Add("PRDWNDS", "ProducerCost");
			parser.Add("DateB", "Period");
			parser.Add("Sert", "Certificates");
			parser.Add("NDS", "Nds");
			parser.Add("Reestr", "RegistryCost");
			parser.Add("ZNVLS", "VitallyImportant");
			parser.Add("Serial", "SerialNumber");
			parser.Add("SertWho", "CertificateAuthority");
			parser.Add("ProdSBar", "EAN13");
			parser.Add("PriceWONDS", "SupplierCostWithoutNDS");
			parser.Add("GTD", "BillOfEntryNumber");
			parser.Add("SUMSNDS", "Amount");
			parser.Add("SUMNDS", "NdsAmount");
			session.Save(parser);

			var ids = new WaybillService().ParseWaybill(new[] { CreateTestLog("00627149.dbf").Id });
			var doc = session.Load<Document>(ids[0]);
			var now = DateTime.Now;
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00627149"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("13.08.2012"));
			Assert.That(doc.Invoice.ShipperInfo, Is.EqualTo("ООО ФК ПУЛЬС"));
			Assert.That(doc.Lines.Count, Is.EqualTo(18));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("05583"));
			Assert.That(line.Product, Is.EqualTo("Ампициллина т/г табл. 250 мг х20"));
			Assert.That(line.SerialNumber, Is.EqualTo("430612"));
			Assert.That(line.Period, Is.EqualTo("01.07.2015"));
			Assert.That(line.SupplierCost, Is.EqualTo(9.46));  
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BY.ФМ05.Д87322"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО \"ОЦС\" г. Екатеринбург"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(14.23));
			Assert.That(line.Producer, Is.EqualTo("Белмедпрепараты"));
			Assert.That(line.Country, Is.EqualTo("БЕЛАРУСЬ"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(8.6));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(8.8));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.ProducerCost, Is.EqualTo(9.68));
			Assert.That(line.EAN13, Is.EqualTo("4810133000169"));
			Assert.That(line.Amount, Is.EqualTo(94.6));
			Assert.That(line.NdsAmount, Is.EqualTo(8.6));
		}
	}
}
