using System.Collections.Generic;
using System.IO;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class NadezhdaFarmCertificateSourceFixture
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
		public void Load_certificates()
		{
			var source = new NadezhdaFarmCertificateSource();
			var line = new DocumentLine {
				ProductEntity = new Product(),
				CertificateFilename = "1473052_1_1.tif;1072321_1_0.tif;"
			};
			var sourceConfig = new CertificateSource {
				FtpSupplier = new Supplier {
					Id = 1
				}
			};
			var task = new CertificateTask(sourceConfig, line);

			TestHelper.InitFiles(task.GetLocalPath(), new[] { "1473052_1_1.tif" });

			Assert.IsTrue(source.CertificateExists(line));
			var files = new List<CertificateFile>();
			source.GetFilesFromSource(task, files);
			Assert.AreEqual(1, files.Count, task.DocumentLine.CertificateError);
		}
	}
}