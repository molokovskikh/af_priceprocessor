using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Common.Tools.Jobs;
using Inforoom.Downloader.Documents;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net;
using LumiSoft.Net.FTP.Server;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;
using Castle.ActiveRecord;
using Common.Tools;
using log4net.Config;

namespace PriceProcessor.Test.Waybills.Handlers
{
	[TestFixture]
	public class WaybillFtpSourceHandlerFixture : BaseWaybillHandlerFixture
	{
		private string ftpHost;
		private int ftpPort;
		private string ftpWaybillDirectory;
		private string user;
		private string password;
		private uint supplierDeliveryId;
		private string ftpRejectDirectory;

		private WaybillFtpSourceHandler handler;
		private IntPtr jobHandle;

		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();

			using (new SessionScope()) {
				var supplierFtpSources = TestWaybillSource.Queryable.Where(source =>
					source.SourceType == TestWaybillSourceType.FtpSupplier);
				foreach (var source in supplierFtpSources) {
					source.SourceType = TestWaybillSourceType.ForTesting;
					source.Update();
				}
			}

			handler = new WaybillFtpSourceHandler();

			ftpHost = "127.0.0.1";
			ftpPort = Generator.Random(Int16.MaxValue).First();
			ftpWaybillDirectory = "Waybills";
			ftpRejectDirectory = "Rejects";
			user = "test";
			password = "test";
			supplierDeliveryId = 1234u;
			supplier = CreateAndSetupSupplier(ftpHost, ftpPort, ftpWaybillDirectory, ftpRejectDirectory, user, password);

			client = TestClient.CreateNaked();
			client.Save();

			address = client.Addresses[0];

			CopyWaybillFiles();
		}

		[TearDown]
		public void Stop()
		{
			using (new SessionScope()) {
				var supplierFtpSources = TestWaybillSource.Queryable.Where(source =>
					source.SourceType == TestWaybillSourceType.ForTesting);
				foreach (var source in supplierFtpSources) {
					source.SourceType = TestWaybillSourceType.FtpSupplier;
					source.Update();
				}
			}
		}

		[Test]
		public void Process_waybills()
		{
			CreateFakeFile("70983_906384.txt");

			ProcessWithFtp();

			var path = Path.Combine(Settings.Default.FTPOptBoxPath, ftpWaybillDirectory);
			//после обработки файлов мы должны их удалить
			Assert.That(Directory.GetFiles(path), Is.Empty);

			var addressId = address.Id;
			// Проверяем наличие записей в document_logs
			var logs = DocumentReceiveLog.Queryable.Where(l => l.Supplier.Id == supplier.Id && l.Address.Id == addressId).ToArray();
			Assert.That(logs.Count(), Is.EqualTo(2));

			// Проверяем наличие записей в documentheaders
			var log = logs.First(l => Path.GetExtension(l.FileName).ToLower() == ".dbf");
			Assert.That(Document.Queryable.Count(d => d.Log.Id == log.Id), Is.EqualTo(1));

			// Проверяем наличие файлов в папках клиентов
			Assert.IsTrue(Directory.Exists(ClientDir));
			var files = Directory.GetFiles(ClientDir);
			Assert.That(files.Count(), Is.EqualTo(2));
		}

		[Test]
		public void Process_waybills_second_time()
		{
			ProcessFiles();
			var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
			var countLogs = logs.Count();

			ProcessFiles();
			var logs2 = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
			Assert.That(countLogs, Is.EqualTo(logs2.Count()));
		}

		[Test]
		public void Process_waybills_second_time_Convert_Dbf_format()
		{
			SetConvertFormat();

			ProcessFiles();
			var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
			var countLogs = logs.Count();

			ProcessFiles();
			var logs2 = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
			Assert.That(countLogs, Is.EqualTo(logs2.Count()));
		}

		[Test]
		public void Process_waybills_convert_Dbf_format()
		{
			SetConvertFormat();

			ProcessFiles();

			var addressId = address.Id;
			// Проверяем наличие записей в document_logs
			var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id && log.Address.Id == addressId);

			Assert.That(logs.Count(), Is.EqualTo(2));

			// Проверяем наличие записей в documentheaders для исходных документов.
			foreach (var documentLog in logs) {
				var count = documentLog.IsFake
					? Document.Queryable.Count(doc => doc.Log.Id == documentLog.Id && doc.Log.IsFake)
					: Document.Queryable.Count(doc => doc.Log.Id == documentLog.Id && doc.Log.IsFake);
				//у нас только одна запись в documentsheaders
				Assert.That(count, documentLog.IsFake ? Is.EqualTo(1) : Is.EqualTo(0));
			}

			// Проверяем наличие файлов в папках клиентов
			var clientDir = ClientDir;
			Assert.IsTrue(Directory.Exists(clientDir));

			var files = Directory.GetFiles(clientDir);
			Assert.That(files.Count(), Is.GreaterThan(0));

			//проверка на существование файла dbf в новом формате.
			var dbfs = Directory.GetFiles(clientDir, logs.FirstOrDefault(log => !log.IsFake).Id + "*.dbf");

			Assert.That(dbfs.Count(), Is.EqualTo(1));
			var data = Dbf.Load(dbfs[0], Encoding.GetEncoding(866));
			Assert.IsTrue(data.Columns.Contains("postid_af"));
			Assert.IsTrue(data.Columns.Contains("ttn"));
			Assert.IsTrue(data.Columns.Contains("przv_post"));
		}

		private void ProcessFiles()
		{
			session.Transaction.Commit();
			var source = session.Load<WaybillSource>(supplier.Id);
			var path = Path.Combine(Settings.Default.FTPOptBoxPath, ftpWaybillDirectory);
			foreach (var file in Directory.GetFiles(path)) {
				handler.ProcessFile(new WaybillType(), source,
					new DownloadedFile(new FileInfo(file)));
			}
		}

		private void ProcessWithFtp()
		{
			jobHandle = JobApi.StartChildProcess(@"..\..\..\..\lib\ftpdmin\ftpdmin.exe", String.Format(" -p {1} \"{0}\"",
				Path.GetFullPath(Settings.Default.FTPOptBoxPath),
				ftpPort));
			try {
				if (session.Transaction.IsActive)
					session.Transaction.Commit();
				handler.CreateDownHandlerPath();
				handler.ProcessData();
			}
			finally {
				Win32.CloseHandle(jobHandle);
			}
		}

		private string ClientDir
		{
			get { return Path.Combine(Settings.Default.DocumentPath, address.Id.ToString(), "Waybills"); }
		}

		private TestSupplier CreateAndSetupSupplier(string ftpHost, int ftpPort, string ftpWaybillDirectory, string ftpRejectDirectory, string user, string password)
		{
			var supplier = TestSupplier.CreateNaked();
			var source = supplier.WaybillSource;
			source.SourceType = TestWaybillSourceType.FtpSupplier;
			source.UserName = user;
			source.Password = password;
			var waybillUri = new UriBuilder("ftp", ftpHost, ftpPort, ftpWaybillDirectory);
			var rejectUri = new UriBuilder("ftp", ftpHost, ftpPort, ftpRejectDirectory);
			source.WaybillUrl = waybillUri.Uri.AbsoluteUri;
			source.RejectUrl = rejectUri.Uri.AbsoluteUri;
			supplier.Save();

			return supplier;
		}

		private void CopyWaybillFiles()
		{
			SetDeliveryCodes(supplierDeliveryId);
			var waybillsDir = Path.Combine(Settings.Default.FTPOptBoxPath, ftpWaybillDirectory);
			if (!Directory.Exists(waybillsDir))
				Directory.CreateDirectory(waybillsDir);

			var source = @"..\..\Data\Waybills\70983_906384.ZIP";
			var destination = Path.Combine(waybillsDir, String.Format("{0}_{1}", supplierDeliveryId, Path.GetFileName(source)));
			File.Copy(source, destination);
		}

		private void CreateFakeFile(string name)
		{
			var waybillsDir = Path.Combine(Settings.Default.FTPOptBoxPath, ftpWaybillDirectory);
			var destination = Path.Combine(waybillsDir, String.Format("{0}_{1}", supplierDeliveryId, name));
			File.WriteAllText(destination, "");
		}
	}
}