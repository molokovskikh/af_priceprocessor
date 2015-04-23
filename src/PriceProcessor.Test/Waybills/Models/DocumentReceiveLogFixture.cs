using System;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Models
{
	[TestFixture]
	public class DocumentReceiveLogFixture : IntegrationFixture
	{
		[Test]
		public void Check_client_region()
		{
			var supplier = new Supplier {
				RegionMask = 2
			};
			var address = new Address {
				Client = new Client {
					MaskRegion = 1
				}
			};
			var log = new DocumentReceiveLog(supplier, address);
			Assert.Catch<EMailSourceHandlerException>(() => log.Check(session));
		}

		[Test, Ignore("Функционал, реализованный в задаче http://redmine.analit.net/issues/32741 отменяет данный тест")]
		public void Check_user_update_time()
		{
			var client = TestClient.CreateNaked();
			var supplier = TestSupplier.Create();

			session.CreateSQLQuery(
				"update Logs.AuthorizationDates set AFTime = '2012-05-06' where UserId = :userId")
				.SetParameter("userId", client.Users[0].Id)
				.ExecuteUpdate();

			var log = new DocumentReceiveLog(
				session.Load<Supplier>(supplier.Id),
				session.Load<Address>(client.Addresses[0].Id));

			var e = Assert.Catch<EMailSourceHandlerException>(() => log.Check(session));
			Assert.That(e.Message, Is.StringContaining("ни один пользователь этого адреса не обновляется более месяца"));
		}
	}
}