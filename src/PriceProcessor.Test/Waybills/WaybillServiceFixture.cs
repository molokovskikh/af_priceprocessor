using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor.Waybills;
using log4net.Config;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class WaybillServiceFixture
	{
		[Test]
		public void Parse_waybill()
		{
			var file = "1008fo.pd";
			var client = TestOldClient.CreateTestClient();
			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			var log = new TestDocumentLog {
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};

			using(new TransactionScope())
				log.SaveAndFlush();

			File.Copy(@"..\..\Data\Waybills\1008fo.pd", Path.Combine(waybillsPath, String.Format("{0}_1008fo.pd", log.Id)));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new [] {log.Id});

			using(new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}

		[Test(Description = "тест разбора накладной с ShortName поставщика в имени файла")]
		public void Parse_waybill_with_ShortName_in_fileName()
		{
			var file = "1008fo.pd";
			var client = TestOldClient.CreateTestClient();
			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			var log = new TestDocumentLog
			{
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};

			var supplier = TestOldClient.Find(log.FirmCode);

			using (new TransactionScope())
				log.SaveAndFlush();

			File.Copy(
				@"..\..\Data\Waybills\1008fo.pd", 
				Path.Combine(waybillsPath, 
					String.Format(
						"{0}_{1}({2}){3}", 
						log.Id,
						supplier.ShortName,
						Path.GetFileNameWithoutExtension(file),
						Path.GetExtension(file))));

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });

			using (new SessionScope())
			{
				var waybill = TestWaybill.Find(ids.Single());
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}

        [Test]
        public void Parse_waybill_sst()
        {
            var file = "00000049080.sst";
            var client = TestOldClient.CreateTestClient();
            var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
            var waybillsPath = Path.Combine(docRoot, "Waybills");
            Directory.CreateDirectory(waybillsPath);
            var log = new TestDocumentLog
            {
                ClientCode = client.Id,
                FirmCode = 2200,
                LogTime = DateTime.Now,
                DocumentType = DocumentType.Waybill,
                FileName = file,
            };

            using (new TransactionScope())
                log.SaveAndFlush();

            File.Copy(@"..\..\Data\Waybills\00000049080.sst", Path.Combine(waybillsPath, String.Format("{0}_00000049080.sst", log.Id)));
           

            var service = new WaybillService();
            var ids = service.ParseWaybill(new[] { log.Id });
            
            //log.
            using (new SessionScope())
            {
                //Assert.Fail("Не бросили исключение, хотя должны были");
               // var waybill = TestWaybill.Find(ids.Single());
                //Assert.That(waybill.ClientCode, Is.EqualTo(8027));
                //Assert.That(waybill.FirmCode,Is.EqualTo(null));
                //Assert.That(waybill, null);
                //Assert.That(File.Exists(Path.Combine(@"C:\Prices\DownWaybills\", String.Format("{0}_00000049080.sst", log.Id))), Is.EqualTo(false));
                Assert.That(ids, Is.EqualTo(0));
                //Assert.That(waybill.Lines, Is.EqualTo());
                
            }
        }

	}
}
