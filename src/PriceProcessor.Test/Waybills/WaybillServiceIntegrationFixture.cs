using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	public class WaybillServiceIntegrationFixture : IntegrationFixture
	{
		[Test, Description("Тестирует сохранение отклоненной накладной")]
		public void RejectWaybillSaveTest()
		{
			var log = new DocumentReceiveLog();
			var client = TestClient.CreateNaked();
			client.Addresses[0].Enabled = false;
			Save(client);
			var supplier = TestSupplier.Create();

			log.Address = session.Query<Address>().First(a => a.Id == client.Addresses[0].Id);
			log.ClientCode = client.Id;
			log.Supplier = session.Query<Supplier>().First(a => a.Id == supplier.Id);
			log.Comment = "комментарий";
			log.DocumentSize = 12;
			log.DocumentType = DocType.Waybill;
			log.FileName = "файл";

			WaybillService.ParseWaybill(log);
			var savedDoc = session.Query<RejectWaybillLog>().Where(t => t.ClientCode == client.Id && t.Supplier == log.Supplier);
			Assert.That(savedDoc.Count(), Is.GreaterThan(0));
		}
	}
}
