using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.MySql;
using Inforoom.PriceProcessor.Waybills.Models;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Handlers
{
	[TestFixture]
	public class WaybillLanSourceHandlerErrorsFixture
	{
		private int maxLogId;
		private uint supplierId;

		[SetUp]
		public void Setup()
		{
			using (new SessionScope()) {
				var count = DocumentReceiveLog.Queryable.Count();
				if (count > 0)
					maxLogId = DocumentReceiveLog.Queryable.Max(l => (int)l.Id);
			}
			var supplier = TestSupplier.Create();
			supplier.WaybillSource.SourceType = WaybillSourceType.FtpInforoom;
			supplier.WaybillSource.ReaderClassName = "SIAMoscow_2788_Reader";
			supplier.Save();

			supplierId = supplier.Id;
		}

		[TearDown]
		public void EndTest()
		{
			With.Connection(connection => {
				var command = new MySqlCommand(@"delete from logs.document_logs where rowid > ?Id", connection);
				command.Parameters.AddWithValue("?Id", maxLogId);
				command.ExecuteNonQuery();
			});
		}

		[Test]
		public void GetClientCodesErrorTest()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader1", supplierId);
			var res = handler.MoveWaybill("test", "test");

			using (new SessionScope()) {
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Id > maxLogId).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(supplierId));
				Assert.That(logs[0].Comment.Contains("Не получилось сформировать SupplierClientId(FirmClientCode) и SupplierDeliveryId(FirmClientCode2) из документа."), Is.True);
			}
		}

		[Test]
		public void FormatOutputFileError()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader2", supplierId);
			var res = handler.MoveWaybill("test", "test");

			using (new SessionScope()) {
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Id > maxLogId).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(supplierId));
				Assert.That(logs[0].Comment.Contains("Количество позиций в документе не соответствует значению в заголовке документа"), Is.True);
			}
		}

		[Test]
		public void ImportDocumentError()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader3", supplierId);
			var res = handler.MoveWaybill("test", "test");

			using (new SessionScope()) {
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Id > maxLogId).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(supplierId));
				Assert.That(logs[0].Comment.Contains("Дублирующийся документ"), Is.True);
			}
		}

		[Test]
		public void WithoutError()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader4", supplierId);
			var res = handler.MoveWaybill("test", "test");

			using (new SessionScope()) {
				Assert.That(res, Is.True);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.Id > maxLogId).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(supplierId));
				Assert.That(logs[0].Comment, Is.EqualTo("Получен с нашего FTP"));
			}
		}
	}
}