using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor.Waybills;
using log4net.Config;
using NHibernate.Criterion;
using NUnit.Framework;
using Test.Support;
using Test.Support.Helpers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class WaybillServiceFixture
	{
		[Test]
		public void Parse_waybill()
		{
			var file = "1008fo.pd";
			var client = TestOldClient.CreateTestClient();
			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			var log = new TestDocumentLog {
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};

			using(new TransactionScope())
				log.SaveAndFlush();

			File.Copy(@"..\..\Data\Waybills\1008fo.pd", Path.Combine(waybillsPath, String.Format("{0}_1008fo.pd", log.Id)));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new [] {log.Id});

			using(new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}

		[Test(Description = "тест разбора накладной с ShortName поставщика в имени файла")]
		public void Parse_waybill_with_ShortName_in_fileName()
		{
			var file = "1008fo.pd";
			var client = TestOldClient.CreateTestClient();
			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
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

			var supplier = TestOldClient.Find(log.FirmCode);

			using (new TransactionScope())
				log.SaveAndFlush();

			File.Copy(
				@"..\..\Data\Waybills\1008fo.pd", 
				Path.Combine(waybillsPath, 
					String.Format(
						"{0}_{1}({2}){3}", 
						log.Id,
						supplier.ShortName,
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
			var client = TestOldClient.CreateTestClient();
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
			TestOldClient client;
			TestOldClient supplier;
			TestPrice price;
			TestDocumentLog log;
			TestProducer producer1;
			TestProducer producer2;
			TestProducer producer3;

			using (new SessionScope())
			{
				client = TestOldClient.CreateTestClient();
				supplier = TestOldClient.CreateTestSupplier();
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
				var productSynonym = new TestSynonym
				                     	{
				                     		ProductId = 12345,
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
			}

			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
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
				Assert.That(waybill.Lines[0].ProductId, Is.EqualTo(12345));
				Assert.That(waybill.Lines[0].ProducerId, Is.EqualTo(producer1.Id));

			}
		}

		[Test(Description = "Проверка сопоставления идентификатора продукта синониму. Синонима нет в БД")]
		public void Check_SetProductId_if_synonym_not_exists()
		{
			const string file = "14356_4.dbf";
			TestOldClient client;
			TestOldClient supplier;
			TestPrice price;
			TestDocumentLog log;
			TestSynonym productSynonym;

			using (new SessionScope())
			{
				client = TestOldClient.CreateTestClient();
				supplier = TestOldClient.CreateTestSupplier();
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

			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
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


		[Test, Ignore("Сопоставление в данной версии отключено")]
		public void Check_SetProductId()
		{
			const string file = "14326_4.dbf";
			var client = TestOldClient.CreateTestClient();
			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
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



		[Test, Ignore("Сопоставление в этой версии отключено")]
		public void Check_SetProducerId()
		{
			const string file = "14326_4.dbf";
			var client = TestOldClient.CreateTestClient();
			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
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
				
		}

	}
}
