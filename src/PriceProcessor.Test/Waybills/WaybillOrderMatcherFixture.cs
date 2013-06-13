using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class WaybillOrderMatcherFixture : IntegrationFixture
	{
		protected TestClient client;
		protected Address address;
		protected TestPrice price;
		protected Supplier appSupplier;

		private OrderHead order1;
		private OrderHead order2;
		private DocumentReceiveLog log;

		private List<OrderHead> orders;

		[SetUp]
		public void SetUp()
		{
			orders = new List<OrderHead>();
			using (new SessionScope()) {
				client = TestClient.Create();
				var testAddress = client.Addresses[0];
				address = Address.Find(testAddress.Id);
				price = TestSupplier.CreateTestSupplierWithPrice();
				var supplier = price.Supplier;
				appSupplier = Supplier.Find(supplier.Id);
			}
		}

		[Test]
		public void ComparisonWithOrdersIfOrderIdInOrderHeadTest()
		{
			Document document;
			using (new SessionScope()) {
				order1 = BuildOrder();
				order1.Save();
				var item = new OrderItem { Code = "Code1", Order = order1, Quantity = 20 };
				item.Save();
				item = new OrderItem { Code = "Code2", Order = order1, Quantity = 25 };
				item.Save();
				item = new OrderItem { Code = "Code3", Order = order1, Quantity = 50 };
				item.Save();
				item = new OrderItem { Code = "Code4", Order = order1, Quantity = 100 };
				item.Save();

				order2 = BuildOrder();
				order2.Save();
				item = new OrderItem { Code = "Code3", Order = order2, Quantity = 15 };
				item.Save();
				item = new OrderItem { Code = "Code3", Order = order2, Quantity = 10 };
				item.Save();
				item = new OrderItem { Code = "Code5", Order = order2, Quantity = 15 };
				item.Save();

				var log = new DocumentReceiveLog(appSupplier, address) { MessageUid = 123, DocumentSize = 100 };
				document = new Document(log) { OrderId = order1.Id, DocumentDate = DateTime.Now };

				var docline = new DocumentLine { Document = document, Code = "Code1", Quantity = 20 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code2", Quantity = 15 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code2", Quantity = 5 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code3", Quantity = 75 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = null, Quantity = 75 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code5", Quantity = 10 };
				document.NewLine(docline);
				log.Save();
				document.Save();
			}

			using (new SessionScope()) {
				order1 = OrderHead.Find(order1.Id);
				order2 = OrderHead.Find(order2.Id);
				orders.Add(order1);
				orders.Add(order2);

				Match(document);
				document.SaveAndFlush();
			}

			var line = document.Lines[0];
			Assert.That(document.Lines[0].OrderItems, Is.EquivalentTo(new[] { order1.Items[0] }));

			line = document.Lines[1];
			Assert.That(line.OrderItems, Is.EquivalentTo(new[] { order1.Items[1] }));

			line = document.Lines[2];
			Assert.That(line.OrderItems, Is.EquivalentTo(new[] { order1.Items[1] }));

			line = document.Lines[3];
			Assert.That(line.OrderItems, Is.EquivalentTo(new[] { order1.Items[2], order2.Items[0], order2.Items[1] }));

			line = document.Lines[4];
			Assert.That(line.OrderItems, Is.Empty);

			line = document.Lines[5];
			Assert.That(line.OrderItems, Is.EquivalentTo(new[] { order2.Items[2] }));
		}

		[Test]
		public void ComparisonWithOrdersTestIfOrderIdInDocumentLineTest()
		{
			Document document;
			using (new SessionScope()) {
				order1 = BuildOrder();
				order1.Save();
				var item = new OrderItem { Code = "Code1", Order = order1, Quantity = 20 };
				item.Save();
				item = new OrderItem { Code = "Code2", Order = order1, Quantity = 25 };
				item.Save();
				item = new OrderItem { Code = "Code3", Order = order1, Quantity = 50 };
				item.Save();
				item = new OrderItem { Code = "Code4", Order = order1, Quantity = 100 };
				item.Save();

				order2 = BuildOrder();
				order2.Save();
				item = new OrderItem { Code = "Code3", Order = order2, Quantity = 15 };
				item.Save();
				item = new OrderItem { Code = "Code3", Order = order2, Quantity = 10 };
				item.Save();
				item = new OrderItem { Code = "Code5", Order = order2, Quantity = 15 };
				item.Save();

				log = new DocumentReceiveLog { Supplier = appSupplier, ClientCode = client.Id, Address = address, MessageUid = 123, DocumentSize = 100 };
				document = new Document(log) { OrderId = order1.Id, DocumentDate = DateTime.Now };

				var docline = new DocumentLine { Document = document, Code = "Code1", Quantity = 20, OrderId = order1.Id };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code2", Quantity = 15, OrderId = order1.Id };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code2", Quantity = 5, OrderId = order2.Id };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code3", Quantity = 50, OrderId = order1.Id };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code3", Quantity = 25, OrderId = order2.Id };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = null, Quantity = 75 };
				document.NewLine(docline);
				docline = new DocumentLine { Document = document, Code = "Code5", Quantity = 10, OrderId = order2.Id };
				document.NewLine(docline);
				log.Save();
				document.Save();
			}

			using (new SessionScope()) {
				order1 = OrderHead.Find(order1.Id);
				order2 = OrderHead.Find(order2.Id);
				orders.Add(order1);
				orders.Add(order2);

				Match(document);
				document.SaveAndFlush();
			}

			var line = document.Lines[0];
			Assert.That(document.Lines[0].OrderItems, Is.EquivalentTo(new[] { order1.Items[0] }));

			line = document.Lines[1];
			Assert.That(line.OrderItems, Is.EquivalentTo(new[] { order1.Items[1] }));

			line = document.Lines[2];
			Assert.That(line.OrderItems, Is.Empty);

			line = document.Lines[3];
			Assert.That(line.OrderItems, Is.EquivalentTo(new[] { order1.Items[2] }));

			line = document.Lines[4];
			Assert.That(line.OrderItems, Is.EquivalentTo(new[] { order2.Items[0], order2.Items[1] }));

			line = document.Lines[5];
			Assert.That(line.OrderItems, Is.Empty);

			line = document.Lines[6];
			Assert.That(line.OrderItems, Is.EquivalentTo(new[] { order2.Items[2] }));

			TestHelper.Execute(String.Format("delete from documents.waybillorders where DocumentLineId in ({0});", document.Lines.Implode(l => l.Id)));

			var detector = new WaybillFormatDetectorFake(document); // проверяем вызов функции ComparisonWithOrders из детектора
			detector.DetectAndParse(null, log);
			document.SaveAndFlush();

			var table = GetMatches(document);
			var rows = table.Rows;
			Assert.That(rows.Count, Is.GreaterThan(0), "для документа {0} не нашли сопоставленную позицию", document.Id);
			Assert.That(rows[0]["DocumentLineId"], Is.EqualTo(document.Lines[0].Id));
			Assert.That(rows[0]["OrderLineId"], Is.EqualTo(order1.Items[0].Id));
		}

		private void Match(Document document)
		{
			var orderItems = orders.SelectMany(o => o.Items).ToList();
			WaybillOrderMatcher.SafeComparisonWithOrders(document, orderItems);
		}

		private OrderHead BuildOrder()
		{
			OrderHead order1;
			order1 = new OrderHead {
				ClientCode = client.Id,
				Address = address,
				Price = Price.Find(price.Id)
			};
			return order1;
		}

		[Test(Description = "Проверка корректности обработки пустого документа")]
		public void ComparisonWithOrdersIfEmptyTest()
		{
			Document document;
			using (new SessionScope()) {
				var log = new DocumentReceiveLog {
					Supplier = appSupplier,
					ClientCode = client.Id,
					Address = address,
					MessageUid = 123,
					DocumentSize = 100
				};
				document = new Document(log);
				log.Save();
				document.Save();
			}
			WaybillOrderMatcher.ComparisonWithOrders(document, null);
		}

		[Test]
		public void Match_by_name()
		{
			var document = GetDocument();
			var line1 = document.NewLine();
			line1.Product = "АЛМАГЕЛЬ   А 170МЛ ФЛАК СУСП";
			line1.Code = "10062";
			line1.Producer = "Балканфарма - Троян АД";

			var line2 = document.NewLine();
			line2.Product = "АВЕНТ СОСКА ПОТОК МЕДЛЕННЫЙ N2 (82820) [82820]";

			var order = new OrderHead {
				Address = document.Address,
				ClientCode = 1
			};
			var orderItem1 = new OrderItem {
				Code = "14934026",
				ProductSynonym = new ProductSynonym("АЛМАГЕЛЬ А 170МЛ ФЛАК СУСП"),
				ProducerSynonym = new ProducerSynonym("Балканфарма - Троян АД")
			};
			var orderItem2 = new OrderItem {
				ProductSynonym = new ProductSynonym("АВЕНТ СОСКА ПОТОК МЕДЛЕННЫЙ N2 (82820) [82820]"),
			};
			order.Items.Add(orderItem1);
			order.Items.Add(orderItem2);

			WaybillOrderMatcher.ComparisonWithOrders(document, order.Items);
			Assert.That(line1.OrderItems, Is.EquivalentTo(new[] { orderItem1 }));
			Assert.That(line2.OrderItems, Is.EquivalentTo(new[] { orderItem2 }));
		}

		[Test]
		public void Match_without_code()
		{
			var document = GetDocument();
			var line = document.NewLine();
			line.Product = "АЛМАГЕЛЬ   А 170МЛ ФЛАК СУСП";
			line.Producer = "Балканфарма - Троян АД";

			var order = new OrderHead {
				Address = document.Address,
				ClientCode = 1
			};
			var orderItem = new OrderItem {
				Code = "14934026",
				ProductSynonym = new ProductSynonym("АЛМАГЕЛЬ А 170МЛ ФЛАК СУСП"),
				ProducerSynonym = new ProducerSynonym("Балканфарма - Троян АД")
			};
			order.Items.Add(orderItem);

			WaybillOrderMatcher.ComparisonWithOrders(document, order.Items);
			Assert.That(line.OrderItems, Is.EquivalentTo(new[] { orderItem }));
		}

		[Test]
		public void Respect_order_id()
		{
			var document = GetDocument();
			var line1 = document.NewLine();
			line1.Product = "АЛМАГЕЛЬ   А 170МЛ ФЛАК СУСП";
			line1.Producer = "Балканфарма - Троян АД";
			line1.OrderId = 1;

			var line2 = document.NewLine();
			line2.Product = "АЛМАГЕЛЬ   А 170МЛ ФЛАК СУСП";
			line2.Producer = "Балканфарма - Троян АД";
			line2.OrderId = 2;

			var order1 = new OrderHead(document.Address, null) {
				Id = 1,
			};
			var orderItem1 = new OrderItem(order1) {
				Code = "14934026",
				ProductSynonym = new ProductSynonym("АЛМАГЕЛЬ А 170МЛ ФЛАК СУСП"),
				ProducerSynonym = new ProducerSynonym("Балканфарма - Троян АД")
			};
			order1.Items.Add(orderItem1);

			var order2 = new OrderHead(document.Address, null) {
				Id = 2,
			};
			var orderItem2 = new OrderItem(order2) {
				Code = "14934026",
				ProductSynonym = new ProductSynonym("АЛМАГЕЛЬ А 170МЛ ФЛАК СУСП"),
				ProducerSynonym = new ProducerSynonym("Балканфарма - Троян АД")
			};
			order2.Items.Add(orderItem2);
			WaybillOrderMatcher.ComparisonWithOrders(document, new[] { order1, order2 }.SelectMany(o => o.Items).ToList());
			Assert.That(line1.OrderItems, Is.EquivalentTo(new[] { orderItem1 }));
			Assert.That(line2.OrderItems, Is.EquivalentTo(new[] { orderItem2 }));
		}

		private DataTable GetMatches(Document document)
		{
			var sql = String.Format("select * from documents.waybillorders where DocumentLineId in ({0});", document.Lines.Implode(l => l.Id));
			var ds = TestHelper.Fill(sql, (MySqlConnection)session.Connection);
			var table = ds.Tables[0];
			return table;
		}

		public class ParserFake : IDocumentParser
		{
			private Document doc;

			public ParserFake(Document doc)
			{
				this.doc = doc;
			}

			public Document Parse(string file, Document d)
			{
				return doc;
			}

			public static bool CheckFileFormat(DataTable data)
			{
				return true;
			}
		}

		public class WaybillFormatDetectorFake : WaybillFormatDetector
		{
			private Document doc;

			public WaybillFormatDetectorFake(Document doc)
			{
				this.doc = doc;
			}

			public override IDocumentParser DetectParser(string file, DocumentReceiveLog documentLog)
			{
				return new ParserFake(doc);
			}
		}

		private static Document GetDocument()
		{
			var log = new DocumentReceiveLog {
				Supplier = new Supplier { Name = "Тест" },
				ClientCode = 1,
				Address = new Address { Id = 2, Name = "Тест", Client = new Client { Id = 1 } },
				MessageUid = 123,
				DocumentSize = 100
			};
			return new Document(log);
		}
	}
}