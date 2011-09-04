using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Waybills;
using NHibernate.Criterion;
using NUnit.Framework;
using Rhino.Mocks;
using Test.Support;
using Test.Support.Helpers;
using System.Collections.Generic;
using System.IO;
using Common.Tools;
using Inforoom.PriceProcessor;

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

		public override void WithService(Action<ProtekService> action)
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
		private TestOrder order;
	    private TestOrder order2;
		private FakeProtekHandler fake;

		[SetUp]
		public void SetUp()
		{			
			using (new SessionScope())
			{
				order = TestOrder.Queryable.First();
                order2 = TestOrder.Queryable.Where(o => o != order).First();
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

			fake.bodyResponce = new getBladingBodyResponse
			{
				@return = new EZakazXML
				{
					blading = new[] {
						new Blading {
							bladingId = 1,
							@uint = (int?) order.Id,
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
						}
					}
				}
			};
		}

		[Test]
		public void Process_protek_waybills()
		{
            DateTime begin = DateTime.Now;
			fake.Process();		    
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(d => d.WriteTime >= begin && d.ClientCode == order.Client.Id).ToList();
				Assert.That(documents.Count, Is.EqualTo(1));
				Assert.That(documents[0].Lines.Count, Is.EqualTo(1));
				var line = documents[0].Lines[0];
				Assert.That(line.Product, Is.EqualTo("Коринфар таб п/о 10мг № 50"));
				Assert.That(line.NdsAmount, Is.EqualTo(12.3));
				Assert.That(documents[0].Log, Is.Not.Null);
				Assert.That(documents[0].Log.IsFake, Is.True);									
				Check_DocumentLine_SetProductId(documents[0]);
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
                        orderUint = (int?)order.Id, 
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
                var documents = Document.Queryable.Where(d => d.WriteTime >= begin && d.ClientCode == order.Client.Id).ToList();
                Assert.That(documents.Count, Is.EqualTo(1));
                Assert.That(documents[0].Lines.Count, Is.EqualTo(1));
                var line = documents[0].Lines[0];
                Assert.That(line.Product, Is.EqualTo("Коринфар таб п/о 10мг № 50"));
                Assert.That(line.NdsAmount, Is.EqualTo(12.3));
                Assert.That(documents[0].Log, Is.Not.Null);
                Assert.That(documents[0].Log.IsFake, Is.True);
                Check_DocumentLine_SetProductId(documents[0]);
                Assert.That(documents[0].OrderId, Is.EqualTo(order.Id));
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
				//Assert.True(document.Lines.Where(l => l.ProductId == null).Count() == document.Lines.Count);
				Assert.True(document.Lines.Where(l => l.ProductEntity == null).Count() == document.Lines.Count);
				return;
			}

			var criteria = DetachedCriteria.For<TestSynonym>();
			criteria.Add(Restrictions.In("Synonym", listSynonym));
			criteria.Add(Restrictions.In("PriceCode", priceCodes));

			var synonym = SessionHelper.WithSession(c => criteria.GetExecutableCriteria(c).List<TestSynonym>()).ToList();
			if (synonym.Count > 0)
			{
				//Assert.IsTrue(synonym.Select(s => s.ProductId).Contains(line.ProductId));
				Assert.IsTrue(synonym.Select(s => s.ProductId).Contains((int)line.ProductEntity.Id));
			}
			else
			{
				//Assert.IsTrue(line.ProductId == null);
				Assert.IsTrue(line.ProductEntity == null);
			}
		}

		[Test]
		public void Test_Parse_and_Convert_to_Dbf()
		{
			var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == order.Client.Id).SingleOrDefault();
			using (new TransactionScope())
			{
				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = (int)Core.Queryable.First().Price.Id;
				settings.SaveAndFlush();
			}			
			var docRoot = Path.Combine(Settings.Default.DocumentPath, order.Address != null ? order.Address.Id.ToString() : order.Client.Id.ToString());
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
	}
}