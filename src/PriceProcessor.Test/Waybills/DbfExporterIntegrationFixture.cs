using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class DbfExporterIntegrationFixture : IntegrationFixture
	{
		private Document document;
		private DocumentReceiveLog log;
		private Product _product;
		private Producer _producer;
		private DocumentLine _line;
		private TestSupplier supplier;
		private TestClient client;
		[SetUp]
		public void Setup()
		{
			client = TestClient.CreateNaked();
			supplier = TestSupplier.CreateNaked();
			log = new DocumentReceiveLog(session.Load<Supplier>(supplier.Id), session.Load<Address>(client.Addresses[0].Id)) {
				FileName = "1234.dbf"
			};
			session.Save(log);
			document = new Document(log) {
				ProviderDocumentId = "11111"
			};
			session.Save(log);

			var catalogName = new TestCatalogName { Name = "testName" };
			var catalogForm = new TestCatalogForm { Form = "testForm" };
			session.Save(catalogForm);
			session.Save(catalogName);
			var catalog = new Catalog { Name = "testCatalog", NameId = catalogName.Id, FormId = catalogForm.Id };
			_product = new Product {
				CatalogProduct = catalog
			};
			_producer = new Producer { Name = "testProducer" };
			session.Save(catalog);
			session.Save(_product);
			session.Save(_producer);
			_line = new DocumentLine {
				ProducerId = (int)_producer.Id,
				ProductEntity = _product,
				Product = "123",
				Document = document
			};
			document.Lines = new List<DocumentLine>();
			document.Lines.Add(_line);
			session.Save(document);
			client.Settings.IsConvertFormat = true;
			client.Settings.WaybillConvertFormat = TestWaybillFormat.UniversalDbf;
			session.Save(client.Settings);
			session.Flush();
		}

		[Test(Description = "Проверяет конвертацию при заданном ассортиментном ПЛ")]
		public void ConvertToUniversalWithAssortimentPriceTest()
		{
			var assortPrice = new TestPrice {
				AgencyEnabled = true,
				Enabled = true,
				Supplier = supplier,
				PriceType = PriceType.Assortment,
				PriceName = "Ассортиментный"
			};
			session.Save(assortPrice);
			client.Settings.AssortmentPriceId = assortPrice.Id;
			session.Save(client.Settings);
			var synonym = new TestProductSynonym("Тестовый синоним товара",
				session.Load<TestProduct>(_product.Id), assortPrice);
			session.Save(synonym);
			var prSynonym = new TestProducerSynonym("Тестовый синоним производителя",
				session.Load<TestProducer>(_producer.Id),
				assortPrice);
			session.Save(prSynonym);
			session.Flush();
			var core = new TestCore {
				Code = "0000",
				Producer = session.Load<TestProducer>(_producer.Id),
				Price = assortPrice,
				ProducerSynonym = prSynonym,
				ProductSynonym = synonym,
				Product = session.Load<TestProduct>(_product.Id),
				Quantity = "1",
				Period = "01.01.2100"
			};

			session.Save(core);
			session.Flush();
			var result = ExportFile();
			Assert.That(result.Rows[0]["name_artis"], Is.EqualTo("Тестовый синоним товара"));
			Assert.That(result.Rows[0]["przv_artis"], Is.EqualTo("Тестовый синоним производителя"));
		}

		[Test(Description = @"Проверяет заполнение продукта и производителя из каталога
вместо ассортиментного Пл, если ассортиментный Пл не задан")]
		public void ConvertToUniversalWithoutAssortimentPriceTest()
		{
			var result = ExportFile();
			Assert.That(result.Rows[0]["name_artis"], Is.EqualTo("testCatalog"));
			Assert.That(result.Rows[0]["przv_artis"], Is.EqualTo("testProducer"));
		}

		private DataTable ExportFile()
		{
			Exporter.ConvertIfNeeded(document, session.Load<WaybillSettings>(client.Settings.Id));
			var newLog = session.Query<DocumentReceiveLog>().First(l => l.ClientCode == client.Id && l.Id != document.Log.Id);
			var file = newLog.GetRemoteFileNameExt();
			Assert.That(File.Exists(file));
			var data = Dbf.Load(file);
			return data;
		}
	}
}
