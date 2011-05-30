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
using Inforoom.PriceProcessor;
using LumiSoft.Net.FTP.Client;
using LumiSoft.Net.FTP.Server;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Waybills;
using Test.Support;
using Test.Support.Suppliers;
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

		private TestSupplier CreateAndSetupSupplier(string ftpHost, int ftpPort, string ftpWaybillDirectory, string ftpRejectDirectory, string user, string password)
		{
			TestSupplier supplier = null;
			using (var scope = new TransactionScope())
			{
				//supplier = TestOldClient.CreateTestSupplier();
				supplier = TestSupplier.Create();
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

		private IList<uint> CopyWaybillFiles(uint oldClientDeliveryCode, uint newClientDeliveryCode, TestSupplier supplier, string ftpWaybillDirectory)
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
			
			var handler = new WaybillFtpSourceHandlerForTesting();
			handler.Process();

			using (new SessionScope())
			{
				foreach (var clientCode in clientCodes)
				{
					// Проверяем наличие записей в document_logs
					var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id && log.ClientCode == clientCode);
					Assert.That(logs.Count(), Is.EqualTo(1));

					// Проверяем наличие записей в documentheaders
					foreach (var documentLog in logs)
						Assert.That(Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id).Count(), Is.EqualTo(1));

					// Проверяем наличие файлов в папках клиентов
					//var clientDir = Path.Combine(Settings.Default.FTPOptBoxPath, clientCode.ToString());
					var clientDir = Path.Combine(Settings.Default.DocumentPath, clientCode.ToString());
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

			var handler = new WaybillFtpSourceHandlerForTesting();
			handler.Process();
			var countLogs = 0;
			using (new SessionScope())
			{
				var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				countLogs = logs.Count();
			}

			handler.Process();
			using (new SessionScope())
			{
				var logs2 = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				Assert.That(countLogs, Is.EqualTo(logs2.Count()));
			}
		}

		[Test]
		public void Process_waybills_second_time_Convert_Dbf_format()
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

			var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == 361).SingleOrDefault();
			using (new TransactionScope())
			{
				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = (int)Core.Queryable.First().Price.Id;
				settings.SaveAndFlush();
			}

			var handler = new WaybillFtpSourceHandlerForTesting();
			handler.Process();
			var countLogs = 0;
			using (new SessionScope())
			{
				var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				countLogs = logs.Count();
			}

			handler.Process();
			using (new SessionScope())
			{
				var logs2 = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id);
				Assert.That(countLogs, Is.EqualTo(logs2.Count()));
			}

			//возвращаем назад для этого клиента значение IsConvertFormat, чтобы работали старые тесты.
			// так как в старых тестах есть проверка на количество записей для поставщика и клиента в document_logs
			//и когда создаем файл в новом формате dbf,то таких записей становится две.
			using (new TransactionScope())
			{
				settings.IsConvertFormat = false;
				settings.AssortimentPriceId = null;
				settings.SaveAndFlush();
			}
		}


		[Test]
		public void Process_waybills_convert_Dbf_format()
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

			var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == 361).SingleOrDefault();
			using (new TransactionScope())
			{
				settings.IsConvertFormat = true;
				settings.AssortimentPriceId = (int)Core.Queryable.First().Price.Id;
				settings.SaveAndFlush();
			}
			
			var handler = new WaybillFtpSourceHandlerForTesting();
			handler.Process();

			using (new SessionScope())
			{
				foreach (var clientCode in clientCodes)
				{
					// Проверяем наличие записей в document_logs
					var logs = DocumentReceiveLog.Queryable.Where(log => log.Supplier.Id == supplier.Id && log.ClientCode == clientCode);

					Assert.That(logs.Count(), clientCode == 361 ? Is.EqualTo(2) : Is.EqualTo(1));

					// Проверяем наличие записей в documentheaders для исходных документов.
					foreach (var documentLog in logs)
					{
						var count = documentLog.IsFake
						            	? Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id && doc.Log.IsFake).Count()
						            	: Document.Queryable.Where(doc => doc.Log.Id == documentLog.Id && doc.Log.IsFake).Count();
						//у нас только одна запись в documentsheaders
						Assert.That(count, documentLog.IsFake ? Is.EqualTo(1) : Is.EqualTo(0));
					}
					
					// Проверяем наличие файлов в папках клиентов
					//var clientDir = Path.Combine(Settings.Default.FTPOptBoxPath, clientCode.ToString());
					var clientDir = Path.Combine(Settings.Default.DocumentPath, clientCode.ToString());
					Assert.IsTrue(Directory.Exists(clientDir));

					var files = Directory.GetFiles(Path.Combine(clientDir, "Waybills"));
					Assert.That(files.Count(), Is.GreaterThan(0));

					//проверка на существование файла dbf в новом формате.
					if (clientCode == 361)
					{
						var files_dbf = Directory.GetFiles(Path.Combine(clientDir, "Waybills"), "*.dbf");
						Assert.That(files_dbf.Count(), Is.EqualTo(1));
						var data = Dbf.Load(files_dbf[0], Encoding.GetEncoding(866));
						Assert.IsTrue(data.Columns.Contains("postid_af"));
						Assert.IsTrue(data.Columns.Contains("ttn"));
						Assert.IsTrue(data.Columns.Contains("przv_post"));
					}
				}
			}

			//возвращаем назад для этого клиента значение IsConvertFormat, чтобы работали старые тесты.
			// так как в старых тестах есть проверка на количество записей для поставщика и клиента в document_logs
			//и когда создаем файл в новом формате dbf,то таких записей становится две.
			using (new TransactionScope())
			{
				settings.IsConvertFormat = false;
				settings.AssortimentPriceId = null;
				settings.SaveAndFlush();
			}

		}
	}
}
