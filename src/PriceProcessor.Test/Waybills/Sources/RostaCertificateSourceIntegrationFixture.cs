using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Sources
{
	public class RostaCertificateSourceIntegrationFixture : IntegrationFixture
	{
		private CertificateSource _source;
		private TestSupplier _testSupplier;
		private TestUser _testUser;
		private Supplier _realSupplier;

		[SetUp]
		public void SetUp()
		{
			_testSupplier = TestSupplier.Create();
			_realSupplier = session.Query<Supplier>().FirstOrDefault(s => s.Id == _testSupplier.Id);

			var certificateSources = session.Query<CertificateSource>().Where(s => s.SourceClassName == typeof(RostaCertificateSource).Name).ToList();
			certificateSources.ForEach(c => session.Delete(c));

			_source = new CertificateSource {
				SourceClassName = typeof(RostaCertificateSource).Name
			};
			_source.Suppliers = new List<Supplier>();
			_source.Suppliers.Add(_realSupplier);
			session.Save(_source);

			_testUser = TestClient.Create().Users[0];
		}

		[Test, Ignore("Для ручной проверки что бы не долбить сервер Rosta")]
		public void DeleteTempFolderTest()
		{
			var rostaSource = new RostaCertificateSource();
			var product = session.Query<Product>().First(p => p.CatalogProduct != null);
			var documentLog = new TestDocumentLog {
				Supplier = _testSupplier,
				Client = _testUser.Client,
				DocumentType = DocumentType.Waybill,
				LogTime = DateTime.Now,
				FileName = Path.GetRandomFileName() + ".txt"
			};
			var document = new TestWaybill(documentLog);
			session.Save(document);

			var realDocument = session.Load<Document>(document.Id);

			var task = new CertificateTask {
				SerialNumber = "123",
				CatalogProduct = product.CatalogProduct,
			};
			task.CertificateSource = _source;
			task.DocumentLine = new DocumentLine {
				Code = "000002",
				SerialNumber = "C392764",
				Document = realDocument,
				ProductEntity = product
			};
			Save(task.DocumentLine);
			Save(task);

			var certificsteCatalog = new CertificateSourceCatalog {
				CertificateSource = _source,
				SerialNumber = task.DocumentLine.SerialNumber,
				SupplierCode = task.DocumentLine.Code,
				OriginFilePath = "005/0052602p-0.gif",
				CatalogProduct = product.CatalogProduct
			};
			Save(certificsteCatalog);
			Reopen();
			rostaSource.GetCertificateFiles(task, session);
			// Проверяем, что временная папка удалена
			Assert.That(Directory.Exists(rostaSource.TMPDownloadDir), Is.False);
		}
	}
}
