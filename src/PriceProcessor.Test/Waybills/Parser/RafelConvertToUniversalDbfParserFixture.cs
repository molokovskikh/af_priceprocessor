using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class RafelConvertToUniversalDbfParserFixture
	{
		[Test]
		public void Parse()
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
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\ПР-Д-КЗ043809.xml");
			doc.Log = log;
			doc.Address = new Address {
				Name = "Тестовый Адрес",
				Id = 2321321
			};
			DbfExporter.SaveUniversalDbf(doc, "ПР-Д-КЗ043809.dbf");
			var data = Dbf.Load("ПР-Д-КЗ043809.dbf");
			Assert.That(data.Rows[0]["sgodn"], Is.EqualTo(DBNull.Value));
			Assert.That(data.Rows[0]["przv_post"], Is.EqualTo("ООО \"Медполимерторг\""));
		}
	}
}
