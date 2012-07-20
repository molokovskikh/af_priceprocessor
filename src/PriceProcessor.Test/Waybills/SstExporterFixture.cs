using System;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class SstExporterFixture
	{
		private Document document;
		private DocumentReceiveLog log;

		[SetUp]
		public void Setup()
		{
			log = new DocumentReceiveLog {
				Id = 100,
				Supplier = new Supplier {
					Id = 201,
					Name = "Тестовый поставщик"
				},
				DocumentType = DocType.Waybill,
				LogTime = DateTime.Now,
				ClientCode = 1001,
				Address = new Address {
					Id = 501,
					Org = new Org {
						FullName = "Тестовое юр.лицо"
					}
				}
			};
			document = new Document(log) {
				ProviderDocumentId = "001-01"
			};
			document.NewLine(new DocumentLine {
				Product = "Алька-прим шип.таб. Х10",
				Code = "21603",
				SerialNumber = "S123S",
				Certificates = "РОСС PL.ФМ01.Д70041",
				Period = "01.12.2012",
				Producer = "Polfa/Polpharma",
				Country = "Польша",
				ProducerCost = 89.26m,
				SupplierCost = 89.26m,
				SupplierCostWithoutNDS = 98.19m,
				Quantity = 4,
				Nds = 10,
			});
			document.CalculateValues();
		}

		[Test]
		public void ExportProtekSstFile()
		{
			SstExporter.Save(document);
			var resultFile = Path.GetFullPath(@"DocumentPath\501\waybills\100_Тестовый поставщик(001-01).sst");
			Assert.That(log.DocumentSize, Is.GreaterThan(0));
			Assert.That(log.FileName, Is.EqualTo("001-01.sst"));
			Assert.That(File.Exists(resultFile), Is.True, "файл накладной несуществует {0}", resultFile);

			var content = File.ReadAllText(resultFile, Encoding.GetEncoding(1251));
			Assert.That(content, Is.StringContaining("[Header]"));
			Assert.That(content, Is.StringContaining("[Body]"));
			var index = content.IndexOf(";S123S");
			Assert.That(index, Is.GreaterThan(0));
			var serialString = content.Substring(index, 7);
			var bytes = Encoding.GetEncoding(1251).GetBytes(serialString);
			Assert.That(bytes.Length, Is.EqualTo(7));
			Assert.That(bytes[6], Is.EqualTo(127), "Разделителем не является символ с кодом 127");
		}

		[Test]
		public void Write_additional_fields()
		{
			SstExporter.Save(document);
			document.Lines[0].CertificateAuthority = "Test";
			var resultFile = Path.GetFullPath(@"DocumentPath\501\waybills\100_Тестовый поставщик(001-01).sst");
			var content = File.ReadLines(resultFile, Encoding.GetEncoding(1251)).ToArray();
			Console.WriteLine(content[3], Is.StringEnding("432.04;Test;10;39.28;;;"));
		}
	}
}