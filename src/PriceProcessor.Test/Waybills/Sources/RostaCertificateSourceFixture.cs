using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class RostaCertificateSourceFixture : IntegrationFixture
	{
		private CertificateSource _source;

		[SetUp]
		public void SetUp()
		{
			var supplier = TestSupplier.Create();
			var realSupplier = Supplier.Find(supplier.Id);

			using (new TransactionScope()) {
				var certificateSources = CertificateSource.Queryable.Where(s => s.SourceClassName == typeof(RostaCertificateSource).Name).ToList();
				certificateSources.ForEach(c => c.Delete());

				_source = new CertificateSource {
					SourceClassName = typeof(RostaCertificateSource).Name
				};
				_source.Suppliers = new List<Supplier>();
				_source.Suppliers.Add(realSupplier);
				_source.Save();
			}
		}

		[Test]
		public void CertificateExists()
		{
			var rostaSource = new RostaCertificateSource();

			var product = Product.FindFirst();

			var line = new DocumentLine {
				Code = "22651",
				SerialNumber = "835495",
				ProductEntity = product
			};

			Assert.That(rostaSource.CertificateExists(line), Is.False);
			var catalog = new CertificateSourceCatalog {
				CertificateSource = _source,
				SerialNumber = line.SerialNumber,
				//Код в накладной и в каталоге может не совпадать, сравниваем по CatalogId
				SupplierCode = "C!" + line.Code,
				CatalogProduct = product.CatalogProduct,
				OriginFilePath = Path.GetRandomFileName()
			};
			catalog.Save();
			Assert.That(rostaSource.CertificateExists(line), Is.True);
		}

		[Test, Ignore("Для ручной проверки что бы не долбить сервер Rosta")]
		public void TestGetFiles()
		{
			var rostaSource = new RostaCertificateSource();
			var task = new CertificateTask();
			task.CertificateSource = _source;
			task.DocumentLine = new DocumentLine {
				Code = "000002",
				SerialNumber = "C392764"
			};

			var catalog = new CertificateSourceCatalog {
				CertificateSource = _source,
				SerialNumber = task.DocumentLine.SerialNumber,
				SupplierCode = task.DocumentLine.Code,
				OriginFilePath = "005/0052602p-0.gif"
			};
			catalog.Save();

			catalog = new CertificateSourceCatalog {
				CertificateSource = _source,
				SerialNumber = task.DocumentLine.SerialNumber,
				SupplierCode = task.DocumentLine.Code,
				OriginFilePath = "005/0052602pd-0.gif"
			};
			catalog.Save();

			var files = rostaSource.GetCertificateFiles(task, session);

			Assert.That(files.Count, Is.EqualTo(2));
			var file = files[0];
			Assert.That(File.Exists(file.LocalFile), Is.True, "файл не существует {0}", file.LocalFile);
			Assert.That(file.ExternalFileId, Is.EqualTo(@"005/0052602p-0.gif"));
			Assert.That(file.OriginFilename, Is.EqualTo(@"0052602p-0.gif"));
			Assert.That(file.Extension, Is.EqualTo(".GIF").IgnoreCase);
		}
	}
}