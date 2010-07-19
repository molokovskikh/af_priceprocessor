using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Common.MySql;
using Inforoom.Common;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Properties;
using LumiSoft.Net.FTP.Client;
using LumiSoft.Net.FTP.Server;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Waybills;
using Test.Support;
using WaybillSourceType = Test.Support.WaybillSourceType;
using Castle.ActiveRecord;
using Common.Tools;

namespace PriceProcessor.Test.Waybills
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
	public class WaybillFtpSourceHandlerFixture
	{
		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();

			using (new SessionScope())
			{
				var supplierFtpSources = TestWaybillSource.Queryable.Where(source =>
					source.SourceType == WaybillSourceType.FtpSupplier);
				foreach (var source in supplierFtpSources)
				{
					source.SourceType = WaybillSourceType.ForTesting;
					source.Update();
				}
			}
		}

		[TearDown]
		public void Stop()
		{
			using (new SessionScope())
			{
				var supplierFtpSources = TestWaybillSource.Queryable.Where(source =>
					source.SourceType == WaybillSourceType.ForTesting);
				foreach (var source in supplierFtpSources)
				{
					source.SourceType = WaybillSourceType.FtpSupplier;
					source.Update();
				}
			}
		}

		private IList<uint> SetDeliveryCodes(uint? supplierDeliveryIdForOldClient, uint? supplierDeliveryIdForNewClient, uint supplierId)
		{
			var clientCodes = new List<uint>();

			With.Connection(connection =>
			{
				var command = new MySqlCommand(@"
SELECT Id FROM usersettings.Intersection
WHERE PriceCode = (
	SELECT PriceCode 
	FROM usersettings.pricesdata 
	WHERE FirmCode = ?SupplierId
)
limit 1;
", connection);
				command.Parameters.AddWithValue("?SupplierId", supplierId);
				var intersectionId = Convert.ToUInt32(command.ExecuteScalar());

				command.CommandText = (@"
SELECT Id FROM future.addressintersection
WHERE IntersectionId = (
	SELECT Id 
	FROM future.intersection 
	WHERE PriceId = (
		SELECT PriceCode 
		FROM usersettings.pricesdata 
		WHERE FirmCode = ?SupplierId
	) limit 1
)
limit 1;");
				var addressIntersectionId = Convert.ToUInt32(command.ExecuteScalar());

				command.CommandText = @"
UPDATE usersettings.Intersection
SET FirmClientCode2 = ?oldClientSupplierDeliveryId
WHERE Id = ?IntersectionId;

UPDATE future.addressintersection
SET SupplierDeliveryId = ?newClientSupplierDeliveryId
WHERE Id = ?AddressIntersectionId;";
				command.Parameters.AddWithValue("?IntersectionId", intersectionId);
				command.Parameters.AddWithValue("?AddressIntersectionId", addressIntersectionId);
				command.Parameters.AddWithValue("?oldClientSupplierDeliveryId", supplierDeliveryIdForOldClient);
				command.Parameters.AddWithValue("?newClientSupplierDeliveryId", supplierDeliveryIdForNewClient);
				command.ExecuteNonQuery();

				command.CommandText = @"
SELECT ClientCode as ClientId
FROM usersettings.Intersection
WHERE Id = ?IntersectionId
UNION
SELECT ClientId
FROM future.Intersection
WHERE Id = (
	SELECT IntersectionId
	FROM future.addressintersection
	WHERE Id = ?AddressIntersectionId
)";
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
						clientCodes.Add(reader.GetUInt32("ClientId"));
				}
			});

			return clientCodes;
		}

		private TestOldClient CreateAndSetupSupplier(string ftpHost, int ftpPort, string ftpWaybillDirectory, string ftpRejectDirectory, string user, string password)
		{
			TestOldClient supplier = null;
			using (var scope = new TransactionScope())
			{
				supplier = TestOldClient.CreateTestSupplier();
				var source = new TestWaybillSource()
				{
					Id = supplier.Id,
					SourceType = WaybillSourceType.FtpSupplier,
					UserName = user,
					Password = password,
					WaybillUrl = PathHelper.CombineFtpUrl(ftpHost, ftpPort.ToString(), ftpWaybillDirectory),
					RejectUrl = PathHelper.CombineFtpUrl(ftpHost, ftpPort.ToString(), ftpRejectDirectory),
				};
				source.Create();
				scope.VoteCommit();
			}
			With.Connection(connection =>
			{
				var command = new MySqlCommand(@"
UPDATE documents.waybill_sources SET FirmCode = ?SupplierId WHERE FirmCode = 0;
", connection);
				command.Parameters.AddWithValue("?SupplierId", supplier.Id);
				command.ExecuteNonQuery();
			});
			return supplier;
		}

		private IList<uint> CopyWaybillFiles(uint oldClientDeliveryCode, uint newClientDeliveryCode, TestOldClient supplier, string ftpWaybillDirectory)
		{
			var clientCodes = SetDeliveryCodes(oldClientDeliveryCode, newClientDeliveryCode, supplier.Id);
			var waybillsDir = Path.Combine(Settings.Default.FTPOptBoxPath, ftpWaybillDirectory);
			if (!Directory.Exists(waybillsDir))
				Directory.CreateDirectory(waybillsDir);

			var waybillForOldClient = @"..\..\Data\Waybills\70983_906301.ZIP";
			var waybillForNewClient = @"..\..\Data\Waybills\70983_906384.ZIP";
			File.Copy(waybillForNewClient, Path.Combine(waybillsDir, String.Format("{0}_{1}", newClientDeliveryCode, Path.GetFileName(waybillForNewClient))));
			File.Copy(waybillForOldClient, Path.Combine(waybillsDir, String.Format("{0}_{1}", oldClientDeliveryCode, Path.GetFileName(waybillForOldClient))));

			return clientCodes;
		}

		[Test]
		public void Process_waybills()
		{
			var ftpHost = "ftp.narod.ru";
			var ftpPort = 21;
			var ftpWaybillDirectory = "Waybills";
			string ftpRejectDirectory = "Rejects";
			var user = "test";
			var password = "test";
			var newClientDeliveryCode = 1234u;
			var oldClientDeliveryCode = 12345u;

			var supplier = CreateAndSetupSupplier(ftpHost, ftpPort, ftpWaybillDirectory, ftpRejectDirectory, user, password);
			var clientCodes = CopyWaybillFiles(oldClientDeliveryCode, newClientDeliveryCode, supplier, ftpWaybillDirectory);
			ArchiveHelper.SevenZipExePath = @".\7zip\7z.exe";

			using (new SessionScope())
			{
				var handler = new WaybillFtpSourceHandlerForTesting();
				handler.Process();

				foreach (var clientCode in clientCodes)
				{
					// Проверяем наличие записей в document_logs
					var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id && log.ClientCode == clientCode);
					Assert.That(logs.Count(), Is.EqualTo(1));

					// Проверяем наличие записей в documentheaders
					foreach (var documentLog in logs)
						Assert.That(Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id).Count(), Is.EqualTo(1));

					// Проверяем наличие файлов в папках клиентов
					var clientDir = Path.Combine(Settings.Default.FTPOptBoxPath, clientCode.ToString());
					Assert.IsTrue(Directory.Exists(clientDir));
					var files = Directory.GetFiles(Path.Combine(clientDir, "Waybills"));
					Assert.That(files.Count(), Is.GreaterThan(0));
				}
			}
		}

		[Test]
		public void Process_waybills_second_time()
		{
			var ftpHost = "ftp.narod.ru";
			var ftpPort = 21;
			var ftpWaybillDirectory = "Waybills";
			string ftpRejectDirectory = "Rejects";
			var user = "test";
			var password = "test";
			var newClientDeliveryCode = 1234u;
			var oldClientDeliveryCode = 12345u;

			var supplier = CreateAndSetupSupplier(ftpHost, ftpPort, ftpWaybillDirectory, ftpRejectDirectory, user, password);
			CopyWaybillFiles(oldClientDeliveryCode, newClientDeliveryCode, supplier, ftpWaybillDirectory);
			ArchiveHelper.SevenZipExePath = @".\7zip\7z.exe";

			using (new SessionScope())
			{
				var handler = new WaybillFtpSourceHandlerForTesting();
				handler.Process();

				var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				var countLogs = logs.Count();

				handler.Process();
				var logs2 = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				Assert.That(countLogs, Is.EqualTo(logs2.Count()));
			}
		}
	}
}
