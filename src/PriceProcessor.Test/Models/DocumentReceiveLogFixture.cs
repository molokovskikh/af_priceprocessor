using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class DocumentReceiveLogFixture
	{
		[Test]
		public void CopyFileTest()
		{
			var fileName = "test1.dbf";
			var file = File.Create(fileName);
			file.Close();
			var log = new DocumentReceiveLog() {
				Supplier = new Supplier { Name = "Тестовый" },
				ClientCode = 0,
				Address = new Address(),
				MessageUid = 123,
				DocumentSize = 100,
				FileName = fileName,
				LocalFileName = fileName
			};

			log.CopyDocumentToClientDirectory();
			Assert.That(File.Exists(log.GetRemoteFileNameExt()));
		}

		[Test]
		public void CopyFileWithoutTest()
		{
			var fileName = "test2.dbf";
			var log = new DocumentReceiveLog() {
				Supplier = new Supplier { Name = "Тестовый" },
				ClientCode = 0,
				Address = new Address(),
				MessageUid = 123,
				DocumentSize = 100,
				FileName = fileName,
				LocalFileName = fileName
			};

			Assert.DoesNotThrow(log.CopyDocumentToClientDirectory);
		}
	}
}
