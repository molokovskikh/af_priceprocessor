using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class RemoteWaybillsInterfaceFixture
	{
		IWaybillService service;
		ServiceHost host;
		TestClient client;
		string waybillsPath;
		string rejectsPath;
		
		TestClient client_dbf;
		string waybillsPath_dbf;
		string rejectsPath_dbf;

		[SetUp]
		public void Setup()
		{
			var binding = new NetTcpBinding();
			host = new ServiceHost(typeof(WaybillService));
			host.AddServiceEndpoint(typeof(IWaybillService), binding, "net.tcp://localhost:9846/Waybill");
			host.Open();

			var factory = new ChannelFactory<IWaybillService>(binding, "net.tcp://localhost:9846/Waybill");
			service = factory.CreateChannel();

			client = TestClient.Create();
			//var docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client.Id.ToString());
			var docRoot = Path.Combine(Settings.Default.DocumentPath, client.Id.ToString());
			waybillsPath = Path.Combine(docRoot, "Waybills");
			rejectsPath = Path.Combine(docRoot, "Rejects");
			Directory.CreateDirectory(rejectsPath);
			Directory.CreateDirectory(waybillsPath);

			Setup_Parse_Convert_Dbf_format();
		}

		private void Setup_Parse_Convert_Dbf_format()
		{
			//client_dbf = TestOldClient.CreateTestClient(1UL, true);
			client_dbf = TestClient.Create();
			//var _docRoot = Path.Combine(Settings.Default.FTPOptBoxPath, client_dbf.Id.ToString());
			var _docRoot = Path.Combine(Settings.Default.DocumentPath, client_dbf.Id.ToString());
			waybillsPath_dbf = Path.Combine(_docRoot, "Waybills");
			rejectsPath_dbf = Path.Combine(_docRoot, "Rejects");
			Directory.CreateDirectory(rejectsPath_dbf);
			Directory.CreateDirectory(waybillsPath_dbf);
		}

		private string GetDocumentDir(uint? AddressId, uint? ClientCode)
		{
			var code = AddressId.HasValue ? AddressId.Value : ClientCode;
		//	var clientDir = Path.Combine(Settings.Default.WaybillsPath, code.ToString().PadLeft(3, '0'));
			//var clientDir = Path.Combine(Settings.Default.FTPOptBoxPath, code.ToString().PadLeft(3, '0'));
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

		[Test]
		public void Parse_documents_on_remote_call_Convert_Dbf_format()
		{
			var file = "1008fo.pd";

			var document = new TestDocumentLog
			{
				ClientCode = client_dbf.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Waybill,
				FileName = file,
			};

			var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == client_dbf.Id).SingleOrDefault();
			using (new TransactionScope())
			{			
				settings.AssortimentPriceId = (int)Core.Queryable.First().Price.Id;
				settings.SaveAndFlush();
			}

			using (new TransactionScope())
				document.Save();

			File.Copy(@"..\..\Data\Waybills\1008fo.pd", Path.Combine(waybillsPath_dbf, String.Format("{0}_1008fo.pd", document.Id)));

			service.ParseWaybill(new[] { document.Id });

			using (new SessionScope())
			{
				var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= document.LogTime).ToList();
				Assert.That(waybills.Count, Is.EqualTo(1));
				var waybill = waybills.Single();
				Assert.That(waybill.Lines.Count, Is.EqualTo(1));
				
				var logs = TestDocumentLog.Queryable.Where(l => l.FirmCode == 1179 && l.ClientCode == client_dbf.Id).ToList();
				var count = logs.Count;
				Assert.That(count, Is.EqualTo(2));

				var log_fake = logs.Where(l => l.IsFake).ToList();
				Assert.That(log_fake.Count, Is.EqualTo(1));
				Assert.That(log_fake[0].FileName, Is.EqualTo(file));
				Assert.That(waybill.DownloadId, Is.EqualTo(log_fake[0].Id));

				var log = logs.Where(l => !l.IsFake).ToList();
				Assert.That(log.Count, Is.EqualTo(1));

				var Supplier_ShortName = Supplier.Queryable.Where(s => s.Id == 1179).Select(s => s.ShortName).Single().ToString();
				var filename = GetRemoteFileNameExt(Supplier_ShortName, document.AddressId, document.ClientCode, file, log[0].Id);
				Assert.That(log[0].FileName, Is.EqualTo(Path.GetFileNameWithoutExtension(filename) + ".dbf"));

				var data = Dbf.Load(Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".dbf"), Encoding.GetEncoding(866));
				Assert.IsTrue(data.Columns.Contains("postid_af"));
				Assert.IsTrue(data.Columns.Contains("ttn"));
				Assert.IsTrue(data.Columns.Contains("przv_post"));
			}
		}

		[Test]
		public void Reject_should_not_be_parsed()
		{
			var file = "1008fo.pd";

			var document = new TestDocumentLog
			{
				ClientCode = client.Id,
				FirmCode = 1179,
				LogTime = DateTime.Now,
				DocumentType = DocumentType.Reject,
				FileName = file,
			};

			using (new TransactionScope())
				document.Save();

			File.Copy(@"..\..\Data\Waybills\1008fo.pd", Path.Combine(rejectsPath, String.Format("{0}_1008fo.pd", document.Id)));

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

			var documentLogs = new List<TestDocumentLog>();
			foreach (var file in files)
			{
				var document = new TestDocumentLog {
					ClientCode = client.Id,
					FirmCode = 1179,
					LogTime = DateTime.Now,
					DocumentType = DocumentType.Waybill,
					FileName = file,
				};
				using (new TransactionScope())
					document.Save();
				documentLogs.Add(document);

				File.Copy(file, Path.Combine(waybillsPath,
					String.Format("{0}_{1}{2}", document.Id, Path.GetFileNameWithoutExtension(file), Path.GetExtension(file))));
			}

			service.ParseWaybill(documentLogs.Select(doc => doc.Id).ToArray());

			using (new SessionScope())
			{
				foreach (var documentLog in documentLogs)
				{
					var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= documentLog.LogTime).ToList();
					Assert.That(waybills.Count, Is.EqualTo(1));

					var documents = TestWaybill.Queryable.Where(doc => doc.ClientCode == documentLog.ClientCode).ToList();
					Assert.That(documents.Count, Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void Parse_multifile_Convert_Dbf_Format()
		{
			var files = new[] { @"..\..\Data\Waybills\multifile\b271433.dbf", @"..\..\Data\Waybills\multifile\h271433.dbf" };
			
			uint? supplierId = 1179;

			var documentLogs = new List<TestDocumentLog>();
			foreach (var file in files)
			{
				var document = new TestDocumentLog
				{
					ClientCode = client_dbf.Id,
					FirmCode = supplierId,
					LogTime = DateTime.Now,
					DocumentType = DocumentType.Waybill,
					FileName = file,
				};
				using (new TransactionScope())
					document.Save();
				documentLogs.Add(document);

				var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == client_dbf.Id).SingleOrDefault();
				using (new TransactionScope())
				{
					settings.AssortimentPriceId = (int)Core.Queryable.First().Price.Id;
					settings.SaveAndFlush();
				}

				File.Copy(file, Path.Combine(waybillsPath_dbf,
					String.Format("{0}_{1}{2}", document.Id, Path.GetFileNameWithoutExtension(file), Path.GetExtension(file))));
			}

			service.ParseWaybill(documentLogs.Select(doc => doc.Id).ToArray());

			using (new SessionScope())
			{
				foreach (var documentLog in documentLogs)
				{
					var waybills = TestWaybill.Queryable.Where(w => w.WriteTime >= documentLog.LogTime).ToList();
					Assert.That(waybills.Count, Is.EqualTo(1));

					var documents = TestWaybill.Queryable.Where(doc => doc.ClientCode == documentLog.ClientCode).ToList();
					Assert.That(documents.Count, Is.EqualTo(1));
				}

				// Проверяем наличие записей в document_logs
				var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == 1179 && log.ClientCode == client_dbf.Id);
				Assert.That(logs.Count(), Is.EqualTo(3));

				// Проверяем наличие записей в documentheaders

				var count = 0;
				foreach (var documentLog in logs)
				{
					count += Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id).Count();
				}
				Assert.That(count, Is.EqualTo(1));
				
				// Проверяем наличие файлов в папках клиентов
				//var clientDir = Path.Combine(Settings.Default.FTPOptBoxPath, client_dbf.Id.ToString());
				var clientDir = Path.Combine(Settings.Default.DocumentPath, client_dbf.Id.ToString());
				Assert.IsTrue(Directory.Exists(clientDir));
				var _files = Directory.GetFiles(Path.Combine(clientDir, "Waybills"));
				Assert.That(_files.Count(), Is.GreaterThan(0));
			}
		}
	}
}
