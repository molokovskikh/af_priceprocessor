using System;
using System.Linq;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using NHibernate.Linq;
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
			var supplier = TestSupplier.CreateNaked();
			var client = TestClient.CreateNaked();
			var settings = session.Load<WaybillSettings>(client.Id);
			settings.AssortimentPriceId = supplier.Prices[0].Id;
			settings.WaybillConvertFormat = WaybillFormat.LipetskFarmacia;
			settings.IsConvertFormat = true;
			var path = @"..\..\Data\Waybills\446406_0.dbf";
			var startLog = new DocumentReceiveLog(session.Load<Supplier>(supplier.Id), session.Load<Address>(client.Addresses[0].Id)) {
				DocumentType = DocType.Waybill,
				LogTime = DateTime.Now,
			};
			session.Save(startLog);
			var document = WaybillParser.Parse(path, startLog);
			document.Log = startLog;
			document.DocumentDate = DateTime.Now;
			document.Log.IsFake = true;

			session.Save(document);


			//test
			var log = Exporter.Convert(document, WaybillFormat.LipetskFarmacia, settings);
			Assert.That(log.FileName.Substring(log.FileName.Length - 4), Is.EqualTo(".xls"));
			Assert.That(document.Log.IsFake, Is.EqualTo(false));
		}

		[Test]
		public void LipetskFormaciaProtekExport()
		{
			var supplier = TestSupplier.CreateNaked();
			var client = TestClient.CreateNaked();
			var settings = session.Load<WaybillSettings>(client.Id);
			settings.AssortimentPriceId = supplier.Prices[0].Id;
			settings.WaybillConvertFormat = WaybillFormat.LipetskFarmacia;
			settings.IsConvertFormat = true;
			var path = @"..\..\Data\Waybills\446406_0.dbf";
			var startLog = new DocumentReceiveLog(session.Load<Supplier>(supplier.Id), session.Load<Address>(client.Addresses[0].Id)) {
				DocumentType = DocType.Waybill,
				LogTime = DateTime.Now,
			};
			session.Save(startLog);
			var document = WaybillParser.Parse(path, startLog);
			document.Log = startLog;
			document.DocumentDate = DateTime.Now;
			document.Log.IsFake = true;
			session.Save(document);

			//test
			Exporter.SaveProtek(document);
			var dblogs = session.Query<DocumentReceiveLog>().Where(i => i.ClientCode == client.Id).ToList();
			Assert.That(dblogs.Count, Is.EqualTo(2));
			Assert.That(dblogs[0].IsFake, Is.False);
			Assert.That(dblogs[1].IsFake, Is.False);
		}
	}
}