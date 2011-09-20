using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class ParseCertificatesFixture
	{
		[Test(Description = "проверка создани€ заданий дл€ несуществующих сертификатов")]
		public void CheckParse()
		{
			var docSupplier = Supplier.Find(39u);

			var firstCatalog = new Catalog {Id = 1, Name = "catalog1"};
			var secondCatalog = new Catalog {Id = 2, Name = "catalog2"};
			var firstProduct = new Product {Id = 3, CatalogProduct = firstCatalog};
			var secondProduct = new Product {Id = 4, CatalogProduct = firstCatalog};
			var thirdProduct = new Product {Id = 5, CatalogProduct = secondCatalog};

			var document = new Document {
				FirmCode = docSupplier.Id
			};
			document.NewLine(new DocumentLine{
				ProductEntity = firstProduct,
				SerialNumber = "крута€ сери€ 1"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = firstProduct,
				SerialNumber = "крута€ сери€ 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = "крута€ сери€ 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = "крута€ сери€ 2",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = string.Empty,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = "крута€ сери€ 1",
				CertificateFilename = "cerFilename"
			});

			CertificateSourceDetector.DetectAndParse(document);

			Assert.That(document.Tasks.Count, Is.EqualTo(3));
			Assert.That(document.Tasks.TrueForAll(t => t.Supplier.Id == docSupplier.Id));

			var task = document.Tasks.OrderBy(t => t.CatalogProduct.Id).ThenBy(t => t.SerialNumber).ToList();
			Assert.That(task[0].CatalogProduct.Id == firstCatalog.Id && task[0].SerialNumber == "крута€ сери€ 1");
			Assert.That(task[1].CatalogProduct.Id == firstCatalog.Id && task[1].SerialNumber == "крута€ сери€ 2");
			Assert.That(task[2].CatalogProduct.Id == secondCatalog.Id && task[2].SerialNumber == "крута€ сери€ 1");
		}

		[Test(Description = "проверка создани€ заданий дл€ несуществующих сертификатов при существовании сертификатов")]
		public void CheckParseWithExistsCertificates()
		{
			var docSupplier = Supplier.Find(39u);
			var anotherSupplier = Supplier.Queryable.Where(s => s.Id != docSupplier.Id).First();

			var catalogs = Catalog.Queryable.Take(2).ToList().OrderBy(c => c.Id).ToList();
			var existsCatalog = catalogs[0];
			var nonExistCatalog = catalogs[1];
			var serialNumber = "крута€ сери€ 5";

			var firstProduct = new Product {Id = 3, CatalogProduct = existsCatalog};
			var secondProduct = new Product {Id = 4, CatalogProduct = nonExistCatalog};
			var thirdProduct = new Product {Id = 5, CatalogProduct = existsCatalog};


			using (new TransactionScope()) {
				var certificates =
					Certificate.Queryable.Where(c => c.SerialNumber == serialNumber).ToList();
				certificates.ForEach(c => c.Delete());
			}

			var existsCertificate = new Certificate();
			using (new TransactionScope()) {
				existsCertificate.CatalogProduct = Catalog.Find(existsCatalog.Id);
				existsCertificate.SerialNumber = serialNumber;
				existsCertificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName(),
						Supplier = docSupplier
					}
				);
				existsCertificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName(),
						Supplier = docSupplier
					}
				);
				existsCertificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName(),
						Supplier = anotherSupplier
					}
				);
				existsCertificate.Create();
			}

			var document = new Document{
				FirmCode = docSupplier.Id
			};
			document.NewLine(new DocumentLine{
				ProductEntity = firstProduct,
				SerialNumber = serialNumber
			});
			document.NewLine(new DocumentLine{
				ProductEntity = firstProduct,
				SerialNumber = "крута€ сери€ 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = serialNumber,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = "крута€ сери€ 2",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = serialNumber,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = "крута€ сери€ 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = "крута€ сери€ 2",
				CertificateFilename = "cerFilename"
			});

			CertificateSourceDetector.DetectAndParse(document);

			Assert.That(document.Tasks.Count, Is.EqualTo(4));
			Assert.That(document.Tasks.TrueForAll(t => t.Supplier.Id == docSupplier.Id));

			var task = document.Tasks.OrderBy(t => t.CatalogProduct.Id).ThenBy(t => t.SerialNumber).ToList();
			Assert.That(task[0].CatalogProduct.Id == existsCatalog.Id && task[0].SerialNumber == "крута€ сери€ 1");
			Assert.That(task[1].CatalogProduct.Id == existsCatalog.Id && task[1].SerialNumber == "крута€ сери€ 2");
			Assert.That(task[2].CatalogProduct.Id == nonExistCatalog.Id && task[2].SerialNumber == "крута€ сери€ 2");
			Assert.That(task[3].CatalogProduct.Id == nonExistCatalog.Id && task[3].SerialNumber == serialNumber);

			Assert.That(document.Lines[0].Certificate.Id, Is.EqualTo(existsCertificate.Id));
			Assert.That(document.Lines[4].Certificate.Id, Is.EqualTo(existsCertificate.Id));
		}

		private TestWaybillLine CreateBodyLine(uint supplierId)
		{
			var supplier = (TestSupplier)TestSupplier.Find(supplierId);
			var user = TestUser.Queryable.First(u => u.AvaliableAddresses.Count > 0);

			var documentLog = new TestDocumentLog {
				FirmCode = supplier.Id,
				ClientCode = user.Client.Id,
				DocumentType = DocumentType.Waybill,
				LogTime = DateTime.Now,
				FileName = Path.GetRandomFileName() + ".txt"
			};

			var document = new TestWaybill {
				ClientCode = user.Client.Id,
				FirmCode = supplier.Supplier,
				DocumentType = DocumentType.Waybill,
				WriteTime = DateTime.Now
			};

			var documentLine = new TestWaybillLine();
			documentLine.Waybill = document;

			document.Lines = new List<TestWaybillLine>();
			document.Lines.Add(documentLine);

			using (new TransactionScope()) {
				documentLog.Create();
				document.DownloadId = documentLog.Id;
				document.Create();
				documentLine.Create();
			}

			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.Lines[0].Id, Is.GreaterThan(0));

			return documentLine;
		}

		public class TestCertificateSourceHandler : CertificateSourceHandler
		{

			public void TestProcessData()
			{
				ProcessData();
			}
		}

		[Test]
		public void CertificatesSourceHandler()
		{
			var supplier = Supplier.Find(39u);
			var documentLine = CreateBodyLine(supplier.Id);

			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = Path.GetRandomFileName();
			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var task = new CertificateTask();
			using (new TransactionScope()) {
				task.Supplier = supplier;
				task.CatalogProduct = Catalog.Find(catalog.Id);
				task.SerialNumber = serialNumber;
				task.DocumentLine = realDocumentLine;
				task.Create();
			}

			Assert.That(task.Id, Is.GreaterThan(0));


			try{

				var memoryAppender = new MemoryAppender();
				//memoryAppender.AddFilter(new LoggerMatchFilter { AcceptOnMatch = true, LoggerToMatch = "PrgData", Next = new DenyAllFilter() });
				BasicConfigurator.Configure(memoryAppender);


				try
				{

					var handler = new TestCertificateSourceHandler();

					handler.TestProcessData();

				}
				catch
				{
					var logEvents = memoryAppender.GetEvents();
					Console.WriteLine("ќшибки при подготовке данных:\r\n{0}", logEvents.Select(item =>
					{
						if (string.IsNullOrEmpty(item.GetExceptionString()))
							return item.RenderedMessage;
						else
							return item.RenderedMessage + Environment.NewLine + item.GetExceptionString();
					}).Implode("\r\n"));
					throw;
				}

				var events = memoryAppender.GetEvents();
				var errors = events.Where(item => item.Level >= Level.Warn);
				Assert.That(errors.Count(), Is.EqualTo(0), "ѕри подготовке данных возникли ошибки:\r\n{0}", errors.Select(item => item.RenderedMessage).Implode("\r\n"));
			}
			finally
			{
				LogManager.ResetConfiguration();
			}


		}

	}
}