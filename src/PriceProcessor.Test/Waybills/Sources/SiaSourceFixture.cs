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
			var task = new CertificateTask();
			task.DocumentLine = new DocumentLine {
				Code = "22651",
				SerialNumber = "835495"
			};
			var files = source.GetCertificateFiles(task);
			Assert.That(files.Count, Is.GreaterThan(0));
			var file = files[0];
			Assert.That(File.Exists(file.LocalFile), Is.True, "файл не существует {0}", file.LocalFile);
			Assert.That(file.ExternalFileId, Is.EqualTo(@"DOCS\2011\3\19\649995736_2.GIF"));
			Assert.That(file.OriginFilename, Is.EqualTo(@"649995736_2.GIF"));
			Assert.That(file.Extension, Is.EqualTo(".GIF"));
		}

		[Test]
		public void Parse_files()
		{
			var files = SiaSource.ParseFiles(@"DOCS\2011\3\19\649995736_2.GIF\r\nDOCS\2011\9\22\663082976_1.GIF").ToList();
			Assert.That(files, Is.EquivalentTo(new [] {@"DOCS\2011\3\19\649995736_2.GIF", @"DOCS\2011\9\22\663082976_1.GIF"}));

			files = SiaSource.ParseFiles(@"<tr><td>DOCS\2011\3\19\649995736_2.GIF</td><td>DOCS\2011\9\22\663082976_1.GIF</td></tr>").ToList();
			Assert.That(files, Is.EquivalentTo(new [] {@"DOCS\2011\3\19\649995736_2.GIF", @"DOCS\2011\9\22\663082976_1.GIF"}));

		}
	}
}