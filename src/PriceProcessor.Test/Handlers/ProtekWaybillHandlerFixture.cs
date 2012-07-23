using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate.Criterion;
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
	public class FakeProtekHandler : ProtekWaybillHandler
	{
		public getBladingHeadersResponse headerResponce;
		public getBladingBodyResponse bodyResponce;

		public void Process()
		{
			Load(261543, 1072995);
		}

		public override void WithService(string uri, Action<ProtekService> action)
		{
			var service = MockRepository.GenerateStub<ProtekService>();
			service.Stub(s => s.getBladingHeaders(null)).IgnoreArguments().Return(headerResponce);
			service.Stub(s => s.getBladingBody(null)).IgnoreArguments().Return(bodyResponce);
			action(service);
		}
	}

	[TestFixture]
	public class ProtekWaybillHandlerFixture
	{
		private TestClient client1;
		private TestOrder order1;

		private TestClient client2;
		private TestOrder order2;

		private FakeProtekHandler fake;
		private Blading blading;
		private DateTime begin;

		[SetUp]
		public void SetUp()
		{			
			using (new SessionScope()) {
				client1 = TestClient.Create();
				client2 = TestClient.Create();
				var price = TestSupplier.CreateTestSupplierWithPrice();
				order1 = new TestOrder();
				order1.Client = client1;
				order1.Address = client1.Addresses[0];
				order1.Price = price;
				order1.Save();

				order2 = new TestOrder();
				order2.Client = client2;
				order2.Address = client2.Addresses[0];
				order2.Price = price;
				order2.Save();
			}

			fake = new FakeProtekHandler();

			fake.headerResponce = new getBladingHeadersResponse {
				@return = new EZakazXML {
					blading = new[] {
						new Blading {
							bladingId = 1,
						},
					}
				}
			};

			blading = new Blading {
				bladingId = 1,
				@uint = (int?) order1.Id,
				bladingItems = new [] {
					new BladingItem {
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
				@return = new EZakazXML {
					blading = new[] { blading }
				}
			};

			begin = DateTime.Now;
		}

		[Test]
		public void Process_protek_waybills()
		{
			using (new SessionScope()) {
				var settings = WaybillSettings.Find(order1.Client.Id);
				//Формат сохранения в dbf теперь не является форматом по умолчанию
				settings.ProtekWaybillSavingType = WaybillFormat.DBF;
				settings.Save();
			}

			fake.Process();
			using (new SessionScope())
			{
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
		}

		[Test]
		public void ProcessProtekWaybillsSst()
		{
			using (new SessionScope()) {
				var settings = WaybillSettings.Find(order1.Client.Id);
				//По умолчанию форматом сохранения является формат sst
				Assert.That(settings.ProtekWaybillSavingType, Is.EqualTo(WaybillFormat.SST));
			}

			fake.Process();
			using (new SessionScope())
			{
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
				new BladingItemSeries {
					bladingItemSeriesCertificates = new [] {
						new BladingItemSeriesCertificate {
							docId = 5478
						}
					}
				}, 
			};

			fake.Process();
			using(new SessionScope())
			{
				var documents = Documents();
				Assert.That(documents[0].Lines[0].ProtekDocIds[0].DocId, Is.EqualTo(5478));
			}
		}

		[Test]
		public void Process_protek_waybills_with_blading_folder()
		{
			Thread.Sleep(1000);
			fake.bodyResponce.@return.blading[0].@uint = null;
			fake.bodyResponce.@return.blading[0].bladingFolder = new[]{
				new BladingFolder
					{
						bladingId = null, 
						folderNum  = "", 
						orderDate = null, 
						orderId = null, 
						orderNum = "", 
						orderUdat = null, 
						orderUdec = null, 
						orderUint = (int?)order1.Id, 
						orderUstr = ""
					},
				new BladingFolder
					{
						bladingId = null, 
						folderNum  = "", 
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
			using (new SessionScope())
			{
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
		}

		public void Check_DocumentLine_SetProductId(Document document)
		{
			var line = document.Lines[0];

			var listSynonym = new List<string> { line.Product };
			var priceCodes = Price.Queryable
									.Where(p => (p.Supplier.Id == document.FirmCode))
									.Select(p => (p.ParentSynonym ?? p.Id)).Distinct().ToList();

			if (priceCodes.Count < 0)
			{
				Assert.True(document.Lines.Count(l => l.ProductEntity == null) == document.Lines.Count);
				return;
			}

			var criteria = DetachedCriteria.For<TestSynonym>();
			criteria.Add(Restrictions.In("Synonym", listSynonym));
			criteria.Add(Restrictions.In("PriceCode", priceCodes));

			var synonym = SessionHelper.WithSession(c => criteria.GetExecutableCriteria(c).List<TestSynonym>()).ToList();
			if (synonym.Count > 0)
			{
				Assert.IsTrue(synonym.Select(s => s.ProductId).Contains(line.ProductEntity.Id));
			}
			else
			{
				Assert.IsTrue(line.ProductEntity == null);
			}
		}

		[Test]
		public void Test_Parse_and_Convert_to_Dbf()
		{
			client1.Settings.IsConvertFormat = true;
			var settings = TestDrugstoreSettings.Queryable.SingleOrDefault(s => s.Id == order1.Client.Id);
			
			using (new TransactionScope())
			{
				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = Core.Queryable.First().Price.Id;
				settings.SaveAndFlush();
			}
			var docRoot = Path.Combine(Settings.Default.DocumentPath, order1.Address != null ? order1.Address.Id.ToString() : order1.Client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			if(Directory.Exists(waybillsPath)) Directory.Delete(waybillsPath, true);
			Directory.CreateDirectory(waybillsPath);
			fake.Process();
			using (new TransactionScope())
			{
				settings.IsConvertFormat = false;
				settings.AssortimentPriceId = null;
				settings.SaveAndFlush();
			}
			var files_dbf = Directory.GetFiles(Path.Combine(docRoot, "Waybills"), "*.dbf");
			Assert.That(files_dbf.Count(), Is.EqualTo(1));
		}

		[Test]
		public void Save_dump()
		{
			ProtekWaybillHandler.Dump(".", blading);
		}
	}
}