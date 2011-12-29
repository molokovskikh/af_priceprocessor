using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class RemoteWaybillsInterfaceFixture : DocumentFixture
	{
		IWaybillService service;
		ServiceHost host;

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

		private string GetDocumentDir(uint? AddressId, uint? ClientCode)
		{
			var code = AddressId.HasValue ? AddressId.Value : ClientCode;
			var clientDir = Path.Combine(Settings.Default.DocumentPath, code.ToString().PadLeft(3, '0'));
			return Path.Combine(clientDir, "Waybill" + "s");
		}

		private string GetRemoteFileName(string FileName, uint Id, string Supplier_ShortName, uint? AddressId, uint? ClientCode)
		{
			var documentDir = GetDocumentDir(AddressId, ClientCode);
			var file = String.Format("{0}_{1}",
				Id,
				Path.GetFileName(FileName));
			var fullName = Path.Combine(documentDir, file);
			if (!File.Exists(fullName))
			{
				file = String.Format("{0}_{1}({2}){3}",
					Id,
					Supplier_ShortName,
					Path.GetFileNameWithoutExtension(FileName),
					Path.GetExtension(FileName));
				return Path.Combine(documentDir, file);
			}
			return fullName;
		}

		private string GetRemoteFileNameExt(string Supplier_ShortName, uint? AddressId, uint? ClientCode, string FileName, uint logId)
		{
			var clientDirectory = GetDocumentDir(AddressId, ClientCode);

			if (!Directory.Exists(clientDirectory))
				Directory.CreateDirectory(clientDirectory);

			return GetRemoteFileName(FileName, logId, Supplier_ShortName, AddressId, ClientCode);
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
			var document = CreateTestLog(file);

			service.ParseWaybill(new [] {document.Id});

			using(new SessionScope())
			{
				var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= document.LogTime).ToList();
				Assert.That(waybills.Count, Is.EqualTo(1));
				var waybill = waybills.Single();
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void Parse_documents_on_remote_call_Convert_Dbf_format()
		{
			var file = "1008fo.pd";
			var document = CreateTestLog(file);

			using (new TransactionScope())
			{
				settings.AssortimentPriceId = price.Id;
				settings.IsConvertFormat = true;
				settings.SaveAndFlush();
			}

			service.ParseWaybill(new[] { document.Id });

			using (new SessionScope())
			{
				var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= document.LogTime).ToList();
				Assert.That(waybills.Count, Is.EqualTo(1));
				var waybill = waybills.Single();
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
				
				var logs = TestDocumentLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.Client.Id == client.Id).ToList();
				var count = logs.Count;
				Assert.That(count, Is.EqualTo(2));

				var log_fake = logs.Where(l => l.IsFake).ToList();
				Assert.That(log_fake.Count, Is.EqualTo(1));
				Assert.That(log_fake[0].FileName, Is.EqualTo(file));
				Assert.That(waybill.Log.Id, Is.EqualTo(log_fake[0].Id));

				var log = logs.Where(l => !l.IsFake).ToList();
				Assert.That(log.Count, Is.EqualTo(1));

				Assert.That(log[0].FileName, Is.EqualTo(Path.ChangeExtension(file, ".dbf")));

				var resultFile = Path.ChangeExtension(GetRemoteFileName(file, log[0].Id, supplier.Name, address.Id, client.Id), ".dbf");
				var data = Dbf.Load(resultFile, Encoding.GetEncoding(866));
				Assert.IsTrue(data.Columns.Contains("postid_af"));
				Assert.IsTrue(data.Columns.Contains("ttn"));
				Assert.IsTrue(data.Columns.Contains("przv_post"));
			}
		}

		[Test]
		public void Reject_should_not_be_parsed()
		{
			var file = "1008fo.pd";
			var document = CreateTestLog(file);
			document.DocumentType = DocumentType.Reject;
			document.Save();

			service.ParseWaybill(new[] { document.Id });

			using (new SessionScope())
			{
				var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= document.LogTime).ToList();
				Assert.That(waybills.Count, Is.EqualTo(0));
			}
		}

		[Test]
		public void Parse_multifile()
		{
			var files = new[] { @"..\..\Data\Waybills\multifile\b271433.dbf", @"..\..\Data\Waybills\multifile\h271433.dbf" };
			var documentLogs = CreateTestLogs(files);

			service.ParseWaybill(documentLogs.Select(doc => doc.Id).ToArray());

			using (new SessionScope())
			{
				foreach (var documentLog in documentLogs)
				{
					var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= documentLog.LogTime).ToList();
					Assert.That(waybills.Count, Is.EqualTo(1));

					var documents = TestWaybill.Queryable.Where(doc => doc.Client == documentLog.Client).ToList();
					Assert.That(documents.Count, Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void Parse_multifile_Convert_Dbf_Format()
		{
			var files = new[] { @"..\..\Data\Waybills\multifile\b271433.dbf", @"..\..\Data\Waybills\multifile\h271433.dbf" };
			var documentLogs = CreateTestLogs(files);
			using (new TransactionScope())
			{
				settings.AssortimentPriceId = price.Id;
				settings.IsConvertFormat = true;
				settings.SaveAndFlush();
			}

			service.ParseWaybill(documentLogs.Select(doc => doc.Id).ToArray());

			using (new SessionScope())
			{
				foreach (var documentLog in documentLogs)
				{
					var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= documentLog.LogTime).ToList();
					Assert.That(waybills.Count, Is.EqualTo(1));

					var documents = TestWaybill.Queryable.Where(doc => doc.Client == documentLog.Client).ToList();
					Assert.That(documents.Count, Is.EqualTo(1));
				}

				// Проверяем наличие записей в document_logs
				var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id && log.ClientCode == client.Id);
				Assert.That(logs.Count(), Is.EqualTo(3));

				// Проверяем наличие записей в documentheaders

				var count = 0;
				foreach (var documentLog in logs)
				{
					count += Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id).Count();
				}
				Assert.That(count, Is.EqualTo(1));
				
				// Проверяем наличие файлов в папках клиентов
				var resultFiles = Directory.GetFiles(waybillsPath);
				Assert.That(resultFiles.Count(), Is.GreaterThan(0));
			}
		}
	}
}
