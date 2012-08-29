using System;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;
using Castle.ActiveRecord;
using Common.Tools;

namespace PriceProcessor.Test.Waybills.Handlers
{
	public class WaybillFtpSourceHandlerForTesting : WaybillFtpSourceHandler
	{
		public void Process()
		{
			CreateDirectoryPath();
			ProcessData();
		}
	}

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

		private WaybillFtpSourceHandlerForTesting handler;

		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();

			using (new SessionScope()) {
				var supplierFtpSources = TestWaybillSource.Queryable.Where(source =>
					source.SourceType == WaybillSourceType.FtpSupplier);
				foreach (var source in supplierFtpSources) {
					source.SourceType = WaybillSourceType.ForTesting;
					source.Update();
				}
			}

			handler = new WaybillFtpSourceHandlerForTesting();

			ftpHost = "ftp.narod.ru";
			ftpPort = 21;
			ftpWaybillDirectory = "Waybills";
			ftpRejectDirectory = "Rejects";
			user = "test";
			password = "test";
			supplierDeliveryId = 1234u;

			client = TestClient.Create();
			client.Settings.ParseWaybills = true;
			client.Save();

			address = client.Addresses[0];

			supplier = CreateAndSetupSupplier(ftpHost, ftpPort, ftpWaybillDirectory, ftpRejectDirectory, user, password);

			CopyWaybillFiles();
		}

		[TearDown]
		public void Stop()
		{
			using (new SessionScope()) {
				var supplierFtpSources = TestWaybillSource.Queryable.Where(source =>
					source.SourceType == WaybillSourceType.ForTesting);
				foreach (var source in supplierFtpSources) {
					source.SourceType = WaybillSourceType.FtpSupplier;
					source.Update();
				}
			}
		}

		[Test]
		public void Process_waybills()
		{
			CreateFakeFile("70983_906384.txt");

			handler.Process();

			using (new SessionScope()) {
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
		}

		[Test]
		public void Process_waybills_second_time()
		{
			handler.Process();
			var countLogs = 0;
			using (new SessionScope()) {
				var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				countLogs = logs.Count();
			}

			handler.Process();
			using (new SessionScope()) {
				var logs2 = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				Assert.That(countLogs, Is.EqualTo(logs2.Count()));
			}
		}

		[Test]
		public void Process_waybills_second_time_Convert_Dbf_format()
		{
			SetConvertFormat();

			handler.Process();

			int countLogs;
			using (new SessionScope()) {
				var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				countLogs = logs.Count();
			}

			handler.Process();
			using (new SessionScope()) {
				var logs2 = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				Assert.That(countLogs, Is.EqualTo(logs2.Count()));
			}
		}

		[Test]
		public void Process_waybills_convert_Dbf_format()
		{
			SetConvertFormat();

			handler.Process();

			using (new SessionScope()) {
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
				var dbfs = Directory.GetFiles(clientDir, "*.dbf");
				Assert.That(dbfs.Count(), Is.EqualTo(1));
				var data = Dbf.Load(dbfs[0], Encoding.GetEncoding(866));
				Assert.IsTrue(data.Columns.Contains("postid_af"));
				Assert.IsTrue(data.Columns.Contains("ttn"));
				Assert.IsTrue(data.Columns.Contains("przv_post"));
			}
		}

		private string ClientDir
		{
			get { return Path.Combine(Settings.Default.DocumentPath, address.Id.ToString(), "Waybills"); }
		}

		private TestSupplier CreateAndSetupSupplier(string ftpHost, int ftpPort, string ftpWaybillDirectory, string ftpRejectDirectory, string user, string password)
		{
			var supplier = TestSupplier.Create();
			using (var scope = new TransactionScope()) {
				var source = supplier.WaybillSource;
				source.SourceType = WaybillSourceType.FtpSupplier;
				source.UserName = user;
				source.Password = password;
				source.WaybillUrl = PathHelper.CombineFtpUrl(ftpHost, ftpPort.ToString(), ftpWaybillDirectory);
				source.RejectUrl = PathHelper.CombineFtpUrl(ftpHost, ftpPort.ToString(), ftpRejectDirectory);
				supplier.Save();

				scope.VoteCommit();
				return supplier;
			}
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