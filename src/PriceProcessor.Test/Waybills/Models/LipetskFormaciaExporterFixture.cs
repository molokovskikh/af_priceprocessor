using System;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;
using Address = Inforoom.PriceProcessor.Waybills.Models.Address;

namespace PriceProcessor.Test.Waybills.Models
{
	[TestFixture]
	public class LipetskFormaciaExporterFixture : IntegrationFixture
	{
		[Test]
		public void LipetskFormaciaExporter()
		{
			var settings = new WaybillSettings();
			settings.IsConvertFormat = true;
			var address = @"..\..\Data\Waybills\446406_0.dbf";
			var startLog = new DocumentReceiveLog {
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
			var document = WaybillParser.Parse(address);
			document.Log = startLog;
			document.Log.IsFake = true;

			//test
			var log = Exporter.Convert(document, WaybillFormat.LipetskFarmacia, settings);
			Assert.That(log.FileName.Substring(log.FileName.Length - 4), Is.EqualTo(".xls"));
			Assert.That(document.Log.IsFake, Is.EqualTo(false));
		}
	}
}