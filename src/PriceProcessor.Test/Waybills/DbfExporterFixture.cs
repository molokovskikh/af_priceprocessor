using System;
using System.IO;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class DbfExporterFixture
	{
		[Test]
		public void Export_protek_dbf_file()
		{
			var log = new DocumentReceiveLog {
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
			var document = new Document(log);
			document.NewLine(new DocumentLine {
				Product = "Алька-прим шип.таб. Х10",
				Code = "21603",
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
			DbfExporter.SaveProtek(document);
			var resultFile = Path.GetFullPath(@"DocumentPath\501\waybills\100_Тестовый поставщик(100).dbf");
			Assert.That(log.DocumentSize, Is.GreaterThan(0));
			Assert.That(log.FileName, Is.EqualTo("100.dbf"));
			Assert.That(File.Exists(resultFile), Is.True, "файл накладной несуществует {0}", resultFile);
			var table = Dbf.Load(resultFile);
			Assert.That(table.Rows.Count, Is.EqualTo(1));
		}
	}
}