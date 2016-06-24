using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.MySql;
using Inforoom.PriceProcessor.Waybills.Models;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Handlers
{
	[TestFixture]
	public class WaybillLanSourceHandlerErrorsFixture : IntegrationFixture
	{
		private uint supplierId;

		[SetUp]
		public void Setup()
		{
			session.CreateSQLQuery("delete from logs.document_logs").ExecuteUpdate();
			var supplier = TestSupplier.Create(session);
			supplier.WaybillSource.SourceType = TestWaybillSourceType.FtpInforoom;
			supplier.WaybillSource.ReaderClassName = "SIAMoscow_2788_Reader";
			session.Save(supplier);

			supplierId = supplier.Id;
		}

		[Test]
		public void GetClientCodesErrorTest()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader1", supplierId);
			var res = handler.MoveWaybill("test", "test");

			Assert.That(res, Is.False);
			var logs = session.Query<DocumentReceiveLog>().ToList();
			Assert.That(logs.Count, Is.EqualTo(1));
			Assert.That(logs[0].Supplier.Id, Is.EqualTo(supplierId));
			Assert.That(logs[0].Comment.Contains("Не получилось сформировать SupplierClientId(FirmClientCode) и SupplierDeliveryId(FirmClientCode2) из документа."), Is.True);
		}

		[Test]
		public void FormatOutputFileError()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader2", supplierId);
			var res = handler.MoveWaybill("test", "test");

			Assert.That(res, Is.False);
			var logs = session.Query<DocumentReceiveLog>().ToList();
			Assert.That(logs.Count, Is.EqualTo(1));
			Assert.That(logs[0].Supplier.Id, Is.EqualTo(supplierId));
			Assert.That(logs[0].Comment.Contains("Количество позиций в документе не соответствует значению в заголовке документа"), Is.True);
		}

		[Test]
		public void ImportDocumentError()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader3", supplierId);
			var res = handler.MoveWaybill("test", "test");

			Assert.That(res, Is.False);
			var logs = session.Query<DocumentReceiveLog>().ToList();
			Assert.That(logs.Count, Is.EqualTo(1));
			Assert.That(logs[0].Supplier.Id, Is.EqualTo(supplierId));
			Assert.That(logs[0].Comment.Contains("Дублирующийся документ"), Is.True);
		}

		[Test]
		public void WithoutError()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader4", supplierId);
			var res = handler.MoveWaybill("test", "test");

			Assert.That(res, Is.True);
			var logs = session.Query<DocumentReceiveLog>().ToList();
			Assert.That(logs.Count, Is.EqualTo(1));
			Assert.That(logs[0].Supplier.Id, Is.EqualTo(supplierId));
			Assert.That(logs[0].Comment, Is.EqualTo("Получен с нашего FTP"));
		}
	}
}