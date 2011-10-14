using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class WaybillLANSourceHandlerErrorsFixture
	{
		private DateTime start;

		[SetUp]
		public void Setup()
		{
			Thread.Sleep(1000);
			start = DateTime.Now;
			Thread.Sleep(1000);
		}

		[Test]
		public void GetClientCodesErrorTest()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader1");
			var res = handler.MoveWaybill("test", "test");			
			using(new SessionScope())
			{
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.LogTime >= start).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment.Contains("Ќе получилось сформировать SupplierClientId(FirmClientCode) и SupplierDeliveryId(FirmClientCode2) из документа."), Is.True);
			}
		}

		[Test]
		public void FormatOutputFileError()
		{			
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader2");
			var res = handler.MoveWaybill("test", "test");
			using (new SessionScope())
			{
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.LogTime >= start).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment.Contains(" оличество позиций в документе не соответствует значению в заголовке документа"), Is.True);
			}
		}

		[Test]
		public void ImportDocumentError()
		{			
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader3");
			var res = handler.MoveWaybill("test", "test");
			using (new SessionScope())
			{
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.LogTime >= start).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment.Contains("ƒублирующийс€ документ"), Is.True);
			}
		}

		[Test]
		public void WithoutError()
		{			
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader4");
			var res = handler.MoveWaybill("test", "test");
			using (new SessionScope())
			{
				Assert.That(res, Is.True);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.LogTime >= start).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment, Is.Null);
			}
		}
	}
}