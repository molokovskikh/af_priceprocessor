using System;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using Rhino.Mocks;
using Test.Support;

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
			
			fake.Process();

			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(d => d.WriteTime >= begin).ToList();
				Assert.That(documents.Count, Is.EqualTo(2));
				Assert.That(documents[0].Lines.Count, Is.EqualTo(1));
				Assert.That(documents[0].Lines[0].Product, Is.EqualTo("Коринфар таб п/о 10мг № 50"));
				Assert.That(documents[0].Log, Is.Not.Null);
			}
		}
	}
}