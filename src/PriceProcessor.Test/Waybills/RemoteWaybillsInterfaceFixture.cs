using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class RemoteWaybillsInterfaceFixture
	{
		private IWaybillService service;
		private ServiceHost host;

		[SetUp]
		public void Setup()
		{
			var binding = new NetTcpBinding();
			host = new ServiceHost(typeof(WaybillService));
			host.AddServiceEndpoint(typeof(IWaybillService), binding, "net.tcp://localhost:9846/Waybill");
			host.Open();

			var factory = new ChannelFactory<IWaybillService>(binding, "net.tcp://localhost:9846/Waybill");
			service = factory.CreateChannel();
		}

		[TearDown]
		public void TearDown()
		{
			host.Close();
		}

		[Test]
		public void Parse_documents_on_remote_call()
		{
			var file = "1008fo.pd";
			var client = TestOldClient.CreateTestClient();
			var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			
			var document = new TestDocumentLog {
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};
			using (new TransactionScope())
				document.Save();
			
			File.Copy(@"..\..\Data\Waybills\1008fo.pd", Path.Combine(waybillsPath, String.Format("{0}_1008fo.pd", document.Id)));

			service.ParseWaybill(new [] {document.Id});

			using(new SessionScope())
			{
				var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= document.LogTime).ToList();
				Assert.That(waybills.Count, Is.EqualTo(1));
				var waybill = waybills.Single();
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}
	}
}
