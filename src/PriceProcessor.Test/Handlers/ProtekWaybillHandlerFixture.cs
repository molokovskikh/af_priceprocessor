using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using NHibernate.Criterion;
using NHibernate.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using Test.Support;
using Test.Support.Helpers;
using System.Collections.Generic;
using System.IO;
using Common.Tools;
using Inforoom.PriceProcessor;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Handlers
{
	public class FakeProtekHandler : WaybillProtekHandler
	{
		public getBladingHeadersResponse headerResponce;
		public getBladingBodyResponse bodyResponce;

		public void Process()
		{
			Load(new ProtekServiceConfig(null, 261543, 1072995, 0));
		}

		public override void WithService(string uri, Action<EzakazWebService> action)
		{
			var service = MockRepository.GenerateStub<EzakazWebService>();
			service.Stub(s => s.getBladingHeaders(null)).IgnoreArguments().Return(headerResponce);
			service.Stub(s => s.getBladingBody(null)).IgnoreArguments().Return(bodyResponce);
			action(service);
		}
	}

	[TestFixture]
	public class ProtekWaybillHandlerFixture : IntegrationFixture
	{
		private TestClient client1;
		private TestOrder order1;

		private TestClient client2;
		private TestOrder order2;

		private FakeProtekHandler fake;
		private blading blading;
		private DateTime begin;
		private TestSupplier supplier;

		[SetUp]
		public void SetUp()
		{
			supplier = TestSupplier.CreateNaked();
			client1 = TestClient.CreateNaked();
			client2 = TestClient.CreateNaked();

			order1 = new TestOrder();
			order1.Client = client1;
			order1.Address = client1.Addresses[0];
			order1.Price = supplier.Prices[0];
			order1.Save();

			order2 = new TestOrder();
			order2.Client = client2;
			order2.Address = client2.Addresses[0];
			order2.Price = supplier.Prices[0];
			order2.Save();

			fake = new FakeProtekHandler();
			fake.IgnoreOrderFromId = 0;
			fake.IgnoreOrderToId = 100;

			fake.headerResponce = new getBladingHeadersResponse {
				@return = new eZakazXML {
					blading = new[] {
						new blading {
							bladingId = 1,
						},
					}
				}
			};

			blading = new blading {
				bladingId = 1,
				@uint = (int?)order1.Id,
				bladingItems = new[] {
					new bladingItem {
						itemId = 3345,
						itemName = "Коринфар таб п/о 10мг № 50",
						manufacturerName = "",
						bitemQty = 3,
						country = "Хорватия/Германия",
						prodexpiry = DateTime.Parse("17.02.2012"),
						distrPriceNds = 45.05,
						distrPriceWonds = 40.95,
						vitalMed = 1,
						sumVat = 12.3
					}
				},
			};
			fake.bodyResponce = new getBladingBodyResponse {
				@return = new eZakazXML {
					blading = new[] { blading }
				}
			};

			begin = DateTime.Now;
		}

		[Test]
		public void Process_protek_waybills()
		{
			var settings = WaybillSettings.Find(order1.Client.Id);
			//Формат сохранения в dbf теперь не является форматом по умолчанию
			settings.ProtekWaybillSavingType = WaybillFormat.ProtekDbf;
			settings.Save();

			fake.Process();

			var documents = Documents();
			Assert.That(documents.Count, Is.EqualTo(1));
			var document = documents[0];
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Коринфар таб п/о 10мг № 50"));
			Assert.That(line.NdsAmount, Is.EqualTo(12.3));
			var log = document.Log;
			Assert.That(log, Is.Not.Null);
			Assert.That(log.FileName, Is.EqualTo(String.Format("{0}.dbf", log.Id)));
			Assert.That(log.DocumentSize, Is.GreaterThan(0));
			Check_DocumentLine_SetProductId(document);
		}

		[Test]
		public void ProcessProtekWaybillsSst()
		{
			var settings = WaybillSettings.Find(order1.Client.Id);
			//По умолчанию форматом сохранения является формат sst
			Assert.That(settings.ProtekWaybillSavingType, Is.EqualTo(WaybillFormat.Sst));

			fake.Process();

			var documents = Documents();
			Assert.That(documents.Count, Is.EqualTo(1));
			var document = documents[0];
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Коринфар таб п/о 10мг № 50"));
			Assert.That(line.NdsAmount, Is.EqualTo(12.3));
			var log = document.Log;
			Assert.That(log, Is.Not.Null);
			Assert.That(log.FileName, Is.EqualTo(String.Format("{0}.sst", log.Id)));
			Assert.That(log.DocumentSize, Is.GreaterThan(0));
			Check_DocumentLine_SetProductId(document);
		}

		private List<Document> Documents()
		{
			var documents = Document.Queryable.Where(d => d.WriteTime >= begin && d.ClientCode == order1.Client.Id).ToList();
			return documents;
		}

		[Test]
		public void Save_document_id()
		{
			blading.bladingItems[0].bladingItemSeries = new[] {
				new bladingItemSeries {
					bladingItemSeriesCertificates = new[] {
						new bladingItemSeriesCertificate {
							docId = 5478
						}
					}
				},
			};

			fake.Process();
			var documents = Documents();
			Assert.That(documents[0].Lines[0].ProtekDocIds[0].DocId, Is.EqualTo(5478));
		}

		[Test]
		public void Process_protek_waybills_with_blading_folder()
		{
			Thread.Sleep(1000);
			fake.bodyResponce.@return.blading[0].@uint = null;
			fake.bodyResponce.@return.blading[0].bladingFolder = new[] {
				new bladingFolder {
					bladingId = null,
					folderNum = "",
					orderDate = null,
					orderId = null,
					orderNum = "",
					orderUdat = null,
					orderUdec = null,
					orderUint = (int?)order1.Id,
					orderUstr = ""
				},
				new bladingFolder {
					bladingId = null,
					folderNum = "",
					orderDate = null,
					orderId = null,
					orderNum = "",
					orderUdat = null,
					orderUdec = null,
					orderUint = (int?)order2.Id,
					orderUstr = ""
				}
			};

			DateTime begin = DateTime.Now;
			fake.Process();
			var documents = Documents();
			Assert.That(documents.Count, Is.EqualTo(1));
			Assert.That(documents[0].Lines.Count, Is.EqualTo(1));
			var line = documents[0].Lines[0];
			Assert.That(line.Product, Is.EqualTo("Коринфар таб п/о 10мг № 50"));
			Assert.That(line.NdsAmount, Is.EqualTo(12.3));
			Assert.That(documents[0].Log, Is.Not.Null);
			Check_DocumentLine_SetProductId(documents[0]);
			Assert.That(documents[0].OrderId, Is.EqualTo(order1.Id));
		}

		public void Check_DocumentLine_SetProductId(Document document)
		{
			var line = document.Lines[0];

			var listSynonym = new List<string> { line.Product };
			var priceCodes = Price.Queryable
				.Where(p => (p.Supplier.Id == document.FirmCode))
				.Select(p => (p.ParentSynonym ?? p.Id)).Distinct().ToList();

			if (priceCodes.Count < 0) {
				Assert.True(document.Lines.Count(l => l.ProductEntity == null) == document.Lines.Count);
				return;
			}

			var criteria = DetachedCriteria.For<TestSynonym>();
			criteria.Add(Restrictions.In("Synonym", listSynonym));
			criteria.Add(Restrictions.In("PriceCode", priceCodes));

			var synonym = SessionHelper.WithSession(c => criteria.GetExecutableCriteria(c).List<TestSynonym>()).ToList();
			if (synonym.Count > 0) {
				Assert.IsTrue(synonym.Select(s => s.ProductId).Contains(line.ProductEntity.Id));
			}
			else {
				Assert.IsTrue(line.ProductEntity == null);
			}
		}

		[Test]
		public void Test_Parse_and_Convert_to_Dbf()
		{
			client1.Settings.IsConvertFormat = true;
			client1.Settings.AssortimentPriceId = Core.Queryable.First().Price.Id;
			session.Flush();

			var docRoot = Path.Combine(Settings.Default.DocumentPath, order1.Address.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			if (Directory.Exists(waybillsPath)) Directory.Delete(waybillsPath, true);
			Directory.CreateDirectory(waybillsPath);

			fake.Process();

			var files = Directory.GetFiles(Path.Combine(docRoot, "Waybills"), "*.dbf");
			Assert.That(files.Count(), Is.EqualTo(1));
		}

		[Test]
		public void Detect_client_for_unknown_order_id()
		{
			var intersection = session.Query<TestAddressIntersection>()
				.First(a => a.Intersection.Price.Supplier.Id == supplier.Id
					&& a.Intersection.Client.Id == client1.Id
					&& a.Address.Id == client1.Addresses[0].Id);
			intersection.SupplierDeliveryId = "83943";

			Flush();
			session.Transaction.Commit();

			blading.recipientId = 83943;

			blading.@uint = null;
			var doc = fake.ToDocument(blading, new ProtekServiceConfig("", 0, 0, supplier.Id));
			Assert.That(doc.Address.Id, Is.EqualTo(client1.Addresses[0].Id));
		}

		[Test]
		public void Save_dump()
		{
			WaybillProtekHandler.Dump(".", blading);
		}
	}
}