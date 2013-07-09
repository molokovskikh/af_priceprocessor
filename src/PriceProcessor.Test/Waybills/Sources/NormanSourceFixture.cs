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
		[Test, Ignore("Для ручного тестирования")]
		public void Load_certificate()
		{
			var source = new NormanSource();
			var line = new DocumentLine {
				CertificateFilename = "109127-1570912-r-1.jpg",
				ProductEntity = new Product()
			};
			var task = new CertificateTask(new CertificateSource(), line);
			Assert.IsTrue(source.CertificateExists(task.DocumentLine));

			var files = new List<CertificateFile>();
			source.GetFilesFromSource(task, files);
			Assert.AreEqual(1, files.Count);
			Assert.IsTrue(File.Exists(files[0].LocalFile), files[0].LocalFile);
		}
	}
}