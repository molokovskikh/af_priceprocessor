using System;
using System.IO;
using System.Linq;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class SiaSourceFixture
	{
		[Test, Ignore("Для ручной проверки что бы не долбить сервер сиа")]
		public void Check_sia_source()
		{
			var source = new SiaSource();
			var task = new CertificateTask {
				CertificateSource = new CertificateSource {
					LookupUrl = "http://sia:321632@online.penza.siaint.ru/Home/GetCert"
				}
			};
			task.DocumentLine = new DocumentLine {
				Code = "84029",
				SerialNumber = "0020214"
			};
			var files = source.GetCertificateFiles(task, null);
			Assert.That(files.Count, Is.GreaterThan(0));
			var file = files[0];
			Assert.That(File.Exists(file.LocalFile), Is.True, "файл не существует {0}", file.LocalFile);
			Assert.That(file.ExternalFileId, Is.EqualTo(@"http://online.penza.siaint.ru/Home/GetFile?NaimFile=DOCS\2014\4\5\707504403_1.GIF"));
			Assert.That(file.OriginFilename, Is.EqualTo(@"707504403_1.GIF"));
			Assert.That(file.Extension, Is.EqualTo(".GIF"));
		}
	}
}