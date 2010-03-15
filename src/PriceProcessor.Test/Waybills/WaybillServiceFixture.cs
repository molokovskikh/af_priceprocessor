using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor.Waybills;
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
			var rule = ParseRule.Find(1179u);
			rule.ReaderClassName = "ProtekParser";
			rule.Update();
			var file = "1008fo.pd";
			var client = TestOldClient.CreateTestClient();
			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			
			var document = new TestDocument {
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};
			document.Save();
			File.Copy(@"..\..\Data\Waybills\1008fo.pd", Path.Combine(waybillsPath, String.Format("{0}_1008fo.pd", document.Id)));

			var service = new WaybillService();
			service.ParseWaybill(new [] {document.Id});

			using(new SessionScope())
			{
				var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= document.LogTime).ToList();
				Assert.That(waybills.Count, Is.EqualTo(1));
				var waybill = waybills.Single();
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}
	}
}
