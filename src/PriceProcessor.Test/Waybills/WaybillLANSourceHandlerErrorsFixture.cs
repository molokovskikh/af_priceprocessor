using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.PriceProcessor.Waybills.Models;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class WaybillLANSourceHandlerErrorsFixture
	{
		private int maxLogId = 0;

		[SetUp]
		public void Setup()
		{
			using (new SessionScope()) {
				var count = DocumentReceiveLog.Queryable.Count();
				if(count > 0)
					maxLogId = DocumentReceiveLog.Queryable.Max(l => (int)l.Id);
			}
		}

		[TearDown]
		public void EndTest()
		{
			With.Connection(connection =>
			{
				var command = new MySqlCommand(@"delete from logs.document_logs where rowid > ?Id", connection);
				command.Parameters.AddWithValue("?Id", maxLogId);
				command.ExecuteNonQuery();
			});
		}

		[Test]
		public void GetClientCodesErrorTest()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader1");
			var res = handler.MoveWaybill("test", "test");
			using(new SessionScope())
			{
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Id > maxLogId).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment.Contains("�� ���������� ������������ SupplierClientId(FirmClientCode) � SupplierDeliveryId(FirmClientCode2) �� ���������."), Is.True);
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
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Id > maxLogId).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment.Contains("���������� ������� � ��������� �� ������������� �������� � ��������� ���������"), Is.True);
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
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Id > maxLogId).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment.Contains("������������� ��������"), Is.True);
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
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Id > maxLogId).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment, Is.Null);
			}
		}
	}
}