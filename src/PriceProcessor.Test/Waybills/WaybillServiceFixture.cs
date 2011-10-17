using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate.Criterion;
using NUnit.Framework;
using PriceProcessor.Test.Waybills.Parser;
using Test.Support;
using Test.Support.Helpers;
using Test.Support.Suppliers;
using log4net.Config;

namespace PriceProcessor.Test.Waybills
{
	public class DocumentFixture
	{
		protected TestClient client;
		protected TestSupplier supplier;
		protected TestPrice price;
		protected TestAddress testAddress;

		protected Supplier appSupplier;
		protected WaybillSettings settings;
		protected Address address;

		protected string docRoot;
		protected string waybillsPath;

		[SetUp]
		public void Setup()
		{
			client = TestClient.Create();
			testAddress = client.Addresses[0];
			address = Address.Find(testAddress.Id);
			settings = WaybillSettings.Find(client.Id);

			docRoot = Path.Combine(Settings.Default.DocumentPath, address.Id.ToString());
			waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);

			price = TestSupplier.CreateTestSupplierWithPrice();
			supplier = price.Supplier;
			appSupplier = Supplier.Find(supplier.Id);
		}

		public TestDocumentLog CreateTestLog(string file)
		{
			var log = new TestDocumentLog(supplier, testAddress, file);
			using (new TransactionScope())
				log.SaveAndFlush();

			File.Copy(@"..\..\Data\Waybills\" + file, Path.Combine(waybillsPath, String.Format("{0}_{1}({2}){3}",
				log.Id,
				supplier.Name,
				Path.GetFileNameWithoutExtension(file),
				Path.GetExtension(file))));
			return log;
		}

		public TestDocumentLog[] CreateTestLogs(params string[] files)
		{
			return files.Select(f => CreateTestLog(f)).ToArray();
		}
	}

	[TestFixture]
	public class WaybillServiceFixture : DocumentFixture
	{
		private uint[] ParseFile(string filename)
		{
			var file = filename;
			var log = CreateTestLog(file);

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] {log.Id});
			return ids;
		}

		[Test]
		public void Parse_waybill()
		{
			var ids = ParseFile("1008fo.pd");

			using(new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}

		[Test(Description = "тест разбора накладной с ShortName поставщика в имени файла")]
		public void Parse_waybill_with_ShortName_in_fileName()
		{
			var ids = ParseFile("1008fo.pd");

			using (new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}


		[Test]
		public void Parse_waybill_without_header()
		{
			var ids = ParseFile("00000049080.sst");

			using (new SessionScope())
			{
				Assert.That(ids.Count(),Is.EqualTo(0));
			}
		}

		[Test(Description = "Проверка сопоставления идентификатора продукта синониму. Синоним есть в БД")]
		public void Check_SetProductId_if_synonym_exists()
		{
			var file = "14356_4.dbf";
			TestDocumentLog log;
			TestProducer producer1;
			TestProducer producer2;
			TestProduct product;

			using (new SessionScope())
			{
				log = CreateTestLog(file);

				product = new TestProduct("тестовый товар");
				product.SaveAndFlush();

				var productSynonym = new TestSynonym {
					ProductId = product.Id,
					Synonym = "Коринфар таб п/о 10мг № 50",
					PriceCode = (int?) price.Id
				};

				productSynonym.SaveAndFlush();

				productSynonym = new TestSynonym {
					ProductId = null,
					Synonym = "Коринфар таб п/о 10мг № 50",
					PriceCode = (int?) price.Id
				};

				producer1 = new TestProducer {
					Name = "Тестовый производитель",
				};
				producer1.SaveAndFlush();

				producer2 = new TestProducer {
					Name = "Тестовый производитель",
				};
				producer2.SaveAndFlush();

				var producerSynonym = new TestProducerSynonym {
					Price = price,
					Name = "Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ",
					Producer = null
				};
				producerSynonym.SaveAndFlush();

				producerSynonym = new TestProducerSynonym {
					Price = price,
					Name = "Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ",
					Producer = producer1
				};
				producerSynonym.SaveAndFlush();

				producerSynonym = new TestProducerSynonym
				{
					Price = price,
					Name = "Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ",
					Producer = producer2
				};
				producerSynonym.SaveAndFlush();
			}
			
			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

			using (new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
				Assert.IsTrue(waybill.Lines[0].ProductId != null);
				Assert.That(waybill.Lines[0].ProductId, Is.EqualTo(product.Id));
				Assert.That(waybill.Lines[0].ProducerId, Is.EqualTo(producer1.Id));
			}
		}

		[Test(Description = "Проверка сопоставления идентификатора продукта синониму. Синонима нет в БД")]
		public void Check_SetProductId_if_synonym_not_exists()
		{
			var ids = ParseFile("14356_4.dbf");

			using (new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
				Assert.IsTrue(waybill.Lines[0].ProductId == null);
				Assert.IsTrue(waybill.Lines[0].ProducerId == null);
			}
		}

		[Test]
		public void Check_SetProductId()
		{
			var ids = ParseFile("14326_4.dbf");

			using (new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(4));
				Check_DocumentLine_SetProductId(waybill);
			}
		}

		public void Check_DocumentLine_SetProductId(TestWaybill document)
		{
			var line = document.Lines[0];
			
			var listSynonym = new List<string> { line.Product };
			var priceCodes = Price.Queryable
									.Where(p => (p.Supplier.Id == document.FirmCode))
									.Select(p => (p.ParentSynonym ?? p.Id)).Distinct().ToList();

			if (priceCodes.Count < 0)
			{
				Assert.True(document.Lines.Where(l => l.ProductId == null).Count() == document.Lines.Count);
				return;
			}
			
			var criteria = DetachedCriteria.For<TestSynonym>();
			criteria.Add(Restrictions.In("Synonym", listSynonym));
			criteria.Add(Restrictions.In("PriceCode", priceCodes));

			var synonym = SessionHelper.WithSession(c => criteria.GetExecutableCriteria(c).List<TestSynonym>()).ToList();
			if (synonym.Count > 0)
			{
				Assert.IsTrue(synonym.Where(s => s.ProductId != null).Select(s => s.ProductId).Contains(line.ProductId));
			}
			else
			{
				Assert.IsTrue(line.ProductId == null);
			}
		}

		[Test]
		public void Check_SetProducerId()
		{
			var ids = ParseFile("14326_4.dbf");

			using (new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(4));
				Check_DocumentLine_SetProducerId(waybill);
			}
		}

		public void Check_DocumentLine_SetProducerId(TestWaybill document)
		{			
			var line = document.Lines[3];

			var listSynonym = new List<string> { line.Producer };
			var priceCodes = Price.Queryable
									.Where(p => (p.Supplier.Id == document.FirmCode))
									.Select(p => (p.ParentSynonym ?? p.Id)).Distinct().ToList();

			if (priceCodes.Count < 0)
			{
				Assert.True(document.Lines.Where(l => l.ProducerId == null).Count() == document.Lines.Count);
				return;
			}

			var criteria = DetachedCriteria.For<TestSynonymFirm>();
			criteria.Add(Restrictions.In("Synonym", listSynonym));
			criteria.Add(Restrictions.In("PriceCode", priceCodes));

			var synonym = SessionHelper.WithSession(c => criteria.GetExecutableCriteria(c).List<TestSynonymFirm>()).ToList();
			if (synonym.Count > 0)
			{				
				Assert.IsTrue(synonym.Where(s => s.CodeFirmCr != null).Select(s => s.CodeFirmCr).Contains(line.ProducerId));
			}
			else
			{
				Assert.IsTrue(line.ProducerId == null);
			}
		}

		[Test, Description("Парсинг накладной и проверка настройки IsConvertFormat для клиента. Настройка разрешает сохранение накладной в dbf формате.")]		
		public void Parse_and_Convert_to_Dbf()
		{
			using (new TransactionScope())
			{
				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = Core.Queryable.First().Price.Id;
				settings.SaveAndFlush();
			}

			ParseFile("14326_4.dbf");

			using (new SessionScope())
			{
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.ClientCode == client.Id);
				Assert.That(logs.Count(), Is.EqualTo(2));
				Assert.That(logs.Where(l => l.IsFake).Count(), Is.EqualTo(1));
				Assert.That(logs.Where(l => !l.IsFake).Count(), Is.EqualTo(1));

				// Проверяем наличие записей в documentheaders для исходных документов.
				foreach (var documentLog in logs)
				{
					var count = documentLog.IsFake
									? Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id && doc.Log.IsFake).Count()
									: Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id && doc.Log.IsFake).Count();
					Assert.That(count, documentLog.IsFake ? Is.EqualTo(1) : Is.EqualTo(0));
				}
				var files_dbf = Directory.GetFiles(Path.Combine(docRoot, "Waybills"), "*.dbf");
				Assert.That(files_dbf.Count(), Is.EqualTo(2));
				settings.IsConvertFormat = false;
				settings.AssortimentPriceId = null;
				settings.SaveAndFlush();
			}
		}

		[Test(Description = "Проверка сопоставления кода клиента по ассортиментному прайс листу")]
		public void Check_SetAssortimentInfo()
		{
			var file = "14356_4.dbf";
			TestDocumentLog log;
			TestProducer producer;
			TestProduct product;

			using (new SessionScope())
			{
				log = CreateTestLog(file);

				product = new TestProduct("тестовый товар");
				product.SaveAndFlush();
				var productSynonym = new TestProductSynonym("Коринфар таб п/о 10мг № 50", product, price);
				productSynonym.SaveAndFlush();

				producer = new TestProducer{Name = "Тестовый производитель"};
				producer.SaveAndFlush();

				var producerSynonym = new TestProducerSynonym{ Price = price, Name = "Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ", Producer = producer };
				producerSynonym.SaveAndFlush();

				var core = new TestCore() { Price = price, Code = "1234567", ProductSynonym = productSynonym, ProducerSynonym = producerSynonym, Product = product, Producer = producer, Quantity = "0", Period = "01.01.2015"};
				core.SaveAndFlush();

				core = new TestCore() { Price = price, Code = "111111", ProductSynonym = productSynonym, ProducerSynonym = producerSynonym, Product = product, Producer = producer, Quantity = "0", Period = "01.01.2015" };
				core.SaveAndFlush();

				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = price.Id;
				settings.SaveAndFlush();
			}

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

			using (new SessionScope())
			{
				var doc = Document.Find(ids.Single());
				Assert.That(doc.Lines.Count, Is.EqualTo(1));
				Assert.IsTrue(doc.Lines[0].ProductEntity != null);
				Assert.That(doc.Lines[0].ProductEntity.Id, Is.EqualTo(product.Id));
				Assert.That(doc.Lines[0].ProducerId, Is.EqualTo(producer.Id));

				var resultDoc = DocumentReceiveLog.Queryable.Single(d => d.Address == address && d.IsFake == false);
				var files = Directory.GetFiles(waybillsPath, "*.dbf");
				Assert.That(files.Count(), Is.EqualTo(2), files.Implode());
	
				Console.WriteLine(waybillsPath);
				var data = Dbf.Load(resultDoc.GetFileName(), Encoding.GetEncoding(866));
				Assert.IsTrue(data.Columns.Contains("id_artis"));
				Assert.That(data.Rows[0]["id_artis"], Is.EqualTo("111111"));
				Assert.IsTrue(data.Columns.Contains("name_artis"));
				Assert.That(data.Rows[0]["name_artis"], Is.EqualTo("Коринфар таб п/о 10мг № 50"));
				Assert.IsTrue(data.Columns.Contains("przv_artis"));
				Assert.That(data.Rows[0]["przv_artis"], Is.EqualTo("Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ"));
			}
		}

		[Test]
		public void TestSaveWaybills()
		{
			string waybillpath = @"..\..\Data\Waybills\";
			string file = "1039428.xml";
			string savefilename = Path.Combine(Settings.Default.DownWaybillsPath, Path.GetFileName(file));
			string waybillfilename = Path.Combine(waybillpath, Path.GetFileName(file));
			WaybillService.SaveWaybill(waybillfilename);
			Assert.That(File.Exists(savefilename), Is.True);
			File.Delete(savefilename);
		}

		[Test]
		public void Parse_and_Convert_to_Dbf_if_sert_date()
		{
			using (new TransactionScope())
			{
				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = price.Id;
				settings.SaveAndFlush();
			}

			var ids = ParseFile("20101119_8055_250829.xml");

			using (new SessionScope())
			{
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.ClientCode == client.Id);
				Assert.That(logs.Count(), Is.EqualTo(2));
				Assert.That(logs.Where(l => l.IsFake).Count(), Is.EqualTo(1));
				Assert.That(logs.Where(l => !l.IsFake).Count(), Is.EqualTo(1));

				// Проверяем наличие записей в documentheaders для исходных документов.
				foreach (var documentLog in logs)
				{
					var count = documentLog.IsFake
									? Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id && doc.Log.IsFake).Count()
									: Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id && doc.Log.IsFake).Count();
					Assert.That(count, documentLog.IsFake ? Is.EqualTo(1) : Is.EqualTo(0));
				}
				var files_dbf = Directory.GetFiles(Path.Combine(docRoot, "Waybills"), "*.dbf");
				Assert.That(files_dbf.Count(), Is.EqualTo(1));
				var file_dbf = files_dbf.Select(f => f).First();
				var data = Dbf.Load(file_dbf, Encoding.GetEncoding(866));
				Assert.IsTrue(data.Columns.Contains("sert_date"));
				Assert.That(data.Rows[0]["sert_date"], Is.EqualTo("10.08.2009"));
			}
		}

		[Test]
		public void Document_invoice_test()
		{
			Document doc;
			using (new SessionScope())
			{
				var settings = TestDrugstoreSettings.FindFirst();
				var order = TestOrder.FindFirst();

				var log = new DocumentReceiveLog() {
					Supplier = appSupplier,
					ClientCode = settings.Id,
					Address = address,
					MessageUid = 123,
					DocumentSize = 100
				};

				doc = new Document(log) {
					OrderId = order.Id,
					DocumentDate = DateTime.Now
				};
				var inv = doc.SetInvoice();
				inv.BuyerName = "Тестовый покупатель";

				log.Save();
				doc.Save();
			}

			using (new SessionScope())
			{
				Document doc2 = Document.Find(doc.Id);
				Assert.That(doc2, Is.Not.Null);
				Assert.That(doc2.Invoice, Is.Not.Null);
				Assert.That(doc2.Invoice.Id, Is.EqualTo(doc.Id));
				Assert.That(doc2.Invoice.BuyerName, Is.EqualTo("Тестовый покупатель"));

				Invoice inv2 = Invoice.Find(doc.Id);
				Assert.That(inv2, Is.Not.Null);
				Assert.That(inv2.Id, Is.EqualTo(doc.Id));
				Assert.That(inv2.Document, Is.Not.Null);
				Assert.That(inv2.Document.Id, Is.EqualTo(doc.Id));
				Assert.That(inv2.Document.Address.Id, Is.EqualTo(doc.Address.Id));
				Assert.That(inv2.BuyerName, Is.EqualTo("Тестовый покупатель"));
			}
		}

		[Test]
		public void Convert_if_exist_ean13_field()
		{
			var doc = WaybillParser.Parse("69565_0.dbf");
			var invoice = doc.Invoice;
			Assert.That(invoice, Is.Not.Null);
			using (new TransactionScope())
			{
				var order = TestOrder.FindFirst();
				var log = new DocumentReceiveLog {
					Supplier = appSupplier,
					ClientCode = client.Id,
					Address = address,
					DocumentType = DocType.Waybill,
					MessageUid = 123,
					DocumentSize = 100
				};

				doc.Log = log;
				doc.OrderId = order.Id;
				doc.Address = log.Address;
				doc.FirmCode = log.Supplier.Id;
				doc.ClientCode = (uint) log.ClientCode;

				settings.AssortimentPriceId = price.Id;
				settings.Save();

				doc.SetProductId();

				var path = Path.GetDirectoryName(log.GetRemoteFileNameExt());
				Directory.Delete(path, true);

				var result = DbfExporter.ConvertAndSaveDbfFormatIfNeeded(doc);

				Assert.That(result, Is.True, "файл не был сконвертирован");
				var files_dbf = Directory.GetFiles(path, "*.dbf");
				Assert.That(files_dbf.Count(), Is.EqualTo(1));
				var file_dbf = files_dbf.Select(f => f).First();
				var data = Dbf.Load(file_dbf, Encoding.GetEncoding(866));
				Assert.IsTrue(data.Columns.Contains("ean13"));
				Assert.That(data.Rows[0]["ean13"], Is.EqualTo("5944700100019"));
			}
		}

		[Test]
		public void RemoveDoubleSpacesTest()
		{
			IList<string> ls = new List<string>();
			ls.Add(" aaa         bbbb ccc       ddd ");
			ls.Add(String.Empty);
			ls.Add(null);
			  
			ls = ls.Select(l => l.RemoveDoubleSpaces()).ToList();

			Assert.That(ls[0], Is.EqualTo(" aaa bbbb ccc ddd "));
			Assert.That(ls[1], Is.EqualTo(String.Empty));
			Assert.That(ls[2], Is.EqualTo(String.Empty));
		}

		[Test(Description = "Пытаемся разобрать накладную от СИА с возможностью конвертации накладной, в результирующем файле конвертируемой накладной должны быть корректно выставлены коды сопоставленных позиций")]
		public void ConvertWaybillToDBFWithAssortmentCodes()
		{
			var doc = WaybillParser.Parse("9046752.DBF");
			using (new TransactionScope())
			{
				var testsupplier = (TestSupplier)TestSupplier.Find(2779u);
				var supplier = Supplier.Find(testsupplier.Id);
				var settings = TestDrugstoreSettings.Find(10365u);
				Assert.That(settings.IsConvertFormat, Is.True);
				Assert.That(settings.AssortimentPriceId, Is.Not.Null);
				var client = (TestClient)TestClient.Find(settings.Id);
				var order = TestOrder.FindFirst();
				var address = Address.Find(client.Addresses[0].Id);
				DocumentReceiveLog log = new DocumentReceiveLog() {
					Supplier = supplier,
					ClientCode = settings.Id,
					Address = address,
					MessageUid = 123,
					DocumentSize = 100
				};

				doc.Log = log;
				doc.OrderId = order.Id;
				doc.Address = address;
				doc.FirmCode = log.Supplier.Id;
				doc.ClientCode = (uint)log.ClientCode;

				doc.SetProductId();

				var path = Path.GetDirectoryName(log.GetRemoteFileNameExt());
				Directory.Delete(path, true);

				DbfExporter.ConvertAndSaveDbfFormatIfNeeded(doc);

				var files_dbf = Directory.GetFiles(path, "*.dbf");
				Assert.That(files_dbf.Count(), Is.EqualTo(1));
				var file_dbf = files_dbf[0];
				var data = Dbf.Load(file_dbf, Encoding.GetEncoding(866));
				Assert.That(data.Rows.Count, Is.EqualTo(45));
				Assert.IsTrue(data.Columns.Contains("ID_ARTIS"));
				Assert.IsTrue(data.Columns.Contains("NAME_ARTIS"));
				Assert.IsTrue(data.Columns.Contains("NAME_POST"));
				Assert.That(data.Rows[0]["NAME_POST"], Is.EqualTo("Амоксициллин 500мг таб. Х20 (R)"));
				Assert.That(data.Rows[1]["NAME_POST"], Is.EqualTo("Андипал Таб Х10"));

				Assert.That(data.Rows[0]["ID_ARTIS"], Is.Not.EqualTo("100208"));
				Assert.That(data.Rows[0]["NAME_ARTIS"], Is.Not.EqualTo("МилдронатR р-р д/ин., 10 % 5 мл № 10"));

				Assert.That(data.Rows[1]["ID_ARTIS"], Is.Not.EqualTo("100208"));
				Assert.That(data.Rows[1]["NAME_ARTIS"], Is.Not.EqualTo("МилдронатR р-р д/ин., 10 % 5 мл № 10"));
			}
		}
		
		[Test]
		public void ComparisonWithOrdersTest()
		{
			OrderHead order1;
			OrderHead order2;
			Document document;

			using (new SessionScope())
			{
				order1 = BuildOrder();
				order1.Save();
				var item = new OrderItem { Code = "Code1", Order = order1, Quantity = 20 }; item.Save();
				item = new OrderItem { Code = "Code2", Order = order1, Quantity = 25 }; item.Save();
				item = new OrderItem { Code = "Code3", Order = order1, Quantity = 50 }; item.Save();
				item = new OrderItem { Code = "Code4", Order = order1, Quantity = 100 }; item.Save();

				order2 = BuildOrder();
				order2.Save();
				item = new OrderItem { Code = "Code3", Order = order2, Quantity = 15 }; item.Save();
				item = new OrderItem { Code = "Code3", Order = order2, Quantity = 10 }; item.Save();
				item = new OrderItem { Code = "Code5", Order = order2, Quantity = 15 }; item.Save();

				var log = new DocumentReceiveLog {
					Supplier = appSupplier,
					ClientCode = client.Id,
					Address = address,
					MessageUid = 123,
					DocumentSize = 100
				};
				document = new Document(log) {
					OrderId = order1.Id,
					DocumentDate = DateTime.Now
				};
				var docline = new DocumentLine { Document = document, Code = "Code1", Quantity = 20 };
				document.NewLine(docline);                
				docline = new DocumentLine { Document = document, Code = "Code2", Quantity = 15 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code2", Quantity = 5 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code3", Quantity = 75 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = null, Quantity = 75 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code5", Quantity = 10 };
				document.NewLine(docline);                
				log.Save();
				document.Save();
			}

			IList<OrderHead> orders = new List<OrderHead>();

			using (new SessionScope())
			{
				orders.Add(OrderHead.Find(order1.Id));
				orders.Add(OrderHead.Find(order2.Id));

				document = Document.Find(document.Id);
				order1 = OrderHead.Find(order1.Id);
				order2 = OrderHead.Find(order2.Id);
			}
						
			WaybillService.ComparisonWithOrders(document, orders);

			string inStr = String.Empty;
			foreach (var line in document.Lines)
			{
				if (!String.IsNullOrEmpty(inStr)) inStr += ",";
				inStr += line.Id.ToString();
			}
			var ds = TestHelper.Fill(String.Format("select * from documents.waybillorders where DocumentLineId in {0};", String.Format("({0})", inStr)));
			var table = ds.Tables[0];
			Assert.That(table.Rows.Count, Is.EqualTo(7));
			Assert.That(table.Rows[0]["DocumentLineId"], Is.EqualTo(document.Lines[0].Id));
			Assert.That(table.Rows[0]["OrderLineId"], Is.EqualTo(order1.Items[0].Id));
			Assert.That(table.Rows[1]["DocumentLineId"], Is.EqualTo(document.Lines[1].Id));
			Assert.That(table.Rows[1]["OrderLineId"], Is.EqualTo(order1.Items[1].Id));
			Assert.That(table.Rows[2]["DocumentLineId"], Is.EqualTo(document.Lines[2].Id));
			Assert.That(table.Rows[2]["OrderLineId"], Is.EqualTo(order1.Items[1].Id));
			Assert.That(table.Rows[3]["DocumentLineId"], Is.EqualTo(document.Lines[3].Id));
			Assert.That(table.Rows[3]["OrderLineId"], Is.EqualTo(order1.Items[2].Id));
			Assert.That(table.Rows[4]["DocumentLineId"], Is.EqualTo(document.Lines[3].Id));
			Assert.That(table.Rows[4]["OrderLineId"], Is.EqualTo(order2.Items[0].Id));
			Assert.That(table.Rows[5]["DocumentLineId"], Is.EqualTo(document.Lines[3].Id));
			Assert.That(table.Rows[5]["OrderLineId"], Is.EqualTo(order2.Items[1].Id));
			Assert.That(table.Rows[6]["DocumentLineId"], Is.EqualTo(document.Lines[5].Id));
			Assert.That(table.Rows[6]["OrderLineId"], Is.EqualTo(order2.Items[2].Id));
		}

		private OrderHead BuildOrder()
		{
			OrderHead order1;
			order1 = new OrderHead {
				ClientCode = client.Id,
				Address = address,
				Price = Price.Find(price.Id)
			};
			return order1;
		}

		[Test(Description = "Тестирует ситуацию, когда файл накладной может появиться в директории с задержкой")]
		public void check_parse_waybill_if_file_is_not_local()
		{
			var file = "9229370.dbf";
			var log = new TestDocumentLog(supplier, testAddress, file);
			using (new TransactionScope())
				log.SaveAndFlush();

			var service = new WaybillService(); // файл накладной в нужной директории отсутствует
			var ids = service.ParseWaybill(new[] { log.Id });
			using (new SessionScope())
			{
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.ClientCode == client.Id).ToList();
				Assert.That(logs.Count(), Is.EqualTo(1));
				Assert.That(ids.Length, Is.EqualTo(0));
				// Проверяем наличие записей в documentheaders				
				Assert.That(Document.Queryable.Where(doc => doc.Log.Id == logs[0].Id).Count(), Is.EqualTo(0));
			}
			Thread thread = new Thread(() =>
			{
				Thread.Sleep(3000);
				File.Copy(@"..\..\Data\Waybills\9229370.dbf", Path.Combine(waybillsPath, String.Format("{0}_{1}({2}){3}", log.Id, 
					supplier.Name, Path.GetFileNameWithoutExtension(file), Path.GetExtension(file))));
			});
			thread.Start(); // подкладываем файл в процессе разбора накладной
			ids = service.ParseWaybill(new[] {log.Id});
			using (new SessionScope())
			{
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.ClientCode == client.Id).ToList();
				Assert.That(logs.Count(), Is.EqualTo(1));
				Assert.That(ids.Length, Is.EqualTo(1));
				Assert.That(Document.Queryable.Where(doc => doc.Log.Id == logs[0].Id).Count(), Is.EqualTo(1));
			}		
		}

		[Test(Description = "Тестирует сопоставление продукта и производителя позиции в накладной в случае, если позиция фармацевтика и в качестве производителя указан сторонний производитель")]
		public void resolve_product_and_producer_for_farmacie()
		{
			Document doc;
			TestProduct product1, product2, product3, product4;			
			TestProducer producer1, producer2, producer3;
			
			using(new SessionScope())
			{
				var order = new TestOrder();
	
				product1 = new TestProduct("Активированный уголь (табл.)");
				product1.CatalogProduct.Pharmacie = true;
				product1.CreateAndFlush();
				Thread.Sleep(1000);
				product2 = new TestProduct("Виагра (табл.)");
				product2.CatalogProduct.Pharmacie = true;
				product2.CreateAndFlush();
				Thread.Sleep(1000);
				product3 = new TestProduct("Крем для кожи (гель.)");
				product3.CatalogProduct.Pharmacie = false;
				product3.CreateAndFlush();
				Thread.Sleep(1000);
				product4 = new TestProduct("Эластичный бинт");
				product4.CatalogProduct.Pharmacie = false;
				product4.CreateAndFlush();		
				
				producer1 = new TestProducer("ВероФарм");
				producer1.CreateAndFlush();
				producer2 = new TestProducer("Пфайзер");
				producer2.CreateAndFlush();
				producer3 = new TestProducer("Воронежская Фармацевтическая компания");
				producer3.CreateAndFlush();

				new TestSynonym() {Synonym = "Активированный уголь", ProductId = product1.Id, PriceCode = (int?)price.Id}.CreateAndFlush();			
				new TestSynonym() {Synonym = "Виагра", ProductId = product2.Id, PriceCode = (int?)price.Id }.CreateAndFlush();				
				new TestSynonym() {Synonym = "Крем для кожи", ProductId = product3.Id, PriceCode = (int?) price.Id}.CreateAndFlush();
				new TestSynonym() { Synonym = "Эластичный бинт", ProductId = product4.Id, PriceCode = (int?)price.Id }.CreateAndFlush();

				new TestSynonymFirm() {Synonym = "ВероФарм", CodeFirmCr = (int?) producer1.Id, PriceCode = (int?) price.Id}.CreateAndFlush();
				new TestSynonymFirm() { Synonym = "Пфайзер", CodeFirmCr = (int?)producer1.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
				new TestSynonymFirm() { Synonym = "Пфайзер", CodeFirmCr = (int?)producer2.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
				new TestSynonymFirm() { Synonym = "Верофарм", CodeFirmCr = (int?)producer2.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
				new TestSynonymFirm() { Synonym = "ВоронежФарм", CodeFirmCr = (int?)producer3.Id, PriceCode = (int?)price.Id }.CreateAndFlush();

				TestAssortment.CheckAndCreate(product1, producer1);
				TestAssortment.CheckAndCreate(product2, producer2);

				var log = new DocumentReceiveLog() {
					Supplier = appSupplier,
					ClientCode = client.Id,
					Address = address,
					MessageUid = 123,
					DocumentSize = 100
				};

				doc = new Document(log) {
					OrderId = order.Id,
					Address = log.Address,
					DocumentDate = DateTime.Now
				};

				var line = doc.NewLine();
				line.Product = "Активированный уголь";
				line.Producer = "ВероФарм";

				line = doc.NewLine();
				line.Product = "Виагра";
				line.Producer = " Верофарм  ";

				line = doc.NewLine();
				line.Product = " КРЕМ ДЛЯ КОЖИ  ";
				line.Producer = "Тестовый производитель";

				line = doc.NewLine();
				line.Product = "эластичный бинт";
				line.Producer = "Воронежфарм";
			}

			doc.SetProductId();

			Assert.That(doc.Lines[0].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[0].ProductEntity.Id, Is.EqualTo(product1.Id));
			Assert.That(doc.Lines[0].ProducerId, Is.EqualTo(producer1.Id));
			Assert.That(doc.Lines[1].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[1].ProductEntity.Id, Is.EqualTo(product2.Id));
			Assert.That(doc.Lines[1].ProducerId, Is.EqualTo(producer2.Id));
			Assert.That(doc.Lines[2].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[2].ProductEntity.Id, Is.EqualTo(product3.Id));
			Assert.That(doc.Lines[2].ProducerId, Is.Null);
			Assert.That(doc.Lines[3].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[3].ProductEntity.Id, Is.EqualTo(product4.Id));
			Assert.That(doc.Lines[3].ProducerId, Is.EqualTo(producer3.Id));
		}

		[Test(Description = "разбор накладной с установленными файлами сертификатов")]
		public void ParseCertificateFiles()
		{
			var file = "9832937_Аптека-Холдинг(3334_1459366).dbf";
			var log = CreateTestLog(file);

			var service = new WaybillService(); // файл накладной в нужной директории отсутствует
			var ids = service.ParseWaybill(new[] { log.Id });

			using (new SessionScope())
			{
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.ClientCode == client.Id).ToList();
				Assert.That(logs.Count(), Is.EqualTo(1));
				Assert.That(ids.Length, Is.EqualTo(1));

				var docs = Document.Queryable.Where(doc => doc.Log.Id == logs[0].Id).ToList();
				Assert.That(docs.Count, Is.EqualTo(1));

				Assert.That(docs[0].Lines.ToList().TrueForAll(docLine => !String.IsNullOrEmpty(docLine.CertificateFilename)));
			}
		}
	}
}
