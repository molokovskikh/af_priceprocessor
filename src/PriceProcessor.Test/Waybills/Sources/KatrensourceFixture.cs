using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net;
using LumiSoft.Net.FTP.Server;
using NUnit.Framework;
using Rhino.Mocks.Constraints;
using Test.Support;
using Is = NUnit.Framework.Is;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class KatrenSourceFixture : IntegrationFixture
	{
		[Test, Ignore("Для ручного тестирования")]
		public void Download_source()
		{
			var testProduct = new TestProduct("Тестовый продукт");
			session.Save(testProduct);
			var product = Product.Find(testProduct.Id);

			var source = new KatrenSource();

			var certificateSource = new CertificateSource();
			certificateSource.SourceClassName = source.GetType().Name;
			session.Save(certificateSource);

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
			session.Save(sourceCatalog);

			var task = new CertificateTask(certificateSource, line);
			var files = source.GetCertificateFiles(task, null);
			Assert.That(files.Count, Is.EqualTo(4));
		}

		[Test]
		public void Load_file_without_dir()
		{
			FTP_Server server = null;
			try {
				server = new FTP_Server();
				server.BindInfo = new [] { new BindInfo(BindInfoProtocol.TCP, IPAddress.Loopback, new Random().Next(10000, 20000)), };
				server.StartServer();
				var testProduct = new TestProduct("Тестовый продукт");
				session.Save(testProduct);
				var product = session.Load<Product>(testProduct.Id);
				var line = new DocumentLine {
					ProductEntity = product,
					SerialNumber = "012011",
				};

				var source = new KatrenSource();
				var certificateSource = new CertificateSource();
				certificateSource.SourceClassName = source.GetType().Name;
				session.Save(certificateSource);

				var sourceCatalog = new CertificateSourceCatalog {
					CertificateSource = certificateSource,
					SerialNumber = line.SerialNumber,
					CatalogProduct = product.CatalogProduct,
					SupplierCode = "34266440",
					SupplierProducerCode = "13483667",
					OriginFilePath = KatrenSource.ToOriginFileName(0x1B9EFC8),
				};
				session.Save(sourceCatalog);
				certificateSource.LookupUrl = "ftp://127.0.0.1:10001/";

				source.GetFilesFromSource(new CertificateTask(certificateSource, line), new List<CertificateFile>());
			}
			finally {
				if (server != null)
					server.Dispose();
			}
		}
	}
}