using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using log4net.Config;
using NHibernate.Criterion;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class CertificateModelsFixture
	{
		private TestWaybillLine CreateBodyLine()
		{
			var supplier = (TestSupplier)TestSupplier.Queryable.First();
			var user = TestUser.Queryable.First(u => u.AvaliableAddresses.Count > 0);

			var documentLog = new TestDocumentLog {
				Supplier = supplier,
				Client = user.Client,
				DocumentType = DocumentType.Waybill,
				LogTime = DateTime.Now,
				FileName = Path.GetRandomFileName() + ".txt"
			};

			var document = new TestWaybill(documentLog);

			var documentLine = new TestWaybillLine();
			documentLine.Waybill = document;

			document.Lines.Add(documentLine);

			using (new TransactionScope()) {
				document.Save();
			}

			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.Lines[0].Id, Is.GreaterThan(0));

			return documentLine;
		}

		[Test(Description = "создаем задачу на разбор сертификата")]
		public void SimpleCreateTask()
		{
			var supplier = Supplier.Queryable.First();
			var certificateSource = CreateSourceForSupplier(supplier);
			var documentLine = CreateBodyLine();
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = Path.GetRandomFileName();
			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var task = new CertificateTask();
			using (new TransactionScope()) {
				task.CertificateSource = certificateSource;
				task.CatalogProduct = Catalog.Find(catalog.Id);
				task.SerialNumber = serialNumber;
				task.DocumentLine = realDocumentLine;
				task.Create();
			}

			Assert.That(task.Id, Is.GreaterThan(0));
		}

		[Test(Description = "создаем задачу на разбор сертификата с повторением уникального ключа")]
		public void CreateTaskOnUniqueKey()
		{
			var supplier = Supplier.Queryable.First();
			var certificateSource = CreateSourceForSupplier(supplier);
			var documentLine = CreateBodyLine();
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = "Мама мыла раму";
			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			using (new TransactionScope()) {
				var certificateTasks =
					CertificateTask.Queryable.Where(c => c.SerialNumber.Equals(serialNumber)).ToList();
				certificateTasks.ForEach(c => c.Delete());
			}

			var task = new CertificateTask();
			using (new TransactionScope()) {
				task.CertificateSource = certificateSource;
				task.CatalogProduct = Catalog.Find(catalog.Id);
				task.SerialNumber = serialNumber;
				task.DocumentLine = realDocumentLine;
				task.Create();
			}

			Assert.That(task.Id, Is.GreaterThan(0));

			var doubleDocumentLine = CreateBodyLine();
			var doubleRealDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var doubleTask = new CertificateTask {
				CertificateSource = certificateSource,
				CatalogProduct = task.CatalogProduct,
				SerialNumber = "мАМА мыла рАМУ",
				DocumentLine = doubleRealDocumentLine
			};

			try {
				using (new TransactionScope()) {
					doubleTask.Create();
				}

				Assert.Fail("При сохранении должны были получить исключение с нарушением уникального ключа");
			}
			catch (Exception exception) {
				if (!ExceptionHelper.IsDuplicateEntryExceptionInChain(exception))
					throw;
			}
		}

		private CertificateSource CreateSourceForSupplier(Supplier supplier)
		{
			var source = CertificateSource.Queryable.FirstOrDefault(s => s.Suppliers.FirstOrDefault(certificateSupplier => certificateSupplier.Id == supplier.Id) != null);
			if (source == null)
				using (new TransactionScope()) {
					source = new CertificateSource {
						SourceClassName = Path.GetRandomFileName() 
					};
					source.Suppliers = new List<Supplier>();
					source.Suppliers.Add(supplier);
					source.Create();
				}
			return source;
		}

		[Test(Description = "создаем сертификат")]
		public void SimpleCreateCertificate()
		{
			var supplier = Supplier.Queryable.First();
			var certificateSource = CreateSourceForSupplier(supplier);
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = Path.GetRandomFileName();

			var certificate = new Certificate();
			using (new TransactionScope()) {
				certificate.CatalogProduct = Catalog.Find(catalog.Id);
				certificate.SerialNumber = serialNumber;
				CreateFiles(certificateSource, certificate, 2);
				certificate.Create();
			}

			Assert.That(certificate.Id, Is.GreaterThan(0));
			Assert.That(certificate.CertificateFiles.ToList().TrueForAll(f => f.Id > 0));
		}

		[Test(Description = "создаем сертификат")]
		public void SimpleCreateCertificateWithSave()
		{
			var supplier = Supplier.Queryable.First();
			var certificateSource = CreateSourceForSupplier(supplier);
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = Path.GetRandomFileName();

			var certificate = CreateCertificateWithFiles(certificateSource, catalog, serialNumber);

			Assert.That(certificate.Id, Is.GreaterThan(0));
			Assert.That(certificate.CertificateFiles.ToList().TrueForAll(f => f.Id > 0));
		}

		private static Certificate CreateCertificateWithFiles(CertificateSource certificateSource,
			TestCatalogProduct catalog,
			string serialNumber)
		{
			var certificate = new Certificate();
			using (new TransactionScope())
			{
				certificate.CatalogProduct = Catalog.Find(catalog.Id);
				certificate.SerialNumber = serialNumber;
				CreateFiles(certificateSource, certificate, 2);
				certificate.Save();
			}
			return certificate;
		}

		[Test(Description = "создаем сертификат с повторением уникального ключа")]
		public void CreateCertificateOnUniqueKey()
		{
			var supplier = Supplier.Queryable.First();
			var certificateSource = CreateSourceForSupplier(supplier);
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = "Мама мыла раму";

			using (new TransactionScope()) {
				var certificates =
					Certificate.Queryable.Where(c => c.SerialNumber.Equals(serialNumber)).ToList();
				certificates.ForEach(c => c.Delete());
			}

			var certificate = CreateCertificateWithFiles(certificateSource, catalog, serialNumber);

			Assert.That(certificate.Id, Is.GreaterThan(0));
			Assert.That(certificate.CertificateFiles.ToList().TrueForAll(f => f.Id > 0));

			var doubleСertificate = new Certificate {
				CatalogProduct = certificate.CatalogProduct,
				SerialNumber = "мАМА мыла рАМУ"
			};

			try {
				using (new TransactionScope()) {
					doubleСertificate.Create();
				}

				Assert.Fail("При сохранении должны были получить исключение с нарушением уникального ключа");
			}
			catch (Exception exception) {
				if (!ExceptionHelper.IsDuplicateEntryExceptionInChain(exception))
					throw;
			}
		}

		private static void CreateFiles(CertificateSource certificateSource, Certificate certificate, int count)
		{
			for(var i = 0; i < count; i++)
			{
				certificate.NewFile(
					new CertificateFile {
						OriginFilename = Path.GetRandomFileName(),
						CertificateSource = certificateSource,
						Extension = ".tif"
					});
			}
		}

		[Test(Description = "создаем сертификат с повторением уникального ключа и исправляем ошибку в одной транзакции")]
		public void CreateTaskWithUniqueKeyAndCorrect()
		{
			var supplier = Supplier.Queryable.First();
			var certificateSource = CreateSourceForSupplier(supplier);
			var documentLine = CreateBodyLine();
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = "Мама мыла раму";
			var anotherSerialNumber = "Папа мыла раму";
			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			using (new TransactionScope()) {
				var certificateTasks =
					CertificateTask.Queryable.Where(c => c.SerialNumber.Equals(serialNumber)).ToList();
				certificateTasks.ForEach(c => c.Delete());
			}

			using (new TransactionScope()) {
				var certificateTasks =
					CertificateTask.Queryable.Where(c => c.SerialNumber.Equals(anotherSerialNumber)).ToList();
				certificateTasks.ForEach(c => c.Delete());
			}

			var task = new CertificateTask();
			using (new TransactionScope()) {
				task.CertificateSource = certificateSource;
				task.CatalogProduct = Catalog.Find(catalog.Id);
				task.SerialNumber = serialNumber;
				task.DocumentLine = realDocumentLine;
				task.Create();
			}

			Assert.That(task.Id, Is.GreaterThan(0));

			var doubleDocumentLine = CreateBodyLine();
			var doubleRealDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var doubleTask = new CertificateTask {
				CertificateSource = certificateSource,
				CatalogProduct = task.CatalogProduct,
				SerialNumber = "мАМА мыла рАМУ",
				DocumentLine = doubleRealDocumentLine
			};

			using (var transaction = new TransactionScope()) {

				//Если в транзакции возникает ошибка DuplicateEntry, то транзакция откатывается
				//поэтому перед сохранением надо проверять, что такое задание не существует
				var existsTask = CertificateTask.Exists(
					DetachedCriteria.For<CertificateTask>()
						.Add(Restrictions.Eq("CertificateSource.Id", task.CertificateSource.Id))
						.Add(Restrictions.Eq("CatalogProduct.Id", task.CatalogProduct.Id))
						.Add(Restrictions.Eq("SerialNumber", doubleTask.SerialNumber)));

				if (existsTask)
					doubleTask.SerialNumber = anotherSerialNumber;

				doubleTask.Create();

				transaction.VoteCommit();
			}

			Assert.That(doubleTask.Id, Is.GreaterThan(0));
			Assert.That(doubleTask.Id, Is.Not.EqualTo(task.Id));
		}

		[Test(Description = "проверка поиска сертификата с различными параметрами")]
		public void CheckLikedSearch()
		{
			var supplier = Supplier.Queryable.First();
			var certificateSource = CreateSourceForSupplier(supplier);

			var anotherSupplier = Supplier.Queryable.Where(s => s.Id != supplier.Id).First();
			var anotherSupplierSource = CreateSourceForSupplier(anotherSupplier);

			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = "Мама мыла раму";

			using (new TransactionScope()) {
				var certificates =
					Certificate.Queryable.Where(c => c.SerialNumber.Equals(serialNumber)).ToList();
				certificates.ForEach(c => c.Delete());
			}

			var certificate = CreateCertificateWithFiles(certificateSource, catalog, serialNumber);

			Assert.That(certificate.Id, Is.GreaterThan(0));
			Assert.That(certificate.CertificateFiles.ToList().TrueForAll(f => f.Id > 0));

			using (var transaction = new TransactionScope()) {

				var findedCertificate = Certificate.FindFirst(
					DetachedCriteria.For<Certificate>()
						.Add(Restrictions.Eq("CatalogProduct.Id", certificate.CatalogProduct.Id))
						.Add(Restrictions.Eq("SerialNumber", serialNumber)));

				Assert.That(findedCertificate, Is.Not.Null);
				Assert.That(findedCertificate.Id, Is.EqualTo(certificate.Id));


				findedCertificate = Certificate.FindFirst(
					DetachedCriteria.For<Certificate>()
						.Add(Restrictions.Eq("CatalogProduct.Id", certificate.CatalogProduct.Id))
						.Add(Restrictions.Eq("SerialNumber", "мАМА мыла рАМУ")));

				Assert.That(findedCertificate, Is.Not.Null);
				Assert.That(findedCertificate.Id, Is.EqualTo(certificate.Id));

				findedCertificate = Certificate.FindFirst(
					DetachedCriteria.For<Certificate>()
						.Add(Restrictions.Eq("CatalogProduct.Id", certificate.CatalogProduct.Id))
						.Add(Restrictions.Eq("SerialNumber", "какая-то большая фигня")));

				Assert.That(findedCertificate, Is.Null);

				findedCertificate =
					Certificate.Queryable.FirstOrDefault(
						c => c.CatalogProduct.Id == certificate.CatalogProduct.Id && c.SerialNumber == serialNumber);

				Assert.That(findedCertificate, Is.Not.Null);
				Assert.That(findedCertificate.Id, Is.EqualTo(certificate.Id));


				findedCertificate = 
					Certificate.Queryable.FirstOrDefault(
						c => c.CatalogProduct.Id == certificate.CatalogProduct.Id && c.SerialNumber == "мАМА мыла рАМУ");

				Assert.That(findedCertificate, Is.Not.Null);
				Assert.That(findedCertificate.Id, Is.EqualTo(certificate.Id));

				findedCertificate = 
					Certificate.Queryable.FirstOrDefault(
						c => c.CatalogProduct.Id == certificate.CatalogProduct.Id && c.SerialNumber == "какая-то большая фигня");

				Assert.That(findedCertificate, Is.Null);


				//Поиск сертификата с привязкой к поставщику
				findedCertificate = 
					Certificate.Queryable.FirstOrDefault(
						c => c.CatalogProduct.Id == certificate.CatalogProduct.Id && c.SerialNumber == serialNumber && c.CertificateFiles.Any(f => f.CertificateSource.Id == anotherSupplierSource.Id));

				Assert.That(findedCertificate, Is.Null);


				transaction.VoteRollBack();
			}

		}
	}
}
	