using System;
using System.Collections.Generic;
using System.IO;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class FarmKomplektCertificateSourceFixture
	{
		[Test(Description = "для строки из накладной определяем существование сертификатов")]
		public void CertificateExists()
		{
			var farmKomplektCertificateSource = new FarmKomplektCertificateSource();

			var product = Product.FindFirst();

			var line = new DocumentLine {
				Code = "22651",
				SerialNumber = "835495",
				ProductEntity = product
			};

			Assert.That(farmKomplektCertificateSource.CertificateExists(line), Is.False);

			line.CertificateFilename = "test_00";

			Assert.That(farmKomplektCertificateSource.CertificateExists(line), Is.True);
		}

		[Test(Description = "проверяем получение файлов из источника")]
		public void GetFilesFromSource()
		{
			var testSupplier = TestSupplier.Create();
			var supplier = Supplier.Find(testSupplier.Id);

			GetFiles(supplier, new[] { "1.tif" }, "1.tif", "1.tif", ".tif");

			GetFiles(supplier, new[] { "2.jpg" }, "2.jpg", "2.jpg", ".jpg");

			GetFiles(supplier, new[] { @"d\2.jpg" }, @"d\2.jpg", @"d\2.jpg", ".jpg");

			GetFiles(supplier, new[] { @"d\2" }, @"d\2", @"d\2", ".tif");

			GetFiles(supplier, new[] { @"d\2." }, @"d\2.", @"d\2.", ".tif");

			GetFiles(supplier, new[] { @"d\2.123" }, @"d\2.123", @"d\2.123", ".tif");

			GetFiles(supplier, new[] { @"d\2.123", @"d\2." }, @"d\2.", @"d\2.", ".tif");

			GetFiles(supplier, new[] { @"d\2.123", @"d\3." }, @"d\2.", null, null);
		}

		private void GetFiles(Supplier supplier, string[] existsFiles, string certFilename, string externalFileId, string extension)
		{
			var supplierCertificatesDir = Path.Combine(Settings.Default.FTPOptBoxPath, supplier.Id.ToString().PadLeft(3, '0'), "Certificats");
			if (!Directory.Exists(supplierCertificatesDir))
				Directory.CreateDirectory(supplierCertificatesDir);

			try {
				foreach (var existsFile in existsFiles) {
					var dirName = Path.GetDirectoryName(existsFile);
					if (!Directory.Exists(Path.Combine(supplierCertificatesDir, dirName)))
						Directory.CreateDirectory(Path.Combine(supplierCertificatesDir, dirName));

					File.WriteAllText(Path.Combine(supplierCertificatesDir, existsFile), "this is test file: " + existsFile);
				}

				var task = new CertificateTask {
					CertificateSource = new CertificateSource { FtpSupplier = supplier },
					DocumentLine = new DocumentLine { CertificateFilename = certFilename }
				};

				var files = new List<CertificateFile>();
				try {
					var source = new FarmKomplektCertificateSource();
					source.GetFilesFromSource(task, files);

					if (String.IsNullOrEmpty(externalFileId))
						Assert.That(files.Count, Is.EqualTo(0));
					else {
						Assert.That(files.Count, Is.EqualTo(1));
						var createdCertificateFile = files[0];
						Assert.That(createdCertificateFile.ExternalFileId, Is.EqualTo(externalFileId));
						Assert.That(createdCertificateFile.Extension, Is.EqualTo(extension));
						Assert.That(createdCertificateFile.OriginFilename, Is.EqualTo(Path.GetFileName(certFilename)));
					}
				}
				finally {
					foreach (var certificateFile in files)
						if (File.Exists(certificateFile.LocalFile))
							File.Delete(certificateFile.LocalFile);
				}
			}
			finally {
				Directory.Delete(supplierCertificatesDir, true);
			}
		}
	}
}