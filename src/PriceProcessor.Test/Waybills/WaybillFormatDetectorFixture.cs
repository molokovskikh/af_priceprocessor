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

		[SetUp]
		public void Setup()
		{
			var testSupplier = TestSupplier.Create();
			var testClient = TestClient.Create();
			supplier = session.Load<Supplier>(testSupplier.Id);
			address = session.Load<Address>(testClient.Addresses[0].Id);
		}

		[Test]
		public void Reject_duplicate_documents()
		{
			var log = new DocumentReceiveLog(supplier, address);
			var document = new Document(log) {ProviderDocumentId = "i-1"};
			var document1 = new Document(log) {ProviderDocumentId = "i-1"};

			document = WaybillFormatDetector.ProcessDocument(document, new List<OrderHead>());
			Assert.That(document, Is.EqualTo(document));
			session.Save(log);
			session.Save(document);

			document = WaybillFormatDetector.ProcessDocument(document1, new List<OrderHead>());
			Assert.That(document, Is.Null);
		}
	}
}