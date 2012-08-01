using System;
using System.Collections.Generic;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class WaybillFormatDetectorFixture : IntegrationFixture
	{
		private Supplier supplier;
		private Address address;
		private Price price;

		[SetUp]
		public void Setup()
		{
			var testSupplier = TestSupplier.Create();
			price = session.Load<Price>(testSupplier.Prices[0].Id);

			var testClient = TestClient.Create();
			supplier = session.Load<Supplier>(testSupplier.Id);
			address = session.Load<Address>(testClient.Addresses[0].Id);
		}

		[Test]
		public void Do_not_match_with_order_duplicate_document()
		{
			var log = new DocumentReceiveLog(supplier, address);
			var order = new OrderHead(address, price);

			order.Items.Add(new OrderItem(order) {Code = "1"});

			var document = new Document(log) {ProviderDocumentId = "i-1"};
			var line = document.NewLine();
			line.Code = "1";

			var document1 = new Document(log) {ProviderDocumentId = "i-1"};
			var line1 = document1.NewLine();
			line.Code = "1";

			session.Save(order);
			line.OrderId = order.Id;
			line.OrderId = order.Id;

			document = WaybillFormatDetector.ProcessDocument(document, new List<OrderHead> {order});
			Assert.That(document, Is.EqualTo(document));
			Assert.That(line.OrderItems.Count, Is.EqualTo(1));
			session.Save(log);
			session.Save(document);

			document = WaybillFormatDetector.ProcessDocument(document1, new List<OrderHead> {order});
			Assert.That(document, Is.EqualTo(document1));
			Assert.That(line1.OrderItems, Is.Empty);
		}
	}
}