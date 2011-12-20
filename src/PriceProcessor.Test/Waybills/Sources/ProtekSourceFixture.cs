using System;
using System.Collections.Generic;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class ProtekSourceFixture
	{
		[Test, Ignore("Для ручной проверки что бы не долбить сервер Протек, работает только для IP 91.209.124.50")]
		public void Check_protek_source()
		{
			var source = new ProtekSource();
			var task = new CertificateTask();
			task.DocumentLine = new DocumentLine {
				ProtekDocIds = new List<ProtekDoc> {
					new ProtekDoc {
						DocId = 4938262
					}
				}
			};
			var files = source.GetCertificateFiles(task);
			Assert.That(files.Count, Is.GreaterThan(0));
			var file = files[0];
			Assert.That(file.ExternalFileId, Is.Not.Null);
			Assert.That(file.LocalFile, Is.Not.Null);
			Assert.That(file.Extension, Is.EqualTo(".tif"));
		}
	}
}