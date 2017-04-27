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
	public class FarmSet_VoronezhParserFixture : DocumentFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Та534098.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("ФК001534098"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("13.11.2012")));
			var line = doc.Lines[2];
			Assert.That(line.Code, Is.EqualTo("114"));
			Assert.That(line.Product, Is.EqualTo("Бронхолитин 125мл сироп"));
			Assert.That(line.Producer, Is.EqualTo("Sopharma"));
			Assert.That(line.Country, Is.EqualTo("БОЛГАРИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(52.39));
			Assert.That(line.SupplierCost, Is.EqualTo(57.63));
			Assert.That(line.SerialNumber, Is.EqualTo("4010512"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BG.ФМ03.Д79568"));
			Assert.That(line.Period, Is.EqualTo("01.05.2016"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(43.73));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.EqualTo(3800010650175));
			Assert.That(line.CertificateFilename, Is.EqualTo(@"Б\БРОНХОЛИТИН_4010512.TIF"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10210190/030712/0011654"));
			Assert.That(line.OrderId, Is.EqualTo(35302648));
			Assert.That(line.CountryCode, Is.EqualTo("100"));
		}

		/// <summary>
		/// Новые данные от задачи
		/// http://redmine.analit.net/issues/28233
		/// </summary>
		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\Ли081018.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(10));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("ФК002081018"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("08.10.2014")));

			var line = document.Lines[3];
			Assert.That(line.Code, Is.EqualTo("12879"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130130/090714/0011201/3"));
			Assert.That(line.EAN13, Is.EqualTo(4013054001264));
			Assert.That(line.CountryCode, Is.EqualTo("276"));
			Assert.That(line.UnitCode, Is.EqualTo("778"));
		}

		[Test]
		public void Parse_settings()
		{
			var parser = new Inforoom.PriceProcessor.Models.Parser("FarmSet_VoronezhParser", appSupplier);
			parser.Add("DOCNO", "Header_ProviderDocumentId");
			parser.Add("DOCDAT", "Header_DocumentDate");
			parser.Add("CODTOVAR", "Code");
			parser.Add("TOVARNAME", "Product");
			parser.Add("PROIZV", "Producer");
			parser.Add("STRANA", "Country");
			parser.Add("KOLVO", "Quantity");
			parser.Add("NDS", "Nds");
			parser.Add("CENAPOST", "SupplierCostWithoutNDS");
			parser.Add("CENASNDS", "SupplierCost");
			parser.Add("SERIA", "SerialNumber");
			parser.Add("SERT", "Certificates");
			parser.Add("DATAOT", "CertificatesDate");
			parser.Add("DATADO", "CertificatesEndDate");
			parser.Add("ORGAN", "CertificateAuthority");
			parser.Add("SROK", "Period");
			parser.Add("CENAREESTR", "RegistryCost");
			parser.Add("CENAPROIZ", "ProducerCostWithoutNDS");
			parser.Add("PV", "VitallyImportant");
			parser.Add("SHTRIH", "EAN13");
			parser.Add("KODEI", "UnitCode");
			parser.Add("SERTFILE", "CertificateFilename");
			parser.Add("GTD", "BillOfEntryNumber");
			parser.Add("DOC_ID", "OrderId");
			parser.Add("KODSTRANA", "CountryCode");
			session.Save(parser);

			var ids = new WaybillService().ParseWaybill(new[] { CreateTestLog("Ли081018.dbf").Id });
			var document = session.Load<Document>(ids[0]);
			Assert.That(document.Lines.Count, Is.EqualTo(10));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("ФК002081018"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("08.10.2014")));

			var line = document.Lines[3];
			Assert.That(line.Code, Is.EqualTo("12879"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130130/090714/0011201/3"));
			Assert.That(line.EAN13, Is.EqualTo(4013054001264));
			Assert.That(line.CountryCode, Is.EqualTo("276"));
			Assert.That(line.UnitCode, Is.EqualTo("778"));
		}
	}
}