using System.Collections.Generic;
using System.IO;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class NormanSourceFixture
	{
		private NormanSource source;
		private CertificateTask task;
		private List<CertificateFile> files;

		[SetUp]
		public void Setup()
		{
			source = new NormanSource();
			var line = new DocumentLine {
				CertificateFilename = "109127-1570912-S-8.jpg",
				ProductEntity = new Product()
			};
			task = new CertificateTask(new CertificateSource(), line);
			files = new List<CertificateFile>();
		}

		[Test, Ignore("Для ручного тестирования")]
		public void Load_certificate()
		{
			Assert.IsTrue(source.CertificateExists(task.DocumentLine));
			source.GetFilesFromSource(task, files);
			Assert.AreEqual(1, files.Count);
			Assert.IsTrue(File.Exists(files[0].LocalFile), files[0].LocalFile);
		}

		[Test, Ignore("Для ручного тестирования"]
		public void Get_unknown_certificate()
		{
			task.DocumentLine.CertificateFilename = "10912109127-1570912-S-8.jpg";
			Assert.IsTrue(source.CertificateExists(task.DocumentLine));
			source.GetFilesFromSource(task, files);
			Assert.AreEqual(0, files.Count);
			Assert.AreEqual("Поставщик не предоставил сертификат, текст ошибки File \"10912109127-1570912-S-8.jpg\" not found",
				task.DocumentLine.CertificateError);
		}
	}
}