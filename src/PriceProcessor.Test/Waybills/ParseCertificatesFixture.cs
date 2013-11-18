using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.Core;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using NUnit.Framework;
using Test.Support;
using Test.Support.Documents;
using Test.Support.Suppliers;
using log4net.Repository.Hierarchy;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class ParseCertificatesFixture : IntegrationFixture
	{
		private TestSupplier testSupplier;
		private Supplier supplier;

		[SetUp]
		public void Setup()
		{
			CertificateSource.Assembly = typeof(AptekaHoldingVoronezhCertificateSource).Assembly;

			testSupplier = TestSupplier.Create();
			supplier = Supplier.Find(testSupplier.Id);
		}

		[Test(Description = "проверка создания заданий для несуществующих сертификатов")]
		public void CheckParse()
		{
			var certificateSource = CreateRealSourceForSupplier(supplier);

			var firstCatalog = new Catalog { Id = 1, Name = "catalog1" };
			var secondCatalog = new Catalog { Id = 2, Name = "catalog2" };
			var firstProduct = new Product { Id = 3, CatalogProduct = firstCatalog };
			var secondProduct = new Product { Id = 4, CatalogProduct = firstCatalog };
			var thirdProduct = new Product { Id = 5, CatalogProduct = secondCatalog };

			var document = new Document {
				FirmCode = supplier.Id
			};
			document.NewLine(new DocumentLine {
				ProductEntity = firstProduct,
				SerialNumber = "крутая серия 1"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = firstProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = secondProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = secondProduct,
				SerialNumber = "крутая серия 2",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = thirdProduct,
				SerialNumber = string.Empty,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = thirdProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});

			CertificateSourceDetector.DetectAndParse(document);

			Assert.That(document.Tasks.Count, Is.EqualTo(5));
			Assert.That(document.Tasks.TrueForAll(t => t.CertificateSource.Id == certificateSource.Id));

			var task = document.Tasks.OrderBy(t => t.CatalogProduct.Id).ThenBy(t => t.SerialNumber).ToList();
			Assert.That(task[0].CatalogProduct.Id == firstCatalog.Id && task[0].SerialNumber == "крутая серия 1");
			Assert.AreEqual(firstCatalog.Id, task[2].CatalogProduct.Id);
			Assert.AreEqual("крутая серия 2", task[2].SerialNumber);

			Assert.AreEqual(secondCatalog.Id, task[3].CatalogProduct.Id);
			Assert.AreEqual("крутая серия 1", task[3].SerialNumber);

			Assert.AreEqual(secondCatalog.Id, task[4].CatalogProduct.Id);
			Assert.AreEqual(DocumentLine.EmptySerialNumber, task[4].SerialNumber);
		}

		private CertificateSource CreateSourceForSupplier(Supplier supplier, string sourceClassName)
		{
			var source = new CertificateSource {
				SourceClassName = sourceClassName
			};

			var deletedSource = CertificateSource.Queryable.FirstOrDefault(s => s.SourceClassName == sourceClassName);
			if (deletedSource != null)
				deletedSource.Delete();

			source.Suppliers = new List<Supplier>();
			source.Suppliers.Add(supplier);
			source.Save();

			return source;
		}

		private CertificateSource CreateRealSourceForSupplier(Supplier supplier)
		{
			var source = CreateSourceForSupplier(supplier, "AptekaHoldingVoronezhCertificateSource");
			source.FtpSupplier = supplier;
			source.Save();
			return source;
		}

		private CertificateSource CreateRandomSourceForSupplier(Supplier supplier)
		{
			return CreateSourceForSupplier(supplier, Path.GetRandomFileName());
		}

		[Test(Description = "проверка создания заданий для несуществующих сертификатов при существовании сертификатов")]
		public void CheckParseWithExistsCertificates()
		{
			var certificateSource = CreateRealSourceForSupplier(supplier);
			var anotherSupplier = Supplier.Queryable.First(s => s.Id != supplier.Id);
			var anotherSupplierSource = CreateRandomSourceForSupplier(anotherSupplier);

			var catalogs = Catalog.Queryable.Take(2).ToList().OrderBy(c => c.Id).ToList();
			var existsCatalog = catalogs[0];
			var nonExistCatalog = catalogs[1];
			var serialNumber = "крутая серия 5";

			var firstProduct = new Product { Id = 3, CatalogProduct = existsCatalog };
			var secondProduct = new Product { Id = 4, CatalogProduct = nonExistCatalog };
			var thirdProduct = new Product { Id = 5, CatalogProduct = existsCatalog };

			var certificates =
				Certificate.Queryable.Where(c => c.SerialNumber == serialNumber).ToList();
			certificates.ForEach(c => c.Delete());
			session.Flush();

			var existsCertificate = new Certificate();
			existsCertificate.CatalogProduct = Catalog.Find(existsCatalog.Id);
			existsCertificate.SerialNumber = serialNumber;
			existsCertificate.NewFile(
				new CertificateFile {
					OriginFilename = Path.GetRandomFileName(),
					Extension = ".tif",
					CertificateSource = certificateSource
				});
			existsCertificate.NewFile(
				new CertificateFile {
					OriginFilename = Path.GetRandomFileName(),
					Extension = ".tif",
					CertificateSource = certificateSource
				});
			existsCertificate.NewFile(
				new CertificateFile {
					OriginFilename = Path.GetRandomFileName(),
					Extension = ".tif",
					CertificateSource = anotherSupplierSource
				});
			existsCertificate.Save();

			var document = new Document {
				FirmCode = supplier.Id
			};
			document.NewLine(new DocumentLine {
				ProductEntity = firstProduct,
				SerialNumber = serialNumber
			});
			document.NewLine(new DocumentLine {
				ProductEntity = firstProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = secondProduct,
				SerialNumber = serialNumber,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = secondProduct,
				SerialNumber = "крутая серия 2",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = thirdProduct,
				SerialNumber = serialNumber,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = thirdProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = thirdProduct,
				SerialNumber = "крутая серия 2",
				CertificateFilename = "cerFilename"
			});

			CertificateSourceDetector.DetectAndParse(document);

			Assert.That(document.Tasks.Count, Is.EqualTo(5));
			Assert.That(document.Tasks.TrueForAll(t => t.CertificateSource.Id == certificateSource.Id));

			var task = document.Tasks.OrderBy(t => t.CatalogProduct.Id).ThenBy(t => t.SerialNumber).ToList();
			Assert.That(task[0].CatalogProduct.Id == existsCatalog.Id && task[0].SerialNumber == "крутая серия 1");
			Assert.AreEqual(existsCatalog.Id, task[2].CatalogProduct.Id);
			Assert.AreEqual("крутая серия 2", task[2].SerialNumber);

			Assert.AreEqual(nonExistCatalog.Id, task[3].CatalogProduct.Id);
			Assert.AreEqual("крутая серия 2", task[3].SerialNumber);
			Assert.That(task[4].CatalogProduct.Id == nonExistCatalog.Id && task[4].SerialNumber == serialNumber);

			Assert.That(document.Lines[0].Certificate.Id, Is.EqualTo(existsCertificate.Id));
			Assert.That(document.Lines[4].Certificate.Id, Is.EqualTo(existsCertificate.Id));
		}

		private TestWaybillLine CreateBodyLine(string serialNumber = null, TestProduct product = null)
		{
			var user = TestUser.Queryable.First(u => u.AvaliableAddresses.Count > 0);

			if (product == null) {
				product = TestProduct.Queryable.First();
			}

			var documentLog = new TestDocumentLog(testSupplier, user.Client) {
				FileName = Path.GetRandomFileName() + ".txt"
			};

			var document = new TestWaybill(documentLog);

			var documentLine = new TestWaybillLine {
				Waybill = document,
				SerialNumber = serialNumber,
				CatalogProduct = product
			};

			document.Lines = new List<TestWaybillLine>();
			document.Lines.Add(documentLine);

			document.Save();

			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.Lines[0].Id, Is.GreaterThan(0));

			return documentLine;
		}

		public class TestCertificateSourceHandler : CertificateSourceHandler
		{
			public Func<CertificateTask, CertificateSource> OnSource { get; set; }

			protected override CertificateSource DetectSource(CertificateTask certificateTask)
			{
				if (OnSource != null)
					return OnSource(certificateTask);
				return base.DetectSource(certificateTask);
			}
		}

		/// <summary>
		/// удаляем задачи, которые не будут обработаны
		/// </summary>
		private void Clean()
		{
			var certificates = CertificateTask.FindAll().ToList();
			certificates.ForEach(c => c.Delete());
		}

		private void DeleteCertificatedWithEmptySerialNumber()
		{
			//удаляем созданные сертификаты для пустых серий
			var certificates = Certificate.Queryable.Where(c => c.SerialNumber == DocumentLine.EmptySerialNumber).ToList();

			certificates.ForEach(c => session.CreateSQLQuery(@"
update
documents.DocumentBodies db
set
db.CertificateId = null
where
db.CertificateId = :certificateId;
delete from documents.Certificates where Id = :certificateId;
")
				.SetParameter("certificateId", c.Id)
				.ExecuteUpdate());
		}

		private void ProcessCertificatesWithLog(Action action)
		{
			if (session.Transaction.IsActive)
				session.Transaction.Commit();

			try {
				var memoryAppender = new MemoryAppender();
				memoryAppender.AddFilter(new LoggerMatchFilter { AcceptOnMatch = true, LoggerToMatch = "PriceProcessor", Next = new DenyAllFilter() });
				BasicConfigurator.Configure(memoryAppender);

				try {
					action();
				}
				catch {
					var logEvents = memoryAppender.GetEvents();
					Console.WriteLine(
						"Ошибки при обработки задач сертификатов:\r\n{0}",
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
				Assert.That(errors.Count(), Is.EqualTo(0), "При обработки задач сертификатов возникли ошибки:\r\n{0}", errors.Select(item => item.RenderedMessage).Implode("\r\n"));
			}
			finally {
				LogManager.ResetConfiguration();
			}
		}

		[Test(Description = "простой вызов обработки задачи сертификата")]
		public void SimpleCertificatesSourceHandler()
		{
			Clean();

			string supplierCertificatesDir;
			string certificateFile;
			var task = SuccessProcessCertificate(out supplierCertificatesDir, out certificateFile);

			var processedTask = CertificateTask.Queryable.FirstOrDefault(t => t.Id == task.Id);
			Assert.That(processedTask, Is.Null, "Не была удалена задача на создание сертификата после обработки");

			var certificate =
				TestCertificate.Queryable.FirstOrDefault(c => c.CatalogProduct.Id == task.CatalogProduct.Id && c.SerialNumber == task.SerialNumber);
			Assert.That(certificate, Is.Not.Null, "Не был создан сертификат");
			Assert.That(certificate.CertificateFiles.Count, Is.EqualTo(1), "Не были добавлены файлы сертификата");
			Assert.That(certificate.CertificateFiles[0].OriginFilename, Is.EqualTo(certificateFile), "Не совпадает оригинальное имя файла сертификата");
			Assert.That(certificate.CertificateFiles[0].CertificateSource.Id, Is.EqualTo(task.CertificateSource.Id), "Не совпадает источник файла сертификата");
			Assert.IsNotNullOrEmpty(certificate.CertificateFiles[0].ExternalFileId, "Не установлено поле ExternalFileId");
			Assert.That(certificate.CertificateFiles[0].ExternalFileId, Is.EqualTo(certificate.CertificateFiles[0].OriginFilename), "Поле ExternalFileId не совпадает с OriginFilename (только для AptekaHoldingVoronezhCertificateSource)");

			var file = Path.Combine(Settings.Default.CertificatePath, certificate.CertificateFiles[0].Id + ".tif");
			Assert.That(File.Exists(file), "Не скопирован файл {0} сертификата", file);
			Assert.That(File.Exists(Path.Combine(supplierCertificatesDir, certificateFile)), "Удален файл сертификата из исходной папки");

			session.Refresh(task.DocumentLine);
			Assert.That(task.DocumentLine.Certificate.Id, Is.EqualTo(certificate.Id), "В позиции документа не установлена ссылка на сертификат");
		}

		[Test(Description = "Проверка корректного определения источника")]
		public void DetectSourceParser()
		{
			var certificateSource = CreateRealSourceForSupplier(supplier);

			var serialNumber = Path.GetRandomFileName();
			var catalog = TestCatalogProduct.Queryable.First();
			var product = TestProduct.Queryable.First(p => p.CatalogProduct == catalog);

			var documentLine = CreateBodyLine(serialNumber, product);

			var realDocument = Document.Find(documentLine.Waybill.Id);

			var source = CertificateSourceDetector.DetectSource(realDocument);

			Assert.That(source, Is.Not.Null);
			Assert.That(source.Id, Is.EqualTo(certificateSource.Id));
			Assert.That(source.CertificateSourceParser, Is.InstanceOf<AptekaHoldingVoronezhCertificateSource>());
		}

		[Test(Description = "Проверка использования одного и того же файла в разных сертификатах")]
		public void UseExistsCertificateFiles()
		{
			Clean();

			var certificateSource = CreateRealSourceForSupplier(supplier);

			var destinationDir = Settings.Default.CertificatePath;

			//Удаляем файлы, которые имеют Id = 0
			var zeroFiles = Directory.GetFiles(destinationDir, "0.*");
			foreach (var zeroFile in zeroFiles) {
				File.Delete(zeroFile);
			}

			//Создаем существующий файл сертификата
			var existsSerialNumber = Path.GetRandomFileName();
			var existsFileId = Path.GetRandomFileName();
			var existsCertificateCatalog = TestCatalogProduct.Queryable.First();

			var existsCertificate = new Certificate() {
				CatalogProduct = Catalog.Find(existsCertificateCatalog.Id),
				SerialNumber = existsSerialNumber
			};
			var existsCertificateFile = existsCertificate.NewFile();
			existsCertificateFile.CertificateSource = certificateSource;
			existsCertificateFile.OriginFilename = existsFileId;
			existsCertificateFile.ExternalFileId = existsFileId;
			existsCertificateFile.Extension = ".tif";
			existsCertificate.Save();

			File.WriteAllText(Path.Combine(destinationDir, existsCertificateFile.Id + ".tif"), "Это тестовый сертификат", Encoding.GetEncoding(1251));

			var serialNumber = Path.GetRandomFileName();
			var catalog = TestCatalogProduct.Queryable.First(catalogProduct => catalogProduct.Id != existsCertificateCatalog.Id);
			var product = TestProduct.Queryable.First(p => p.CatalogProduct == catalog);

			var supplierCertificatesDir = Path.Combine(Settings.Default.FTPOptBoxPath, supplier.Id.ToString().PadLeft(3, '0'), "Certificats");
			if (Directory.Exists(supplierCertificatesDir))
				Directory.Delete(supplierCertificatesDir, true);
			Directory.CreateDirectory(supplierCertificatesDir);


			var documentLine = CreateBodyLine(serialNumber, product);

			//Файл сертификата в новой разбираемой позиции должен быть таким же
			var certificateFile = existsFileId;
			File.WriteAllText(Path.Combine(supplierCertificatesDir, certificateFile), "Это тестовый сертификат", Encoding.GetEncoding(1251));

			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var task = new CertificateTask(certificateSource, realDocumentLine);
			task.Save();

			documentLine.CertificateFilename = Path.GetFileNameWithoutExtension(certificateFile);
			documentLine.Save();

			Assert.That(task.Id, Is.GreaterThan(0));

			ProcessTestHandler();

			var processedTask = CertificateTask.Queryable.FirstOrDefault(t => t.Id == task.Id);
			Assert.That(processedTask, Is.Null, "Не была удалена задача на создание сертификата после обработки");

			var certificate =
				TestCertificate.Queryable.FirstOrDefault(c => c.CatalogProduct.Id == catalog.Id && c.SerialNumber == serialNumber);
			Assert.That(certificate, Is.Not.Null, "Не был создан сертификат");
			Assert.That(certificate.CertificateFiles.Count, Is.EqualTo(1), "Не были добавлены файлы сертификата");
			Assert.That(certificate.CertificateFiles[0].OriginFilename, Is.EqualTo(certificateFile), "Не совпадает оригинальное имя файла сертификата");
			Assert.That(certificate.CertificateFiles[0].CertificateSource.Id, Is.EqualTo(certificateSource.Id), "Не совпадает источник файла сертификата");
			Assert.IsNotNullOrEmpty(certificate.CertificateFiles[0].ExternalFileId, "Не установлено поле ExternalFileId");
			Assert.That(certificate.CertificateFiles[0].ExternalFileId, Is.EqualTo(certificate.CertificateFiles[0].OriginFilename), "Поле ExternalFileId не совпадает с OriginFilename (только для AptekaHoldingVoronezhCertificateSource)");

			Assert.That(File.Exists(Path.Combine(destinationDir, certificate.CertificateFiles[0].Id + ".tif")), "Не скопирован файл сертификата");
			Assert.That(File.Exists(Path.Combine(supplierCertificatesDir, certificateFile)), "Удален файл сертификата из исходной папки");

			documentLine.Refresh();
			Assert.That(documentLine.Certificate.Id, Is.EqualTo(certificate.Id), "В позиции документа не установлена ссылка на сертификат");

			Assert.That(certificate.CertificateFiles[0].Id, Is.EqualTo(existsCertificateFile.Id), "Не совпадает Id на существующий файл сертификата");
			Assert.That(certificate.CertificateFiles[0].Certificates.Count, Is.EqualTo(2), "Неожидаемое кол-во связанных сертификатов");

			var filesByExternalFileId = CertificateFile.Queryable.Where(f => f.ExternalFileId == existsFileId).ToList();
			Assert.That(filesByExternalFileId.Count, Is.EqualTo(1), "Имеются повторения файлов сертификатов по ExternalFileId: {0}", existsFileId);

			var zeroFilesAfteParse = Directory.GetFiles(destinationDir, "0.*");
			Assert.That(zeroFilesAfteParse.Length, Is.EqualTo(0), "Имеются файлы с Id = 0: {0}", zeroFilesAfteParse.Implode());
		}

		[Test(Description = "проверка доступа к объекту CertificateTask после удаления")]
		public void AccessToDeletedTask()
		{
			Clean();

			var certificateSource = CreateRealSourceForSupplier(supplier);
			var serialNumber = Path.GetRandomFileName();
			var catalog = TestCatalogProduct.Queryable.First();
			var product = TestProduct.Queryable.First(p => p.CatalogProduct == catalog);

			var documentLine = CreateBodyLine(serialNumber, product);

			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var task = new CertificateTask(certificateSource, realDocumentLine);
			task.Save();

			Assert.That(task.Id, Is.GreaterThan(0));
			task.Delete();

			Assert.That(task.Id, Is.GreaterThan(0));
			Assert.That(task.CertificateSource, Is.Not.Null);
			Assert.That(task.CatalogProduct, Is.Not.Null);
			Assert.IsNotNullOrEmpty(task.SerialNumber);

			var deletedTask = CertificateTask.Queryable.FirstOrDefault(t => t.Id == task.Id);
			Assert.That(deletedTask, Is.Null);
			Assert.IsNotNullOrEmpty(task.SerialNumber);
		}


		public class TestErrorSource : ICertificateSource
		{
			public bool CertificateExists(DocumentLine line)
			{
				return !String.IsNullOrEmpty(line.SerialNumber);
			}

			public IList<CertificateFile> GetCertificateFiles(CertificateTask task)
			{
				throw new Exception("Возникла ошибка при обработке задачи: {0}".Format(task));
			}
		}

		public class TestSuccessSource : ICertificateSource
		{
			public bool CertificateExists(DocumentLine line)
			{
				return !String.IsNullOrEmpty(line.SerialNumber);
			}

			public IList<CertificateFile> GetCertificateFiles(CertificateTask task)
			{
				var list = new List<CertificateFile>();

				var tempFile = Path.GetTempFileName();
				list.Add(new CertificateFile(tempFile, Path.GetFileNameWithoutExtension(tempFile), Path.GetFileName(tempFile), task.CertificateSource));

				return list;
			}
		}

		[Test(Description = "проверяем удаление задач сертификатов при возникновении ошибок при обработки")]
		public void SendErrorsOnProcessTask()
		{
			Clean();

			var certificateSource = CreateSourceForSupplier(supplier, typeof(TestErrorSource).Name);

			var serialNumber = Path.GetRandomFileName();
			var catalog = TestCatalogProduct.Queryable.First();
			var product = TestProduct.Queryable.First(p => p.CatalogProduct == catalog);

			var documentLine = CreateBodyLine(serialNumber, product);

			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var task = new CertificateTask(certificateSource, realDocumentLine);
			task.Save();

			Assert.That(task.Id, Is.GreaterThan(0));

			try {
				session.Transaction.Commit();

				((Logger)LogManager.GetLogger(typeof(TestCertificateSourceHandler)).Logger).Level = Level.Warn;
				var memoryAppender = new MemoryAppender();
				memoryAppender.ActivateOptions();
				memoryAppender.AddFilter(new LoggerMatchFilter { AcceptOnMatch = true, LoggerToMatch = "PriceProcessor", Next = new DenyAllFilter() });
				BasicConfigurator.Configure(memoryAppender);

				var handler = new TestCertificateSourceHandler();

				handler.OnSource = (c) => {
					c.CertificateSource.CertificateSourceParser = new TestErrorSource();
					return c.CertificateSource;
				};

				//Обрабатываем задачу первый раз
				handler.ProcessData();
				CheckErrors(task, memoryAppender, handler, 1);

				//Создаем новую задачу с теми же параметрами и обрабатываем задачу второй раз
				task = new CertificateTask(certificateSource, realDocumentLine);
				SaveAndProcess(task, handler);
				CheckErrors(task, memoryAppender, handler, 2);

				//Создаем новую задачу с теми же параметрами и обрабатываем задачу третий раз
				task = new CertificateTask(certificateSource, realDocumentLine);

				SaveAndProcess(task, handler);
				CheckErrors(task, memoryAppender, handler, 3);

				var lastEvents = memoryAppender.GetEvents();
				var errors = lastEvents.Where(item => item.Level >= Level.Warn).ToList();
				Assert.That(errors.Count, Is.EqualTo(3));
				//Последнее сообщение должно быть Error
				Assert.That(errors[2].Level, Is.EqualTo(Level.Error));

				//Успешно обработываем сертификат и список ошибок должен очистится
				handler.OnSource = (c) => {
					c.CertificateSource.CertificateSourceParser = new TestSuccessSource();
					return c.CertificateSource;
				};
				certificateSource.SourceClassName = typeof(TestSuccessSource).Name;
				certificateSource.Save();
				//Создаем новую задачу с теми же параметрами и обрабатываем задачу третий раз
				task = new CertificateTask(certificateSource, realDocumentLine);
				SaveAndProcess(task, handler);

				Assert.That(errors.Count, Is.EqualTo(3));
				Assert.That(handler.Errors.Count, Is.EqualTo(0), handler.Errors.Implode());

				//Задача должна быть удалена из базы данных
				var deletedSuccessTask = CertificateTask.Queryable.FirstOrDefault(t => t.Id == task.Id);
				Assert.That(deletedSuccessTask, Is.Null);
			}
			finally {
				LogManager.ResetConfiguration();
			}
		}

		private void SaveAndProcess(CertificateTask task, TestCertificateSourceHandler handler)
		{
			if (!session.Transaction.IsActive)
				session.BeginTransaction();

			task.Save();
			session.Transaction.Commit();
			session.Flush();
			handler.ProcessData();
		}

		private void CheckErrors(CertificateTask task, MemoryAppender memoryAppender, TestCertificateSourceHandler handler, uint errorCount, bool idEquals = false)
		{
			var firstEvents = memoryAppender.GetEvents();

			var firstErrors = firstEvents.Where(item => item.Level >= Level.Warn);
			Assert.That(firstErrors.Count(), Is.EqualTo(errorCount), firstErrors.Implode(e => e.MessageObject));

			//кол-во ошибок должно быть равно 1
			Assert.That(handler.Errors.Count, Is.EqualTo(1));

			var info = handler.Errors[task.GetErrorId()];
			Assert.That(info.ErrorCount, Is.EqualTo(errorCount));
			Assert.That(info.Exception.Message, Is.StringStarting("Возникла ошибка при обработке задачи: "));
			if (idEquals)
				Assert.That(info.Task.Id, Is.EqualTo(task.Id));
			Assert.That(info.Task.CertificateSource.Id, Is.EqualTo(task.CertificateSource.Id));
			Assert.That(info.Task.CatalogProduct.Id, Is.EqualTo(task.CatalogProduct.Id));
			Assert.That(info.Task.SerialNumber, Is.EqualTo(task.SerialNumber));

			//Задача должна быть удалена из базы данных
			var deletedTask = CertificateTask.Queryable.FirstOrDefault(t => t.Id == task.Id);
			Assert.That(deletedTask, Is.Null);
		}


		public class TestCertifcateSource : AbstractCertifcateSource
		{
			public List<string> LocalFiles = new List<string>();
			public static Func<DocumentLine, bool> CertificateExistsAction;
			public static Action<CertificateTask, IList<CertificateFile>> GetFilesFromSourceAction;

			public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
			{
				if (GetFilesFromSourceAction != null) {
					GetFilesFromSourceAction(task, files);
					return;
				}

				files.Add(new CertificateFile(Path.GetTempFileName(), "1"));
				files.Add(new CertificateFile(Path.GetTempFileName(), "1"));
				files.Each(f => {
					LocalFiles.Add(f.LocalFile);
					if (!File.Exists(f.LocalFile))
						File.WriteAllText(f.LocalFile, "this is test text");
				});

				throw new NotImplementedException();
			}

			public override bool CertificateExists(DocumentLine line)
			{
				if (CertificateExistsAction == null)
					throw new NotImplementedException();
				else
					return CertificateExistsAction(line);
			}

			protected List<CertificateSourceCatalog> GetSourceCatalog(uint catalogId, string serialNumber)
			{
				var name = GetType().Name;
				return CertificateSourceCatalog.Queryable
					.Where(
					c => c.CertificateSource.SourceClassName == name
						&& c.SerialNumber == serialNumber
						&& c.CatalogProduct.Id == catalogId)
					.ToList();
			}
		}

		[Test(Description = "Проверяем удаление локальных файлов при ошибке в CertifcateSource")]
		public void DeleteLocalFilesOnError()
		{
			var source = new TestCertifcateSource();
			var task = new CertificateTask();

			try {
				source.GetCertificateFiles(task);
				Assert.Fail("Не возникло исключение NotImplementedException");
			}
			catch (NotImplementedException) {
			}

			Assert.That(source.LocalFiles.TrueForAll(f => !File.Exists(f)), "Не должно существовать локальных файлов");
		}

		[Test(Description = "проверка создания заданий для пустого серийного номера")]
		public void ParseEmptySerialNumber()
		{
			DeleteCertificatedWithEmptySerialNumber();

			var catalogProduct = new Catalog { Id = 1, Name = "catalog1" };

			var testSupplier = TestSupplier.Create();
			var docSupplier = Supplier.Find(testSupplier.Id);
			var certificateSource = CreateRealSourceForSupplier(docSupplier);

			var product = new Product { Id = 3, CatalogProduct = catalogProduct };

			var document = new Document {
				FirmCode = docSupplier.Id
			};
			document.NewLine(new DocumentLine {
				ProductEntity = product,
				SerialNumber = null,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = product,
				SerialNumber = "",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = product,
				SerialNumber = "  ",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = product,
				SerialNumber = "-",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine {
				ProductEntity = product,
				SerialNumber = "  - ",
				CertificateFilename = "cerFilename"
			});

			CertificateSourceDetector.DetectAndParse(document);

			Assert.That(document.Tasks.Count, Is.EqualTo(5));
			Assert.That(document.Tasks.TrueForAll(t => t.CertificateSource.Id == certificateSource.Id));

			var task = document.Tasks[0];
			Assert.That(task.CatalogProduct.Id == catalogProduct.Id && task.SerialNumber == DocumentLine.EmptySerialNumber);
		}

		[Test(Description = "создаем сертификат для пустой серии (SerialNumber)")]
		public void CreateCertificateOnEmptySerialNumber()
		{
			Clean();
			DeleteCertificatedWithEmptySerialNumber();

			var catalog = TestCatalogProduct.Queryable.First();

			var certificateSource = CreateRealSourceForSupplier(supplier);
			var product = TestProduct.Queryable.First(p => p.CatalogProduct == catalog);

			var supplierCertificatesDir = Path.Combine(Settings.Default.FTPOptBoxPath, supplier.Id.ToString().PadLeft(3, '0'), "Certificats");
			if (Directory.Exists(supplierCertificatesDir))
				Directory.Delete(supplierCertificatesDir, true);
			Directory.CreateDirectory(supplierCertificatesDir);

			var destinationDir = Settings.Default.CertificatePath;

			var documentLine = CreateBodyLine(String.Empty, product);

			var certificateFile = Path.GetRandomFileName();
			File.WriteAllText(Path.Combine(supplierCertificatesDir, certificateFile), "Это тестовый сертификат", Encoding.GetEncoding(1251));

			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var task = new CertificateTask();
			task.CertificateSource = certificateSource;
			task.CatalogProduct = Catalog.Find(catalog.Id);
			task.SerialNumber = DocumentLine.EmptySerialNumber;
			task.DocumentLine = realDocumentLine;
			task.Save();

			documentLine.CertificateFilename = Path.GetFileNameWithoutExtension(certificateFile);
			documentLine.Save();

			Assert.That(task.Id, Is.GreaterThan(0));

			ProcessTestHandler();

			var processedTask = CertificateTask.Queryable.FirstOrDefault(t => t.Id == task.Id);
			Assert.That(processedTask, Is.Null, "Не была удалена задача на создание сертификата после обработки");

			var certificate =
				TestCertificate.Queryable.FirstOrDefault(c => c.CatalogProduct.Id == catalog.Id && c.SerialNumber == DocumentLine.EmptySerialNumber);
			Assert.That(certificate, Is.Not.Null, "Не был создан сертификат");
			Assert.That(certificate.CertificateFiles.Count, Is.EqualTo(1), "Не были добавлены файлы сертификата");
			Assert.That(certificate.CertificateFiles[0].OriginFilename, Is.EqualTo(certificateFile), "Не совпадает оригинальное имя файла сертификата");
			Assert.That(certificate.CertificateFiles[0].CertificateSource.Id, Is.EqualTo(certificateSource.Id), "Не совпадает источник файла сертификата");
			Assert.IsNotNullOrEmpty(certificate.CertificateFiles[0].ExternalFileId, "Не установлено поле ExternalFileId");
			Assert.That(certificate.CertificateFiles[0].ExternalFileId, Is.EqualTo(certificate.CertificateFiles[0].OriginFilename), "Поле ExternalFileId не совпадает с OriginFilename (только для AptekaHoldingVoronezhCertificateSource)");

			var file = Path.Combine(destinationDir, certificate.CertificateFiles[0].Id + ".tif");
			Assert.That(File.Exists(file), "Не скопирован файл {0} сертификата", file);
			Assert.That(File.Exists(Path.Combine(supplierCertificatesDir, certificateFile)), "Удален файл сертификата из исходной папки");

			documentLine.Refresh();
			Assert.That(documentLine.Certificate.Id, Is.EqualTo(certificate.Id), "В позиции документа не установлена ссылка на сертификат");
		}

		[Test]
		public void Log_task_failure()
		{
			var task = CreateTask();

			Process();

			session.Refresh(task.DocumentLine);
			Assert.That(task.DocumentLine.CertificateError, Is.StringContaining("System.NotImplementedException"));
		}

		[Test]
		public void Save_source_error()
		{
			var task = CreateTask();

			TestCertifcateSource.GetFilesFromSourceAction = (t, files) => { t.DocumentLine.CertificateError = "Тестовое сообщение"; };

			Process();

			session.Refresh(task.DocumentLine);
			Assert.That(task.DocumentLine.CertificateError, Is.StringContaining("Тестовое сообщение"));
		}

		[Test]
		public void Ignore_duplicate_task()
		{
			Clean();

			string dir;
			string file;
			var task = SuccessProcessCertificate(out dir, out file);
			var duplicateTask = new CertificateTask(task.CertificateSource, task.DocumentLine);
			session.Save(duplicateTask);

			ProcessTestHandler();

			session.Refresh(task.DocumentLine);
			Assert.That(task.DocumentLine.Certificate.CertificateFiles.Count, Is.EqualTo(1));
		}

		private CertificateTask SuccessProcessCertificate(out string supplierCertificatesDir, out string certificateFile)
		{
			var certificateSource = CreateRealSourceForSupplier(supplier);
			var serialNumber = Path.GetRandomFileName();
			var catalog = TestCatalogProduct.Queryable.First();
			var product = TestProduct.Queryable.First(p => p.CatalogProduct == catalog);

			supplierCertificatesDir = Path.Combine(Settings.Default.FTPOptBoxPath, supplier.Id.ToString().PadLeft(3, '0'), "Certificats");
			if (Directory.Exists(supplierCertificatesDir))
				Directory.Delete(supplierCertificatesDir, true);
			Directory.CreateDirectory(supplierCertificatesDir);

			certificateFile = Path.GetRandomFileName();
			File.WriteAllText(Path.Combine(supplierCertificatesDir, certificateFile), "Это тестовый сертификат", Encoding.GetEncoding(1251));

			var documentLine = CreateBodyLine(serialNumber, product);
			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];
			var task = new CertificateTask(certificateSource, realDocumentLine);
			task.Save();

			documentLine.CertificateFilename = Path.GetFileNameWithoutExtension(certificateFile);
			documentLine.Save();

			Assert.That(task.Id, Is.GreaterThan(0));

			ProcessTestHandler();
			return task;
		}

		private void ProcessTestHandler()
		{
			ProcessCertificatesWithLog(() => {
				var handler = new TestCertificateSourceHandler();

				handler.ProcessData();
			});
		}

		private void Process()
		{
			session.Transaction.Commit();
			var handler = new CertificateSourceHandler();
			handler.ProcessData();
		}

		private CertificateTask CreateTask()
		{
			TestCertifcateSource.GetFilesFromSourceAction = null;
			TestCertifcateSource.CertificateExistsAction = null;
			CertificateSource.Assembly = typeof(TestCertifcateSource).Assembly;

			var source = CreateSourceForSupplier(supplier, "TestCertifcateSource");
			var line = CreateBodyLine();

			var task = new CertificateTask();
			task.CertificateSource = source;
			task.CatalogProduct = session.Load<Catalog>(line.CatalogProduct.CatalogProduct.Id);
			task.SerialNumber = DocumentLine.EmptySerialNumber;
			task.DocumentLine = session.Load<DocumentLine>(line.Id);
			task.Save();
			return task;
		}
	}
}