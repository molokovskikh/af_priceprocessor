using System;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class KatrenSourceFixture
	{
		[Test, Ignore("Для ручного тестирования")]
		public void Download_source()
		{
			var testProduct = new TestProduct("Тестовый продукт");
			testProduct.Save();
			var product = Product.Find(testProduct.Id);

			var source = new KatrenSource();

			var certificateSource = new CertificateSource();
			certificateSource.SourceClassName = source.GetType().Name;
			certificateSource.Save();

			var line = new DocumentLine {
				ProductEntity = product,
				SerialNumber = "012011",
			};

			var sourceCatalog = new CertificateSourceCatalog {
				CertificateSource = certificateSource,
				SerialNumber = line.SerialNumber,
				CatalogProduct = product.CatalogProduct,
				SupplierCode = "34266440",
				SupplierProducerCode = "13483667",
				OriginFilePath = KatrenSource.ToOriginFileName(0x1B9EFC8),
			};
			sourceCatalog.Save();

			var task = new CertificateTask(certificateSource, line);
			var files = source.GetCertificateFiles(task);
			Assert.That(files.Count, Is.EqualTo(4));
		}
	}
}