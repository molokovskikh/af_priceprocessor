﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;
using System.IO;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	class WaybillOrderMatcherFixture
	{
		protected TestClient client;
		protected Address address;
		protected TestPrice price;
		protected Supplier appSupplier;

		OrderHead order1;
		OrderHead order2;		
		DocumentReceiveLog log;

		[SetUp]
		public void Setup()
		{
			using (new SessionScope())
			{
				client = TestClient.Create();
				var testAddress = client.Addresses[0];
				address = Address.Find(testAddress.Id);
				price = TestSupplier.CreateTestSupplierWithPrice();
				var supplier = price.Supplier;
				appSupplier = Supplier.Find(supplier.Id);
			}
		}

		[Test]
		public void ComparisonWithOrdersTest()
		{
			ComparisonWithOrdersIfOrderIdInOrderHeadTest();
			ComparisonWithOrdersTestIfOrderIdInDocumentLineTest();
		}
	
		public void ComparisonWithOrdersIfOrderIdInOrderHeadTest()
		{
			Document document;
			using (new SessionScope())
			{
				order1 = BuildOrder();
				order1.Save();
				var item = new OrderItem { Code = "Code1", Order = order1, Quantity = 20 }; item.Save();
				item = new OrderItem { Code = "Code2", Order = order1, Quantity = 25 }; item.Save();
				item = new OrderItem { Code = "Code3", Order = order1, Quantity = 50 }; item.Save();
				item = new OrderItem { Code = "Code4", Order = order1, Quantity = 100 }; item.Save();

				order2 = BuildOrder();
				order2.Save();
				item = new OrderItem { Code = "Code3", Order = order2, Quantity = 15 }; item.Save();
				item = new OrderItem { Code = "Code3", Order = order2, Quantity = 10 }; item.Save();
				item = new OrderItem { Code = "Code5", Order = order2, Quantity = 15 }; item.Save();

				var log = new DocumentReceiveLog(appSupplier, address) { MessageUid = 123, DocumentSize = 100};
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

			IList<OrderHead> orders = new List<OrderHead>();

			using (new SessionScope())
			{
				order1 = OrderHead.Find(order1.Id);
				order2 = OrderHead.Find(order2.Id);
				orders.Add(order1);
				orders.Add(order2);	
			}

			WaybillOrderMatcher.SafeComparisonWithOrders(document, orders);

			var table = GetMatches(document);
			Assert.That(table.Rows.Count, Is.EqualTo(7));
			Assert.That(table.Rows[0]["DocumentLineId"], Is.EqualTo(document.Lines[0].Id));
			Assert.That(table.Rows[0]["OrderLineId"], Is.EqualTo(order1.Items[0].Id));
			Assert.That(table.Rows[1]["DocumentLineId"], Is.EqualTo(document.Lines[1].Id));
			Assert.That(table.Rows[1]["OrderLineId"], Is.EqualTo(order1.Items[1].Id));
			Assert.That(table.Rows[2]["DocumentLineId"], Is.EqualTo(document.Lines[2].Id));
			Assert.That(table.Rows[2]["OrderLineId"], Is.EqualTo(order1.Items[1].Id));
			Assert.That(table.Rows[3]["DocumentLineId"], Is.EqualTo(document.Lines[3].Id));
			Assert.That(table.Rows[3]["OrderLineId"], Is.EqualTo(order1.Items[2].Id));
			Assert.That(table.Rows[4]["DocumentLineId"], Is.EqualTo(document.Lines[3].Id));
			Assert.That(table.Rows[4]["OrderLineId"], Is.EqualTo(order2.Items[0].Id));
			Assert.That(table.Rows[5]["DocumentLineId"], Is.EqualTo(document.Lines[3].Id));
			Assert.That(table.Rows[5]["OrderLineId"], Is.EqualTo(order2.Items[1].Id));
			Assert.That(table.Rows[6]["DocumentLineId"], Is.EqualTo(document.Lines[5].Id));
			Assert.That(table.Rows[6]["OrderLineId"], Is.EqualTo(order2.Items[2].Id));
		}

		private static DataTable GetMatches(Document document)
		{
			var ds =
				TestHelper.Fill(String.Format("select * from documents.waybillorders where DocumentLineId in ({0});",
					document.Lines.Implode(l => l.Id)));
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
			public Document Parse(string file, Document d) { return doc; }
			public static bool CheckFileFormat(DataTable data) { return true; }
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

		public void ComparisonWithOrdersTestIfOrderIdInDocumentLineTest()
		{
			Document document;
			using (new SessionScope())
			{
				order1 = BuildOrder();
				order1.Save();
				var item = new OrderItem {Code = "Code1", Order = order1, Quantity = 20}; item.Save();
				item = new OrderItem {Code = "Code2", Order = order1, Quantity = 25}; item.Save();
				item = new OrderItem {Code = "Code3", Order = order1, Quantity = 50}; item.Save();
				item = new OrderItem {Code = "Code4", Order = order1, Quantity = 100}; item.Save();

				order2 = BuildOrder();
				order2.Save();
				item = new OrderItem {Code = "Code3", Order = order2, Quantity = 15}; item.Save();
				item = new OrderItem {Code = "Code3", Order = order2, Quantity = 10}; item.Save();
				item = new OrderItem {Code = "Code5", Order = order2, Quantity = 15}; item.Save();

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

			using (new SessionScope())
			{				
				order1 = OrderHead.Find(order1.Id);
				order2 = OrderHead.Find(order2.Id);
			}

			WaybillOrderMatcher.SafeComparisonWithOrders(document, new List<OrderHead>{order1, order2});

			var table = GetMatches(document);
			Assert.That(table.Rows.Count, Is.EqualTo(6));
			Assert.That(table.Rows[0]["DocumentLineId"], Is.EqualTo(document.Lines[0].Id));
			Assert.That(table.Rows[0]["OrderLineId"], Is.EqualTo(order1.Items[0].Id));
			Assert.That(table.Rows[1]["DocumentLineId"], Is.EqualTo(document.Lines[1].Id));
			Assert.That(table.Rows[1]["OrderLineId"], Is.EqualTo(order1.Items[1].Id));
			Assert.That(table.Rows[2]["DocumentLineId"], Is.EqualTo(document.Lines[3].Id));
			Assert.That(table.Rows[2]["OrderLineId"], Is.EqualTo(order1.Items[2].Id));
			Assert.That(table.Rows[3]["DocumentLineId"], Is.EqualTo(document.Lines[4].Id));
			Assert.That(table.Rows[3]["OrderLineId"], Is.EqualTo(order2.Items[0].Id));
			Assert.That(table.Rows[4]["DocumentLineId"], Is.EqualTo(document.Lines[4].Id));
			Assert.That(table.Rows[4]["OrderLineId"], Is.EqualTo(order2.Items[1].Id));
			Assert.That(table.Rows[5]["DocumentLineId"], Is.EqualTo(document.Lines[6].Id));
			Assert.That(table.Rows[5]["OrderLineId"], Is.EqualTo(order2.Items[2].Id));

			TestHelper.Execute(String.Format("delete from documents.waybillorders where DocumentLineId in ({0});", document.Lines.Implode(l => l.Id)));

			var detector = new WaybillFormatDetectorFake(document); // проверяем вызов функции ComparisonWithOrders из детектора
			detector.DetectAndParse(log, null);

			table = GetMatches(document);
			Assert.That(table.Rows[0]["DocumentLineId"], Is.EqualTo(document.Lines[0].Id));
			Assert.That(table.Rows[0]["OrderLineId"], Is.EqualTo(order1.Items[0].Id));
		}

		private OrderHead BuildOrder()
		{
			OrderHead order1;
			order1 = new OrderHead
			{
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
			var line = document.NewLine();
			line.Product = "АЛМАГЕЛЬ   А 170МЛ ФЛАК СУСП";
			line.Code = "10062";
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

			WaybillOrderMatcher.ComparisonWithOrders(document, new [] { order });
			Assert.That(line.OrderItems, Is.EquivalentTo(new [] {orderItem}));
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

			WaybillOrderMatcher.ComparisonWithOrders(document, new [] { order });
			Assert.That(line.OrderItems, Is.EquivalentTo(new [] {orderItem}));
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

			var order1 = new OrderHead {
				Id = 1,
				Address = document.Address,
				ClientCode = 1
			};
			var orderItem1 = new OrderItem {
				Code = "14934026",
				ProductSynonym = new ProductSynonym("АЛМАГЕЛЬ А 170МЛ ФЛАК СУСП"),
				ProducerSynonym = new ProducerSynonym("Балканфарма - Троян АД")
			};
			order1.Items.Add(orderItem1);

			var order2 = new OrderHead {
				Id = 2,
				Address = document.Address,
				ClientCode = 1
			};
			var orderItem2 = new OrderItem {
				Code = "14934026",
				ProductSynonym = new ProductSynonym("АЛМАГЕЛЬ А 170МЛ ФЛАК СУСП"),
				ProducerSynonym = new ProducerSynonym("Балканфарма - Троян АД")
			};
			order2.Items.Add(orderItem2);
			WaybillOrderMatcher.ComparisonWithOrders(document, new [] { order1, order2 });
			Assert.That(line1.OrderItems, Is.EquivalentTo(new [] {orderItem1}));
			Assert.That(line2.OrderItems, Is.EquivalentTo(new [] {orderItem2}));
		}

		private static Document GetDocument()
		{
			var log = new DocumentReceiveLog {
				Supplier = new Supplier {Name = "Тест"},
				ClientCode = 1,
				Address = new Address {Id = 2, Name = "Тест"},
				MessageUid = 123,
				DocumentSize = 100
			};
			return new Document(log);
		}
	}
}
