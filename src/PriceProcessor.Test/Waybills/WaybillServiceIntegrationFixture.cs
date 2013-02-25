using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
			Assert.That(savedDoc.First().RejectReason, Is.EqualTo(RejectReasonType.AddressDisable));
		}

		[Test, Description("Тестирует сопоставление накладной")]
		public void DocumentSetIdTest()
		{
			var price = TestSupplier.CreateTestSupplierWithPrice();
			var supplier = price.Supplier;
			var client = TestClient.CreateNaked();
			var testAddress = client.Addresses[0];
			Document doc;
			TestProduct product1, product2, product3, product4, product5, product6;
			TestProducer producer1, producer2, producer3;

			var order = new TestOrder();

			product1 = new TestProduct("Активированный уголь (табл.)");
			product1.CatalogProduct.Pharmacie = true;
			session.Save(product1);
			product2 = new TestProduct("Виагра (табл.)");
			product2.CatalogProduct.Pharmacie = true;
			session.Save(product2);
			product3 = new TestProduct("Крем для кожи (гель.)");
			product3.CatalogProduct.Pharmacie = false;
			session.Save(product3);
			product4 = new TestProduct("Эластичный бинт");
			product4.CatalogProduct.Pharmacie = false;
			session.Save(product4);
			product5 = new TestProduct("Стерильные салфетки");
			product5.CatalogProduct.Pharmacie = false;
			session.Save(product5);
			product6 = new TestProduct("Аспирин (табл.)");
			product6.CatalogProduct.Pharmacie = false;
			session.Save(product6);

			producer1 = new TestProducer("ВероФарм");
			session.Save(producer1);
			producer2 = new TestProducer("Пфайзер");
			session.Save(producer2);
			producer3 = new TestProducer("Воронежская Фармацевтическая компания");
			session.Save(producer3);
			session.Save(new TestSynonym() { Synonym = "Активированный уголь", ProductId = product1.Id, PriceCode = (int?)price.Id });
			session.Save(new TestSynonym() { Synonym = "Виагра", ProductId = product2.Id, PriceCode = (int?)price.Id });
			session.Save(new TestSynonym() { Synonym = "Крем для кожи", ProductId = product3.Id, PriceCode = (int?)price.Id });
			session.Save(new TestSynonym() { Synonym = "Аспирин", ProductId = product6.Id, PriceCode = (int?)price.Id });


			session.Save(new TestSynonymFirm() { Synonym = "ВероФарм", CodeFirmCr = (int?)producer1.Id, PriceCode = (int?)price.Id });
			session.Save(new TestSynonymFirm() { Synonym = "Пфайзер", CodeFirmCr = (int?)producer1.Id, PriceCode = (int?)price.Id });
			session.Save(new TestSynonymFirm() { Synonym = "Пфайзер", CodeFirmCr = (int?)producer2.Id, PriceCode = (int?)price.Id });
			session.Save(new TestSynonymFirm() { Synonym = "Верофарм", CodeFirmCr = (int?)producer2.Id, PriceCode = (int?)price.Id });
			session.Save(new TestSynonymFirm() { Synonym = "ВоронежФарм", CodeFirmCr = (int?)producer3.Id, PriceCode = (int?)price.Id });

			TestAssortment.CheckAndCreate(product1, producer1);
			TestAssortment.CheckAndCreate(product2, producer2);

			var supplierCode2 = new SupplierCode {
				Code = "45678",
				Supplier = session.Load<Supplier>(supplier.Id),
				ProducerId = (int)producer2.Id,
				Product = session.Load<Product>(product2.Id),
				CodeCr = "1"
			};
			session.Save(supplierCode2);
			var supplierCode4 = new SupplierCode {
				Code = "789",
				Supplier = session.Load<Supplier>(supplier.Id),
				ProducerId = (int)producer2.Id,
				Product = session.Load<Product>(product4.Id),
				CodeCr = "2"
			};
			session.Save(supplierCode4);
			var supplierCode5 = new SupplierCode {
				Code = "12345",
				Supplier = session.Load<Supplier>(supplier.Id),
				ProducerId = (int)producer3.Id,
				Product = session.Load<Product>(product5.Id),
				CodeCr = "3"
			};
			session.Save(supplierCode5);

			var log = new DocumentReceiveLog() {
				Supplier = session.Load<Supplier>(supplier.Id),
				ClientCode = client.Id,
				Address = session.Load<Address>(testAddress.Id),
				MessageUid = 123,
				DocumentSize = 100
			};

			doc = new Document(log) {
				OrderId = order.Id,
				Address = log.Address,
				DocumentDate = DateTime.Now
			};

			// сопоставляется по наименованию, фармацевтика, product1, producer1
			var line = doc.NewLine();
			line.Product = "Активированный уголь";
			line.Producer = "ВероФарм";

			// сопоставляется по коду, product2, producer2
			line = doc.NewLine();
			line.Product = "Виагра";
			line.Producer = " Тестовый производитель  ";
			line.Code = "45678";
			line.CodeCr = "1";

			// сопоставляется по наименованию, product3, производитель - null
			line = doc.NewLine();
			line.Product = " КРЕМ ДЛЯ КОЖИ  ";
			line.Producer = "Тестовый производитель";

			// сопоставляется по коду, product4, producer2
			line = doc.NewLine();
			line.Product = "эластичный бинт";
			line.Producer = "Воронежфарм";
			line.Code = "789";
			line.CodeCr = "2";

			// сопоставляется по коду, product5, producer3
			line = doc.NewLine();
			line.Product = "Салфетки";
			line.Producer = "Воронежфарм";
			line.Code = "12345";
			line.CodeCr = "3";

			// сопоставляется по наименованию, потому как такого кода нет в базе, product6, producer3
			line = doc.NewLine();
			line.Product = "Аспирин";
			line.Producer = "Воронежфарм";
			line.Code = "1952";

			// не сопоставляется, везде null
			line = doc.NewLine();
			line.Product = "Неизвестный продукт";
			line.Producer = "Воронежфарм";
			line.Code = "1952";

			Reopen();

			doc.SetProductId();

			Assert.That(doc.Lines[0].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[0].ProductEntity.Id, Is.EqualTo(product1.Id));
			Assert.That(doc.Lines[0].ProducerId, Is.EqualTo(producer1.Id));
			Assert.That(doc.Lines[1].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[1].ProductEntity.Id, Is.EqualTo(product2.Id));
			Assert.That(doc.Lines[1].ProducerId, Is.EqualTo(producer2.Id));
			Assert.That(doc.Lines[2].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[2].ProductEntity.Id, Is.EqualTo(product3.Id));
			Assert.That(doc.Lines[2].ProducerId, Is.Null);
			Assert.That(doc.Lines[3].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[3].ProductEntity.Id, Is.EqualTo(product4.Id));
			Assert.That(doc.Lines[3].ProducerId, Is.EqualTo(producer2.Id));
			Assert.That(doc.Lines[4].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[4].ProductEntity.Id, Is.EqualTo(product5.Id));
			Assert.That(doc.Lines[4].ProducerId, Is.EqualTo(producer3.Id));
			Assert.That(doc.Lines[5].ProductEntity, Is.Not.Null);
			Assert.That(doc.Lines[5].ProductEntity.Id, Is.EqualTo(product6.Id));
			Assert.That(doc.Lines[5].ProducerId, Is.EqualTo(producer3.Id));
			Assert.That(doc.Lines[6].ProductEntity, Is.Null);
			Assert.That(doc.Lines[6].ProducerId, Is.Null);
		}
	}
}
