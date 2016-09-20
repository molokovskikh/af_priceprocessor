﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Castle.Components.DictionaryAdapter;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using NHibernate.Proxy;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using PriceProcessor.Test.Waybills.Parser;
using Test.Support;
using Test.Support.log4net;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
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
            var waybill = TestWaybill.Find(ids.Single());
            Assert.That(waybill.Lines.Count, Is.EqualTo(1));
        }

        private DocumentReceiveLog ParseFileForRedmine(string filename, bool createIssue = true,
            bool changeValues = true)
        {
            var addressRed =
                session.Query<Address>()
                    .FirstOrDefault(
                        s => (changeValues && s.Id != testAddress.Id) || (!changeValues && s.Id == testAddress.Id));
            var supplierRed =
                session.Query<Supplier>()
                    .FirstOrDefault(s => (changeValues && s.Id != supplier.Id) || (!changeValues && s.Id == supplier.Id));
            if (addressRed.Id != 0) {
                testAddress = new TestAddress() {Id = addressRed.Id};
            }
            if (supplier.Id != 0) {
                supplier = new TestSupplier() {Id = supplierRed.Id};
            }

            if (createIssue) {
                addressRed.Client.RedmineNotificationForUnresolved = true;
                session.Save(addressRed.Client);
            } else {
                addressRed.Client.RedmineNotificationForUnresolved = false;
                session.Save(addressRed.Client);
            }

            var log = new DocumentReceiveLog(supplierRed, addressRed) {
                FileName = filename
            };
            session.Save(log);
            var fi = new FileInfo(log.GetFileName());
            var str = fi.DirectoryName;
            if (!Directory.Exists(str)) {
                Directory.CreateDirectory(str);
            }
            File.Delete(fi.FullName);
            File.Copy(@"..\..\Data\Waybills\" + log.FileName, fi.FullName);

            var w = new WaybillService();
            var waybill = DocumentReceiveLog.Find(log.Id);
            w.Process(new EditableList<DocumentReceiveLog>() {waybill});
            session.Flush();
            return log;
        }

        private void Parse_waybillCleanRedmineIssueTable()
        {
            using (var sqlConnection =
                new MySqlConnection(ConnectionHelper.GetConnectionString())) {
                if (sqlConnection.ConnectionString.IndexOf("localhost") != -1) {
                    sqlConnection.Open();
                    var com = sqlConnection.CreateCommand();
                    com.CommandText = $"DELETE FROM redmine.issues ";
                    com.ExecuteScalar();
                    sqlConnection.Close();
                }
            }
        }

        [Test]
        public void Parse_waybillIssueForRedmine_NoIssueForSuccess()
        {
            Parse_waybillCleanRedmineIssueTable();
            var fileName = "1008fo.pd";
            var log = ParseFileForRedmine(fileName);
            var res = MetadataOfLog.GetMetaFromDataBaseCount(new MetadataOfLog(log).Hash);
            Assert.That(res, Is.EqualTo(0)); 
        }

        [Test]
        public void Parse_waybillIssueForRedmine_IssueFromOne()
        {
            Parse_waybillCleanRedmineIssueTable();
            string doubleTest = "";
            for (int i = 0; i < 2; i++) {
                var fileName = "1008foBroken.pd";
                var log = ParseFileForRedmine(fileName, changeValues: false);
                var res = MetadataOfLog.GetMetaFromDataBaseCount(new MetadataOfLog(log).Hash);
                //должен создаваться только один
                Assert.That(res, Is.EqualTo(1));
                //для одного и того же хэша
                if (doubleTest != string.Empty) {
                    Assert.That(doubleTest, Is.EqualTo(new MetadataOfLog(log).Hash));
                }
                doubleTest = new MetadataOfLog(log).Hash;
            }
        }

        [Test]
        public void Parse_waybillIssueForRedmine_IssueFromMany()
        {
            Parse_waybillCleanRedmineIssueTable();
            var doubleTest = "";
            for (int i = 0; i < 2; i++) {
                var fileName = "1008foBroken.pd";
                var log = ParseFileForRedmine(fileName);
                var res = MetadataOfLog.GetMetaFromDataBaseCount(new MetadataOfLog(log).Hash);
                //для разных хэшей создается по одной задаче
                Assert.That(res, Is.EqualTo(1));
                if (doubleTest != string.Empty) {
                    Assert.That(doubleTest, Is.Not.EqualTo(new MetadataOfLog(log).Hash));
                }
                doubleTest = new MetadataOfLog(log).Hash;
            }
        }

        [Test]
        public void Parse_waybillIssueForRedmine_NoIssueNoClientFlag()
        {
            Parse_waybillCleanRedmineIssueTable();
            //если не клиент не промаркерован, по его накладной задачу не создаем
            var nofileName = "1008foBroken.pd";
            var nolog = ParseFileForRedmine(nofileName, false);
            var nores = MetadataOfLog.GetMetaFromDataBaseCount(new MetadataOfLog(nolog).Hash);
            Assert.That(nores, Is.EqualTo(0));
        }

        [Test(Description = "тест разбора накладной с ShortName поставщика в имени файла")]
        public void Parse_waybill_with_ShortName_in_fileName()
        {
            var ids = ParseFile("1008fo.pd");

            var waybill = TestWaybill.Find(ids.Single());
            Assert.That(waybill.Lines.Count, Is.EqualTo(1));
        }

		[Test]
		public void Parse_waybill_without_header()
		{
			var ids = ParseFile("00000049080.sst");

			Assert.That(ids.Count(), Is.EqualTo(0));
		}

		[Test(Description = "Проверка сопоставления идентификатора продукта синониму. Синоним есть в БД")]
		public void Check_SetProductId_if_synonym_exists()
		{
			var file = "14356_4.dbf";

			var log = CreateTestLog(file);

			var product = new TestProduct("тестовый товар");
			product.SaveAndFlush();

			var productSynonym = new TestSynonym {
				ProductId = product.Id,
				Synonym = "Коринфар таб п/о 10мг № 50",
				PriceCode = (int?)price.Id
			};

			productSynonym.SaveAndFlush();

			productSynonym = new TestSynonym {
				ProductId = null,
				Synonym = "Коринфар таб п/о 10мг № 50",
				PriceCode = (int?)price.Id
			};

			var producer1 = new TestProducer {
				Name = "Тестовый производитель",
			};
			producer1.SaveAndFlush();

			var producer2 = new TestProducer {
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

			producerSynonym = new TestProducerSynonym {
				Price = price,
				Name = "Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ",
				Producer = producer2
			};
			producerSynonym.SaveAndFlush();
			FlushAndCommit();

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

			var waybill = TestWaybill.Find(ids.Single());
			Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			Assert.IsTrue(waybill.Lines[0].CatalogProduct != null);
			Assert.That(waybill.Lines[0].CatalogProduct.Id, Is.EqualTo(product.Id));
			Assert.That(waybill.Lines[0].ProducerId, Is.EqualTo(producer1.Id));
		}

		[Test(Description = "Проверка сопоставления идентификатора продукта синониму. Синонима нет в БД")]
		public void Check_SetProductId_if_synonym_not_exists()
		{
			var ids = ParseFile("14356_4.dbf");

			var waybill = TestWaybill.Find(ids.Single());
			Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			Assert.IsTrue(waybill.Lines[0].CatalogProduct == null);
			Assert.IsTrue(waybill.Lines[0].ProducerId == null);
		}

		[Test, Description("Парсинг накладной и проверка настройки IsConvertFormat для клиента. Настройка разрешает сохранение накладной в dbf формате.")]
		public void Parse_and_Convert_to_Dbf()
		{
			settings.IsConvertFormat = true;
			settings.AssortimentPriceId = Core.Queryable.First().Price.Id;
			settings.SaveAndFlush();

			ParseFile("14326_4.dbf");

			var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.ClientCode == client.Id);
			Assert.That(logs.Count(), Is.EqualTo(2));
			Assert.That(logs.Where(l => l.IsFake).Count(), Is.EqualTo(1));
			Assert.That(logs.Where(l => !l.IsFake).Count(), Is.EqualTo(1));

			// Проверяем наличие записей в documentheaders для исходных документов.
			foreach (var documentLog in logs) {
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

		[Test(Description = "Проверка сопоставления кода клиента по ассортиментному прайс листу")]
		public void Check_SetAssortimentInfo()
		{
			var file = "14356_4.dbf";

			var log = CreateTestLog(file);

			var product = new TestProduct("тестовый товар");
			product.SaveAndFlush();
			var productSynonym = new TestProductSynonym("Коринфар таб п/о 10мг № 50", product, price);
			productSynonym.SaveAndFlush();

			var producer = new TestProducer { Name = "Тестовый производитель" };
			producer.SaveAndFlush();

			var producerSynonym = new TestProducerSynonym { Price = price, Name = "Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ", Producer = producer };
			producerSynonym.SaveAndFlush();

			var core = new TestCore(productSynonym, producerSynonym) { Price = price, Code = "1234567", Quantity = "0", Period = "01.01.2015" };
			core.SaveAndFlush();

			core = new TestCore(productSynonym, producerSynonym) { Price = price, Code = "111111", Quantity = "0", Period = "01.01.2015" };
			core.SaveAndFlush();

			settings.IsConvertFormat = true;
			settings.AssortimentPriceId = price.Id;
			settings.SaveAndFlush();
			FlushAndCommit();

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

			var doc = Document.Find(ids.Single());
			Assert.That(doc.Lines.Count, Is.EqualTo(1));
			Assert.IsTrue(doc.Lines[0].ProductEntity != null);
			Assert.That(doc.Lines[0].ProductEntity.Id, Is.EqualTo(product.Id));
			Assert.That(doc.Lines[0].ProducerId, Is.EqualTo(producer.Id));

			var resultDoc = DocumentReceiveLog.Queryable.Single(d => d.Address.Id == address.Id && !d.IsFake);
			var files = Directory.GetFiles(waybillsPath, "*.dbf");
			Assert.That(files.Count(), Is.EqualTo(2), files.Implode());

			var data = Dbf.Load(resultDoc.GetFileName(), Encoding.GetEncoding(866));
			Assert.IsTrue(data.Columns.Contains("id_artis"));
			Assert.That(data.Rows[0]["id_artis"], Is.EqualTo("111111"));
			Assert.IsTrue(data.Columns.Contains("name_artis"));
			Assert.That(data.Rows[0]["name_artis"], Is.EqualTo("Коринфар таб п/о 10мг № 50"));
			Assert.IsTrue(data.Columns.Contains("przv_artis"));
			Assert.That(data.Rows[0]["przv_artis"], Is.EqualTo("Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ"));
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
			settings.IsConvertFormat = true;
			settings.AssortimentPriceId = price.Id;
			settings.SaveAndFlush();

			var ids = ParseFile("20101119_8055_250829.xml");

			var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.ClientCode == client.Id);
			Assert.That(logs.Count(), Is.EqualTo(2));
			Assert.That(logs.Where(l => l.IsFake).Count(), Is.EqualTo(1));
			Assert.That(logs.Where(l => !l.IsFake).Count(), Is.EqualTo(1));

			// Проверяем наличие записей в documentheaders для исходных документов.
			foreach (var documentLog in logs) {
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

		[Test]
		public void Document_invoice_test()
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

			var doc = new Document(log) {
				OrderId = order.Id,
				DocumentDate = DateTime.Now
			};
			var inv = doc.SetInvoice();
			inv.BuyerName = "Тестовый покупатель";

			log.Save();
			doc.Save();

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

		[Test(Description = "Пытаемся разобрать накладную от СИА с возможностью конвертации накладной, в результирующем файле конвертируемой накладной должны быть корректно выставлены коды сопоставленных позиций")]
		public void ConvertWaybillToDBFWithAssortmentCodes()
		{
			var doc = WaybillParser.Parse("9046752.DBF");
			settings.IsConvertFormat = true;
			settings.AssortimentPriceId = price.Id;
			settings.Save();
			var order = TestOrder.FindFirst();
			var address = Address.Find(client.Addresses[0].Id);
			var log = new DocumentReceiveLog {
				Supplier = Supplier.Find(supplier.Id),
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

			Exporter.ConvertIfNeeded(doc, WaybillSettings.Find(doc.ClientCode));

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

		[Test(Description = "Тестирует ситуацию, когда файл накладной может появиться в директории с задержкой")]
		public void check_parse_waybill_if_file_is_not_local()
		{
			var file = "9229370.dbf";
			var log = new TestDocumentLog(supplier, testAddress, file);
			session.Save(log);

			var service = new WaybillService(); // файл накладной в нужной директории отсутствует
			var ids = service.ParseWaybill(new[] { log.Id });
			using (new SessionScope()) {
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.ClientCode == client.Id).ToList();
				Assert.That(logs.Count(), Is.EqualTo(1));
				Assert.That(ids.Length, Is.EqualTo(0));
				// Проверяем наличие записей в documentheaders
				Assert.That(Document.Queryable.Count(doc => doc.Log.Id == logs[0].Id), Is.EqualTo(0));
			}
			var thread = new Thread(() => {
				Thread.Sleep(3000);
				File.Copy(@"..\..\Data\Waybills\9229370.dbf", Path.Combine(waybillsPath, String.Format("{0}_{1}({2}){3}", log.Id,
					supplier.Name, Path.GetFileNameWithoutExtension(file), Path.GetExtension(file))));
			});
			thread.Start(); // подкладываем файл в процессе разбора накладной
			ids = service.ParseWaybill(new[] { log.Id });
			using (new SessionScope()) {
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.ClientCode == client.Id).ToList();
				Assert.That(logs.Count(), Is.EqualTo(1));
				Assert.That(ids.Length, Is.EqualTo(1));
				Assert.That(Document.Queryable.Where(doc => doc.Log.Id == logs[0].Id).Count(), Is.EqualTo(1), "не нашли документа для {0}", logs[0].Id);
			}
		}

		[Test(Description = "Тестирует сопоставление продукта и производителя позиции в накладной в случае, если позиция фармацевтика и в качестве производителя указан сторонний производитель")]
		public void resolve_product_and_producer_for_farmacie()
		{
			var order = new TestOrder();

			var product1 = new TestProduct("Активированный уголь (табл.)");
			product1.CatalogProduct.Pharmacie = true;
			product1.CreateAndFlush();
			Thread.Sleep(100);
			var product2 = new TestProduct("Виагра (табл.)");
			product2.CatalogProduct.Pharmacie = true;
			product2.CreateAndFlush();
			Thread.Sleep(100);
			var product3 = new TestProduct("Крем для кожи (гель.)");
			product3.CatalogProduct.Pharmacie = false;
			product3.CreateAndFlush();
			Thread.Sleep(100);
			var product4 = new TestProduct("Эластичный бинт");
			product4.CatalogProduct.Pharmacie = false;
			product4.CreateAndFlush();
			Thread.Sleep(100);
			var product5 = new TestProduct("Стерильные салфетки");
			product5.CatalogProduct.Pharmacie = false;
			product5.CreateAndFlush();

			var producer1 = new TestProducer("ВероФарм");
			producer1.CreateAndFlush();
			var producer2 = new TestProducer("Пфайзер");
			producer2.CreateAndFlush();
			var producer3 = new TestProducer("Воронежская Фармацевтическая компания");
			producer3.CreateAndFlush();

			new TestSynonym() { Synonym = "Активированный уголь", ProductId = product1.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
			new TestSynonym() { Synonym = "Виагра", ProductId = product2.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
			new TestSynonym() { Synonym = "Крем для кожи", ProductId = product3.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
			new TestSynonym() { Synonym = "Эластичный бинт", ProductId = product4.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
			new TestSynonym() { Synonym = "Тестовый", ProductId = null, PriceCode = (int?)price.Id, SupplierCode = "12345" }.CreateAndFlush();
			new TestSynonym() { Synonym = "Тестовый2", ProductId = product5.Id, PriceCode = (int?)price.Id, SupplierCode = "12345" }.CreateAndFlush();

			new TestSynonymFirm() { Synonym = "ВероФарм", CodeFirmCr = (int?)producer1.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
			new TestSynonymFirm() { Synonym = "Пфайзер", CodeFirmCr = (int?)producer1.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
			new TestSynonymFirm() { Synonym = "Пфайзер", CodeFirmCr = (int?)producer2.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
			new TestSynonymFirm() { Synonym = "Верофарм", CodeFirmCr = (int?)producer2.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
			new TestSynonymFirm() { Synonym = "ВоронежФарм", CodeFirmCr = (int?)producer3.Id, PriceCode = (int?)price.Id }.CreateAndFlush();
			new TestSynonymFirm() { Synonym = "Тестовый", CodeFirmCr = null, PriceCode = (int?)price.Id, SupplierCode = "12345" }.CreateAndFlush();
			new TestSynonymFirm() { Synonym = "Тестовый2", CodeFirmCr = (int?)producer3.Id, PriceCode = (int?)price.Id, SupplierCode = "12345" }.CreateAndFlush();

			TestAssortment.CheckAndCreate(session, product1, producer1);
			TestAssortment.CheckAndCreate(session, product2, producer2);

			var log = new DocumentReceiveLog() {
				Supplier = appSupplier,
				ClientCode = client.Id,
				Address = address,
				MessageUid = 123,
				DocumentSize = 100
			};

			var doc = new Document(log) {
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

			line = doc.NewLine();
			line.Product = "Салфетки";
			line.Producer = "Воронежфарм";
			line.Code = "12345";

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
			Assert.That(doc.Lines[4].ProductEntity, Is.Null);
			Assert.That(doc.Lines[4].ProducerId, Is.Null);
		}
	}
}