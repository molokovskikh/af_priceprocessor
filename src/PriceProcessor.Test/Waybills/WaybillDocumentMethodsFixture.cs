using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class WaybillDocumentMethodsFixture : IntegrationFixture
	{
		private TestProduct _product;
		private TestSupplier _supplier;
		private TestProducer _producer;
		private Document _doc;
		private SupplierCode _supplierCode;
		private DocumentLine _documentLine;

		[SetUp]
		public void SetUp()
		{
			var priceParent = TestSupplier.Create().Prices[0];
			var price = TestSupplier.Create().Prices[0];
			price.ParentSynonym = priceParent.Id;
			Save(price);
			_supplier = price.Supplier;
			var client = TestClient.CreateNaked();
			var testAddress = client.Addresses[0];

			var order = new TestOrder();

			_product = new TestProduct("Виагра (табл.)");
			_product.CatalogProduct.Pharmacie = true;
			session.Save(_product);

			_producer = new TestProducer("Пфайзер");
			session.Save(_producer);
			session.Save(new TestSynonym() { Synonym = "Виагра", ProductId = _product.Id, PriceCode = (int?)price.Id });

			session.Save(new TestSynonymFirm() { Synonym = "Пфайзер", CodeFirmCr = (int?)_producer.Id, PriceCode = (int?)price.Id });

			TestAssortment.CheckAndCreate(_product, _producer);

			var log = new DocumentReceiveLog() {
				Supplier = session.Load<Supplier>(_supplier.Id),
				ClientCode = client.Id,
				Address = session.Load<Address>(testAddress.Id),
				MessageUid = 123,
				DocumentSize = 100
			};

			_doc = new Document(log) {
				OrderId = order.Id,
				Address = log.Address,
				DocumentDate = DateTime.Now
			};

			_supplierCode = new SupplierCode {
				Code = "45678",
				Supplier = session.Load<Supplier>(_supplier.Id),
				ProducerId = _producer.Id,
				Product = session.Load<Product>(_product.Id),
				CodeCr = string.Empty
			};
			session.Save(_supplierCode);

			// сопоставляется по коду
			_documentLine = _doc.NewLine();
			_documentLine.Product = "Виагра";
			_documentLine.Producer = " Тестовый производитель  ";
			_documentLine.Code = "45678";
		}


		[Test]
		public void DocumentSetIdWithParentSynonymTest()
		{
			Reopen();

			_doc.SetProductId();

			Assert.That(_doc.Lines[0].ProductEntity, Is.Not.Null);
			Assert.That(_doc.Lines[0].ProductEntity.Id, Is.EqualTo(_product.Id));
			Assert.That(_doc.Lines[0].ProducerId, Is.EqualTo(_producer.Id));
		}

		[Test]
		public void DocumentNoSetIdWithParentSynonymAndCodeCrTest()
		{
			_supplierCode.CodeCr = "123456";
			Save(_supplierCode);

			Reopen();

			_doc.SetProductId();

			Assert.That(_doc.Lines[0].ProductEntity, Is.Null);
			Assert.That(_doc.Lines[0].ProducerId, Is.Null);
		}

		[Test]
		public void DocumentSetIdWithParentSynonymAndCodeCrTest()
		{
			_supplierCode.CodeCr = "123456";
			Save(_supplierCode);
			_documentLine.CodeCr = "123456";

			Reopen();

			_doc.SetProductId();

			Assert.That(_doc.Lines[0].ProductEntity, Is.Not.Null);
			Assert.That(_doc.Lines[0].ProductEntity.Id, Is.EqualTo(_product.Id));
			Assert.That(_doc.Lines[0].ProducerId, Is.EqualTo(_producer.Id));
		}
	}
}
