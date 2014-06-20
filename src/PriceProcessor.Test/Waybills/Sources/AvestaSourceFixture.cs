using System;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;
using Common.NHibernate;
using NHibernate.Linq;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class AvestaSourceFixture : IntegrationFixture
	{
		private FileCleaner cleaner;

		[SetUp]
		public void Setup()
		{
			cleaner = new FileCleaner();
		}

		[TearDown]
		public void Teardown()
		{
			cleaner.Dispose();
		}

		[Test]
		public void Load_files()
		{
			var source = new CertificateSource {
				SourceClassName = typeof(AvestaSource).Name,
				LookupUrl = new Uri(Path.GetFullPath("test-cert")).ToString(),
			};
			session.DeleteEach(session.Query<CertificateSource>().Where(s => s.SourceClassName == source.SourceClassName));
			FileHelper.InitDir("test-cert");
			cleaner.WatchDir("test-cert");

			var certFile = "test-cert\\Декларация.tif";
			File.WriteAllText(certFile, "");

			session.Save(source);
			session.Save(new CertificateSourceCatalog {
				CertificateSource = source,
				SupplierCode = "jsodfij1",
				SerialNumber = "012011",
				OriginFilePath = "Декларация.tif",
			});
			var loader = new AvestaSource(cleaner);
			var line = new DocumentLine {
				ProductEntity = new Product(),
				Code = "jsodfij1",
				SerialNumber = "012011",
			};
			var task = new CertificateTask(source, line);
			Assert.IsTrue(loader.CertificateExists(line));
			var files = loader.GetCertificateFiles(task, session);
			Assert.IsNull(line.CertificateError);
			Assert.AreEqual(1, files.Count);
		}

		[Test]
		public void Read_catalog()
		{
			var source = new AvestaSource();
			var table = Dbf.Load(@"..\..\Data\avesta_cert_catalog.dbf");
			var catalog = new CertificateSourceCatalog();
			source.ReadSourceCatalog(catalog, table.Rows[0]);
			Assert.AreEqual("47599", catalog.SupplierCode);
			Assert.AreEqual("1961013", catalog.SerialNumber);
			Assert.AreEqual(@"\СЕРТИФИКАТЫ\КОРВАЛОЛ капли 25мл Ф-Лексредства (фл-кап инд уп)\1961013\Декларация.tif", catalog.OriginFilePath);
		}
	}
}