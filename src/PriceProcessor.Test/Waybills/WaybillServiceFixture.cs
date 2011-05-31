using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Waybills;
using log4net.Config;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using Test.Support.Helpers;
using Common.Tools;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class WaybillServiceFixture
	{
		[Test]
		public void Parse_waybill()
		{
			var file = "1008fo.pd";
			//var client = TestOldClient.CreateTestClient();
			var client = TestClient.Create();
			//var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			/*var log = new TestDocumentLog {
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};*/

			var log = DocumentReceiveLog.Log(1179, client.Id, null, file, DocType.Waybill);

			
			/*using(new TransactionScope())
				log.SaveAndFlush();*/

			File.Copy(@"..\..\Data\Waybills\1008fo.pd", Path.Combine(waybillsPath, String.Format("{0}_1008fo.pd", log.Id)));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new [] {log.Id});

			using(new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}

		[Test]
		[Ignore]
		public void ParseDocument()
		{
			var file = "1008fo.pd";
			var client = TestClient.Create();
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			var log = DocumentReceiveLog.Log(1179, client.Id, null, file, DocType.Waybill);
			var settings = WaybillSettings.Queryable.Where(s => s.Id == client.Id).SingleOrDefault();
			using (new TransactionScope())
			{
				//settings.IsConvertFormat = true;				
				settings.ParseWaybills = true;
				settings.SaveAndFlush();
			}
			//var service = new WaybillService();
			WaybillService.ParserDocument(log);

		}

		[Test(Description = "тест разбора накладной с ShortName поставщика в имени файла")]
		public void Parse_waybill_with_ShortName_in_fileName()
		{
			var file = "1008fo.pd";
			//var client = TestOldClient.CreateTestClient();
			var client = TestClient.Create();
			//var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			var log = new TestDocumentLog
			{
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};

			//var supplier = TestOldClient.Find(log.FirmCode);
			var supplier = TestSupplier.Find(log.FirmCode);

			using (new TransactionScope())
				log.SaveAndFlush();

			File.Copy(
				@"..\..\Data\Waybills\1008fo.pd", 
				Path.Combine(waybillsPath, 
					String.Format(
						"{0}_{1}({2}){3}", 
						log.Id,
						supplier.Name,
						Path.GetFileNameWithoutExtension(file),
						Path.GetExtension(file))));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

			using (new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void Parse_waybill_without_header()
		{
			var file = "00000049080.sst";
			var client = TestClient.Create();
			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			var log = new TestDocumentLog
			{
				ClientCode = client.Id,
				FirmCode = 2207,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};

			using (new TransactionScope())
				log.SaveAndFlush();

			File.Copy(@"..\..\Data\Waybills\00000049080.sst", Path.Combine(waybillsPath, String.Format("{0}_00000049080.sst", log.Id)));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

			using (new SessionScope())
			{
				Assert.That(ids.Count(),Is.EqualTo(0));
			}
		}

		[Test(Description = "Проверка сопоставления идентификатора продукта синониму. Синоним есть в БД")]
		public void Check_SetProductId_if_synonym_exists()
		{
			const string file = "14356_4.dbf";
			TestClient client;
			TestSupplier supplier;
			TestPrice price;
			TestDocumentLog log;
			TestProducer producer1;
			TestProducer producer2;
			TestProduct product;

			using (new SessionScope())
			{
				client = TestClient.Create();
				supplier = TestSupplier.Create();
				price = new TestPrice	// прайс, по которому будут определяться синонимы
				{
					CostType = CostType.MultiColumn,
					Supplier = supplier,
					ParentSynonym = null,
					PriceName = "тестовый прайс"
				};
				price.SaveAndFlush();
				log = new TestDocumentLog
				{
					ClientCode = client.Id,
					FirmCode = supplier.Id,
					LogTime = DateTime.Now,
					DocumentType = DocumentType.Waybill,
					FileName = file,
				};
				log.SaveAndFlush();

				product = new TestProduct("тестовый товар");
				product.SaveAndFlush();

				var productSynonym = new TestSynonym
				                     	{
				                     		ProductId = (int?)product.Id,
				                     		Synonym = "Коринфар таб п/о 10мг № 50",
				                     		PriceCode = (int?) price.Id
				                     	};
				
				productSynonym.SaveAndFlush();

				productSynonym = new TestSynonym
				{
					ProductId = null,
					Synonym = "Коринфар таб п/о 10мг № 50",
					PriceCode = (int?)price.Id
				};



				producer1 = new TestProducer
				               	{
									Name = "Тестовый производитель",
				               	};
				producer1.SaveAndFlush();

				producer2 = new TestProducer
				{
					Name = "Тестовый производитель",
				};
				producer2.SaveAndFlush();

			
				var producerSynonym = new TestProducerSynonym
				                      	{
				                      		Price = price,
				                      		Name = "Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ",
				                      		Producer = null
										};
				producerSynonym.SaveAndFlush();

				producerSynonym = new TestProducerSynonym
				                      	{
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

			//var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");

			Directory.CreateDirectory(waybillsPath);
			File.Copy(@"..\..\Data\Waybills\14356_4.dbf", Path.Combine(waybillsPath, String.Format("{0}_14356_4.dbf", log.Id)));

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
			const string file = "14356_4.dbf";
			TestClient client;
			TestSupplier supplier;
			TestPrice price;
			TestDocumentLog log;
			TestSynonym productSynonym;

			using (new SessionScope())
			{
				client = TestClient.Create();
				supplier = TestSupplier.Create();
				price = new TestPrice
				{
					CostType = CostType.MultiColumn,
					Supplier = supplier,
					ParentSynonym = null,
					PriceName = "тестовый прайс"
				};
				price.SaveAndFlush();
				log = new TestDocumentLog
				{
					ClientCode = client.Id,
					FirmCode = supplier.Id,
					LogTime = DateTime.Now,
					DocumentType = DocumentType.Waybill,
					FileName = file,
				};
				log.SaveAndFlush();
			}

			//var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");

			Directory.CreateDirectory(waybillsPath);
			File.Copy(@"..\..\Data\Waybills\14356_4.dbf", Path.Combine(waybillsPath, String.Format("{0}_14356_4.dbf", log.Id)));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

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
			const string file = "14326_4.dbf";
			var client = TestClient.Create();
			//var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			
			Directory.CreateDirectory(waybillsPath);

			var log = new TestDocumentLog
			{
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};

			using (new TransactionScope())
				log.SaveAndFlush();

			File.Copy(@"..\..\Data\Waybills\14326_4.dbf", Path.Combine(waybillsPath, String.Format("{0}_14326_4.dbf", log.Id)));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

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
			const string file = "14326_4.dbf";
			var client = TestClient.Create();
			//var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");

			Directory.CreateDirectory(waybillsPath);

			var log = new TestDocumentLog
			{
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};

			using (new TransactionScope())
				log.SaveAndFlush();

			File.Copy(@"..\..\Data\Waybills\14326_4.dbf", Path.Combine(waybillsPath, String.Format("{0}_14326_4.dbf", log.Id)));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

			using (new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(4));
				Check_DocumentLine_SetProducerId(waybill);
			}
		}

		public void Check_DocumentLine_SetProducerId(TestWaybill document)
		{			
			var line = document.Lines[0];

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
			const string file = "14326_4.dbf";
			var client = TestClient.Create();
			var supplier = TestSupplier.Create();

			//var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");

			Directory.CreateDirectory(waybillsPath);

			var log = new TestDocumentLog
			{
				ClientCode = client.Id,				
				FirmCode = supplier.Id,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};

			using (new TransactionScope())
				log.SaveAndFlush();

			var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == client.Id).SingleOrDefault();
			using (new TransactionScope())
			{
				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = (int)Core.Queryable.First().Price.Id;
				settings.SaveAndFlush();
			}

			File.Copy(@"..\..\Data\Waybills\14326_4.dbf", Path.Combine(waybillsPath, String.Format("{0}_14326_4.dbf", log.Id)));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

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
			const string file = "14356_4.dbf";
			TestClient client;
			TestSupplier supplier;
			TestPrice price;
			TestDocumentLog log;			
			TestProducer producer;
			TestProduct product;

			using (new SessionScope())
			{
				client = TestClient.Create();
				supplier = TestSupplier.Create();
				price = new TestPrice{CostType = CostType.MultiColumn, Supplier = supplier, ParentSynonym = null, PriceName = "тестовый прайс"};
				price.SaveAndFlush();
				log = new TestDocumentLog{ClientCode = client.Id, FirmCode = supplier.Id, LogTime = DateTime.Now, DocumentType = DocumentType.Waybill, FileName = file,};
				log.SaveAndFlush();
				product = new TestProduct("тестовый товар");
				product.SaveAndFlush();
				var productSynonym = new TestProductSynonym("Коринфар таб п/о 10мг № 50", product, price);				
				productSynonym.SaveAndFlush();

				producer = new TestProducer{Name = "Тестовый производитель"};
				producer.SaveAndFlush();

				var producerSynonym = new TestProducerSynonym{ Price = price, Name = "Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ", Producer = producer };
				producerSynonym.SaveAndFlush();

				var core = new TestCore() { Price = price, Code = "1234567", ProductSynonym = productSynonym, ProducerSynonym = producerSynonym, Product = product, Producer = producer, Quantity = "0"};
				core.SaveAndFlush();
			}
			//var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			File.Copy(@"..\..\Data\Waybills\14356_4.dbf", Path.Combine(waybillsPath, String.Format("{0}_14356_4.dbf", log.Id)));
			var settings = WaybillSettings.Queryable.Where(s => s.Id == client.Id).SingleOrDefault();
			using (new TransactionScope())
			{
				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = (int?)price.Id;
				settings.SaveAndFlush();
			}
			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });
			using (new SessionScope())
			{
				Document doc = Document.Find(ids.Single());
				Assert.That(doc.Lines.Count, Is.EqualTo(1));
				Assert.IsTrue(doc.Lines[0].ProductId != null);
				Assert.That(doc.Lines[0].ProductId, Is.EqualTo(product.Id));
				Assert.That(doc.Lines[0].ProducerId, Is.EqualTo(producer.Id));
				var files_dbf = Directory.GetFiles(Path.Combine(docRoot, "Waybills"), "*.dbf");
				Assert.That(files_dbf.Count(), Is.EqualTo(2));
				var file_dbf = files_dbf.Where(f => f.Contains(supplier.Name)).Select(f => f).First();					
				var data = Dbf.Load(file_dbf, Encoding.GetEncoding(866));
				Assert.IsTrue(data.Columns.Contains("id_artis"));
				Assert.That(data.Rows[0]["id_artis"], Is.EqualTo("1234567"));
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
			const string file = "20101119_8055_250829.xml";
			var client = TestClient.Create();
			var supplier = TestSupplier.Create();			
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			var log = new TestDocumentLog
			{
				ClientCode = client.Id,
				FirmCode = supplier.Id,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};
			using (new TransactionScope())
				log.SaveAndFlush();
			var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == client.Id).SingleOrDefault();
			using (new TransactionScope())
			{
				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = (int)Core.Queryable.First().Price.Id;
				settings.SaveAndFlush();
			}
			File.Copy(@"..\..\Data\Waybills\20101119_8055_250829.xml", Path.Combine(waybillsPath, String.Format("{0}_20101119_8055_250829.xml", log.Id)));
			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });
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
	}
}
