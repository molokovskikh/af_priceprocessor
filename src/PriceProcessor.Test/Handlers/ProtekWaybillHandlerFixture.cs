using System;
using System.Linq;
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

namespace PriceProcessor.Test.Handlers
{
	public class FakeProtekHandler : ProtekWaybillHandler
	{
		public getBladingHeadersResponse headerResponce;
		public getBladingBodyResponse bodyResponce;

		public void Process()
		{
			ProcessData();
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
		[Test]
		public void Process_protek_waybills()
		{
			var begin = DateTime.Now;
			uint orderId;
			using (new SessionScope())
			{
				orderId = TestOrder.Queryable.First().Id;
			}

			var fake = new FakeProtekHandler();

			fake.headerResponce = new getBladingHeadersResponse {
				@return = new EZakazXML {
					blading = new[] {
						new Blading {
							bladingId = 1,
						},
					}
				}
			};

			fake.bodyResponce = new getBladingBodyResponse {
				@return = new EZakazXML {
					blading = new [] {
						new Blading {
							bladingId = 1,
							@uint = (int?) orderId,
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
								},
							}
						}
					}
				}
			};

			uint testClientId = 79888;
			var settings = TestDrugstoreSettings.TryFind(Convert.ToUInt32(testClientId));
			if (settings == null)
			{
				using (new SessionScope())
				{
					settings = new TestDrugstoreSettings
					           	{
									Id = testClientId,
					           		IsConvertFormat = true
					           	};
					settings.SaveAndFlush();
				}
			}

			DateTime log_time = DateTime.Now;

			fake.Process();

			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(d => d.WriteTime >= begin).ToList();
				Assert.That(documents.Count, Is.EqualTo(14));
				Assert.That(documents[0].Lines.Count, Is.EqualTo(1));
				Assert.That(documents[0].Lines[0].Product, Is.EqualTo("Коринфар таб п/о 10мг № 50"));
				Assert.That(documents[0].Log, Is.Not.Null);
				Assert.That(documents[0].Log.IsFake, Is.True);
					
				var log_dbf = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == documents[0].Log.Supplier.Id
															  && l.ClientCode == documents[0].Log.ClientCode
															  && l.AddressId == documents[0].Log.AddressId
															  && l.LogTime >= log_time)
														.OrderBy(l => l.Id)
														.ToList().First();
				var file = Path.Combine(Path.GetDirectoryName(log_dbf.GetRemoteFileNameExt()), log_dbf.FileName);

				var table = Dbf.Load(file);

				Assert.That(table.Rows.Count, Is.EqualTo(1));
				Assert.That(Convert.ToUInt32(table.Rows[0]["postid_af"]), Is.EqualTo(105));
				//Assert.That(table.Rows[0]["post_name_af"], Is.EqualTo("ООО \"ЮКОН-фарм\"")); //При сохранении в dbf имя колонки ограничено 10-ю символами


				Check_DocumentLine_SetProductId(documents[0]);
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
				Assert.True(document.Lines.Where(l => l.ProductId == null).Count() == document.Lines.Count);
				return;
			}

			var criteria = DetachedCriteria.For<TestSynonym>();
			criteria.Add(Restrictions.In("Synonym", listSynonym));
			criteria.Add(Restrictions.In("PriceCode", priceCodes));

			var synonym = SessionHelper.WithSession(c => criteria.GetExecutableCriteria(c).List<TestSynonym>()).ToList();
			if (synonym.Count > 0)
			{
				Assert.IsTrue(synonym.Select(s => s.ProductId).Contains(line.ProductId));
			}
			else
			{
				Assert.IsTrue(line.ProductId == null);
			}
		}
	}
}