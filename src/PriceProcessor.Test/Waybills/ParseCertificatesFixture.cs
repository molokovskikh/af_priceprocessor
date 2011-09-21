using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor;
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
using Test.Support.Documents;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class ParseCertificatesFixture
	{
		[Test(Description = "�������� �������� ������� ��� �������������� ������������")]
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
				SerialNumber = "������ ����� 1"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = firstProduct,
				SerialNumber = "������ ����� 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = "������ ����� 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = "������ ����� 2",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = string.Empty,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = "������ ����� 1",
				CertificateFilename = "cerFilename"
			});

			CertificateSourceDetector.DetectAndParse(document);

			Assert.That(document.Tasks.Count, Is.EqualTo(3));
			Assert.That(document.Tasks.TrueForAll(t => t.Supplier.Id == docSupplier.Id));

			var task = document.Tasks.OrderBy(t => t.CatalogProduct.Id).ThenBy(t => t.SerialNumber).ToList();
			Assert.That(task[0].CatalogProduct.Id == firstCatalog.Id && task[0].SerialNumber == "������ ����� 1");
			Assert.That(task[1].CatalogProduct.Id == firstCatalog.Id && task[1].SerialNumber == "������ ����� 2");
			Assert.That(task[2].CatalogProduct.Id == secondCatalog.Id && task[2].SerialNumber == "������ ����� 1");
		}

		[Test(Description = "�������� �������� ������� ��� �������������� ������������ ��� ������������� ������������")]
		public void CheckParseWithExistsCertificates()
		{
			var docSupplier = Supplier.Find(39u);
			var anotherSupplier = Supplier.Queryable.Where(s => s.Id != docSupplier.Id).First();

			var catalogs = Catalog.Queryable.Take(2).ToList().OrderBy(c => c.Id).ToList();
			var existsCatalog = catalogs[0];
			var nonExistCatalog = catalogs[1];
			var serialNumber = "������ ����� 5";

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
				SerialNumber = "������ ����� 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = serialNumber,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = "������ ����� 2",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = serialNumber,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = "������ ����� 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = "������ ����� 2",
				CertificateFilename = "cerFilename"
			});

			CertificateSourceDetector.DetectAndParse(document);

			Assert.That(document.Tasks.Count, Is.EqualTo(4));
			Assert.That(document.Tasks.TrueForAll(t => t.Supplier.Id == docSupplier.Id));

			var task = document.Tasks.OrderBy(t => t.CatalogProduct.Id).ThenBy(t => t.SerialNumber).ToList();
			Assert.That(task[0].CatalogProduct.Id == existsCatalog.Id && task[0].SerialNumber == "������ ����� 1");
			Assert.That(task[1].CatalogProduct.Id == existsCatalog.Id && task[1].SerialNumber == "������ ����� 2");
			Assert.That(task[2].CatalogProduct.Id == nonExistCatalog.Id && task[2].SerialNumber == "������ ����� 2");
			Assert.That(task[3].CatalogProduct.Id == nonExistCatalog.Id && task[3].SerialNumber == serialNumber);

			Assert.That(document.Lines[0].Certificate.Id, Is.EqualTo(existsCertificate.Id));
			Assert.That(document.Lines[4].Certificate.Id, Is.EqualTo(existsCertificate.Id));
		}

		private TestWaybillLine CreateBodyLine(uint supplierId, string serialNumber, TestProduct product)
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
				FirmCode = supplier.Id,
				DocumentType = DocumentType.Waybill,
				WriteTime = DateTime.Now
			};

			var documentLine = new TestWaybillLine {
				Waybill = document,
				SerialNumber = serialNumber,
				ProductId = product.Id
			};

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

		/// <summary>
		/// ������� ������, ������� �� ����� ����������
		/// </summary>
		private void DeleteNonProcessedTasks()
		{
			using (new TransactionScope()) {
				var certificates =
					CertificateTask.Queryable.Where(c => c.Supplier.Id != 39).ToList();
				certificates.ForEach(c => c.Delete());
			}
		}

		private void ProcessCertificatesWithLog(Action action)
		{
			try{

				var memoryAppender = new MemoryAppender();
				//memoryAppender.AddFilter(new LoggerMatchFilter { AcceptOnMatch = true, LoggerToMatch = "PrgData", Next = new DenyAllFilter() });
				BasicConfigurator.Configure(memoryAppender);

				try {

					action();

				}
				catch
				{
					var logEvents = memoryAppender.GetEvents();
					Console.WriteLine(
						"������ ��� ��������� ����� ������������:\r\n{0}", 
						logEvents.Select(item => {
							if (string.IsNullOrEmpty(item.GetExceptionString()))
								return item.RenderedMessage;
							else
								return item.RenderedMessage + Environment.NewLine + item.GetExceptionString();
						}).Implode("\r\n"));
					throw;
				}

				var events = memoryAppender.GetEvents();
				var errors = events.Where(item => item.Level >= Level.Warn);
				Assert.That(errors.Count(), Is.EqualTo(0), "��� ��������� ����� ������������ �������� ������:\r\n{0}", errors.Select(item => item.RenderedMessage).Implode("\r\n"));
			}
			finally
			{
				LogManager.ResetConfiguration();
			}
		}

		[Test(Description = "������� ����� ��������� ������ �����������")]
		public void SimpleCertificatesSourceHandler()
		{
			DeleteNonProcessedTasks();

			var supplier = Supplier.Find(39u);
			var serialNumber = Path.GetRandomFileName();
			var catalog = TestCatalogProduct.Queryable.First();
			var product = TestProduct.Queryable.First(p => p.CatalogProduct == catalog);

			var supplierCertificatesDir = Path.Combine(Settings.Default.FTPOptBoxPath, supplier.Id.ToString().PadLeft(3, '0'), "Certificats");
			if (Directory.Exists(supplierCertificatesDir))
				Directory.Delete(supplierCertificatesDir, true);
			Directory.CreateDirectory(supplierCertificatesDir);

			var destinationDir = Settings.Default.CertificatePath;

			var documentLine = CreateBodyLine(supplier.Id, serialNumber, product);

			var certificateFile = Path.GetRandomFileName();
			File.WriteAllText(Path.Combine(supplierCertificatesDir, certificateFile), "��� �������� ����������", Encoding.GetEncoding(1251));

			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var task = new CertificateTask();
			using (new TransactionScope()) {
				task.Supplier = supplier;
				task.CatalogProduct = Catalog.Find(catalog.Id);
				task.SerialNumber = serialNumber;
				task.DocumentLine = realDocumentLine;
				task.Create();

				documentLine.CertificateFilename = Path.GetFileNameWithoutExtension(certificateFile);
				documentLine.Save();
			}

			Assert.That(task.Id, Is.GreaterThan(0));

			ProcessCertificatesWithLog(() => { 
				var handler = new TestCertificateSourceHandler();

				handler.TestProcessData();
			});

			using (new TransactionScope()) {
				var processedTask = CertificateTask.Queryable.Where(t => t.Id == task.Id).FirstOrDefault();
				Assert.That(processedTask, Is.Null, "�� ���� ������� ������ �� �������� ����������� ����� ���������");

				var certificate =
					TestCertificate.Queryable.Where(c => c.CatalogProduct.Id == catalog.Id && c.SerialNumber == serialNumber).
						FirstOrDefault();
				Assert.That(certificate, Is.Not.Null, "�� ��� ������ ����������");
				Assert.That(certificate.CertificateFiles.Count, Is.EqualTo(1), "�� ���� ��������� ����� �����������");
				Assert.That(certificate.CertificateFiles[0].OriginFilename, Is.EqualTo(certificateFile), "�� ��������� ������������ ��� ����� �����������");
				Assert.That(certificate.CertificateFiles[0].Supplier.Id, Is.EqualTo(supplier.Id), "�� ��������� ��������� ����� �����������");

				Assert.That(File.Exists(Path.Combine(destinationDir, certificate.CertificateFiles[0].Id + ".tif")), "�� ���������� ���� �����������");
				Assert.That(!File.Exists(Path.Combine(supplierCertificatesDir, certificateFile)), "�� ������ ���� ����������� �� �������� �����");

				documentLine.Refresh();
				Assert.That(documentLine.Certificate.Id, Is.EqualTo(certificate.Id), "� ������� ��������� �� ����������� ������ �� ����������");
			}
		}
		
		[Test(Description = "����������� ������������ �� �� �������� �����"), Ignore("��� �� ����")]
		public void MigrateCertificates()
		{
			return;

			//var certificateFiles = CertificateFile.FindAll().ToList();
			//var distinctCertificateFiles =
			//    certificateFiles.GroupBy(c => c.OriginFilename).Where(g => g.Count() == 1).Select(g => g.First()).ToList();
			//Assert.That(certificateFiles.Count, Is.GreaterThan(0));

			//var sourcePath = @"\\adc.analit.net\Inforoom\FTP\OptBox";
			//var destinationPath = @"\\ADC.ANALIT.NET\Inforoom\WebApps\PrgDataService\Results\Certificates";

			//foreach (var distinctCertificateFile in distinctCertificateFiles) {
			//    var sourceFile = Path.Combine(
			//        sourcePath, 
			//        distinctCertificateFile.Supplier.Id.ToString().PadLeft(3, '0'),
			//        "Certificats", 
			//        distinctCertificateFile.OriginFilename);
			//    var destinationFile = Path.Combine(destinationPath, distinctCertificateFile.Id + ".tif");

			//    if (File.Exists(sourceFile)) {
			//        if (!File.Exists(destinationFile))
			//            File.Move(sourceFile, destinationFile);
			//    }
			//}
		}

	}
}