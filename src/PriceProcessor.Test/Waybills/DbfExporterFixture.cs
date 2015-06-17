using System;
using System.Data;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class DbfExporterFixture
	{
		private Document document;
		private DocumentReceiveLog log;

		[SetUp]
		public void Setup()
		{
			log = new DocumentReceiveLog {
				Id = 100,
				Supplier = new Supplier {
					Id = 201,
					Name = "Тестовый поставщик"
				},
				DocumentType = DocType.Waybill,
				LogTime = DateTime.Now,
				ClientCode = 1001,
				Address = new Address {
					Id = 501,
					Org = new Org {
						FullName = "Тестовое юр.лицо"
					}
				}
			};
			document = new Document(log) {
				ProviderDocumentId = "001-01"
			};
			document.NewLine(new DocumentLine {
				Product = "Алька-прим шип.таб. Х10",
				Code = "21603",
				Certificates = "РОСС PL.ФМ01.Д70041",
				Period = "01.12.2012",
				Producer = "Polfa/Polpharma",
				Country = "Польша",
				ProducerCost = 89.26m,
				SupplierCost = 89.26m,
				SupplierCostWithoutNDS = 98.19m,
				Quantity = 4,
				Nds = 10,
				ProductEntity = new Product { CatalogProduct = new Catalog { Name = "CatalogProduct" } },
			});
		}

		[Test]
		public void Export_protek_dbf_file()
		{
			Exporter.Convert(document, WaybillFormat.ProtekDbf, new WaybillSettings());
			var resultFile = Path.GetFullPath(@"DocumentPath\501\waybills\100_Тестовый поставщик(001-01).dbf");
			Assert.That(log.DocumentSize, Is.GreaterThan(0));
			Assert.That(log.FileName, Is.EqualTo("001-01.dbf"));
			Assert.That(File.Exists(resultFile), Is.True, "файл накладной несуществует {0}", resultFile);
			var table = Dbf.Load(resultFile);
			Assert.That(table.Rows.Count, Is.EqualTo(1));
		}

		[Test(Description = "Похоже что analitf не корректно обрабатывает / в имени файла, заменяем его")]
		public void Strip_slash()
		{
			document.ProviderDocumentId = "001-01/1";
			Exporter.Convert(document, WaybillFormat.ProtekDbf, new WaybillSettings());
			Assert.That(log.DocumentSize, Is.GreaterThan(0));
			Assert.That(log.FileName, Is.EqualTo("001-01_1.dbf"));
		}

		[Test]
		public void Export_universal_dbf()
		{
			var data = ExportFile();
			Assert.That(data.Rows.Count, Is.EqualTo(1));
			Assert.That(data.Rows[0]["name_post"], Is.EqualTo("Алька-прим шип.таб. Х10"));
		}

		[Test]
		public void Export_long_data()
		{
			document.Lines[0].Certificates = new String(Enumerable.Repeat('-', 200).ToArray());
			var data = ExportFile();
			Assert.That(data.Rows[0]["sert"].ToString().Length, Is.EqualTo(150));
		}

		[Test]
		public void ExportWithTTMFFieldsDocument()
		{
			document.SetInvoice();
			document.Invoice.StoreName = "склад";
			document.Lines[0].TradeCost = 1;
			document.Lines[0].SaleCost = 2;
			document.Lines[0].RetailCost = 3;
			document.Lines[0].Cipher = "шифр";
			var data = ExportFile();
			Assert.That(data.Rows[0]["shifr"], Is.EqualTo("шифр"));
			Assert.That(data.Rows[0]["storename"], Is.EqualTo("склад"));
			Assert.That(data.Rows[0]["opt_cena"], Is.EqualTo(1));
			Assert.That(data.Rows[0]["otp_cena"], Is.EqualTo(2));
			Assert.That(data.Rows[0]["rcena"], Is.EqualTo(3));
		}

		[Test]
		public void ExportCodeCr()
		{
			document.Lines[0].AssortimentPriceInfo = new AssortimentPriceInfo { CodeCr = 55555 };
			document.Lines[0].CodeCr = "123456";
			document.Lines[0].CertificatesEndDate = new DateTime(2015, 5, 15);
			var data = ExportFile();
			Assert.That(data.Rows[0]["idproducer"], Is.EqualTo(55555));
			Assert.That(data.Rows[0]["sp_prdr_id"], Is.EqualTo("123456"));
			Assert.AreEqual(new DateTime(2015, 5, 15), data.Rows[0]["sert_end"]);
		}

		private DataTable ExportFile()
		{
			var file = "Export_universal_dbf.dbf";
			if (File.Exists(file))
				File.Delete(file);
			DbfExporter.SaveUniversalV2(document, file);
			Assert.That(File.Exists(file));
			var data = Dbf.Load(file);
			return data;
		}
	}
}