using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.Downloader.DocumentReaders;
using Inforoom.Downloader.Documents;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using PriceProcessor.Test.TestHelpers;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using System.IO;
using MySql.Data.MySqlClient;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Handlers
{
	public class FakeSIAMoscow_2788_Reader1 : SIAMoscow_2788_Reader
	{
		public override List<ulong> ParseAddressIds(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			throw new Exception("Не получилось сформировать SupplierClientId(FirmClientCode) и SupplierDeliveryId(FirmClientCode2) из документа.");
		}
	}

	public class FakeSIAMoscow_2788_Reader2 : SIAMoscow_2788_Reader
	{
		public override List<ulong> ParseAddressIds(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			return null;
		}

		public override string FormatOutputFile(string InputFile, DataRow drSource)
		{
			throw new Exception("Количество позиций в документе не соответствует значению в заголовке документа");
		}
	}

	public class FakeSIAMoscow_2788_Reader3 : SIAMoscow_2788_Reader
	{
		public override List<ulong> ParseAddressIds(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			return new List<ulong> { 0 };
		}

		public override string FormatOutputFile(string InputFile, DataRow drSource)
		{
			return "test";
		}

		public override void ImportDocument(DocumentReceiveLog log, string filename)
		{
			throw new Exception("Дублирующийся документ");
		}
	}

	public class FakeSIAMoscow_2788_Reader4 : SIAMoscow_2788_Reader
	{
		public override List<ulong> ParseAddressIds(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			return new List<ulong> { 0 };
		}

		public override string FormatOutputFile(string InputFile, DataRow drSource)
		{
			return "test";
		}

		public override void ImportDocument(DocumentReceiveLog log, string filename)
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				log.Save();
				transaction.VoteCommit();
			}
		}
	}

	public class FakeWaybillLANSourceHandler : WaybillLanSourceHandler
	{
		private readonly DataRow drLanSource;
		private readonly BaseDocumentReader reader;

		public FakeWaybillLANSourceHandler()
		{
		}

		public FakeWaybillLANSourceHandler(string readerClassName, uint supplierId)
		{
			FillSourcesTable();
			drLanSource = dtSources.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToUInt32(r["FirmCode"]) == supplierId);
			reader = ReflectionHelper.GetDocumentReader<BaseDocumentReader>(readerClassName, Assembly.GetExecutingAssembly());
			_currentDocumentType = new WaybillType();
		}

		public bool MoveWaybill(string archFileName, string fileName)
		{
			return MoveWaybill(archFileName, fileName, drLanSource, reader);
		}
	}

	[TestFixture]
	public class WaybillLanSourceHandlerFixture : BaseWaybillHandlerFixture
	{
		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();
			supplier = TestSupplier.CreateNaked();

			client = TestClient.CreateNaked();
			address = client.Addresses[0];

			waybillDir = CreateSupplierDir(DocType.Waybill);
			rejectDir = CreateSupplierDir(DocType.Reject);
		}

		private void Process()
		{
			session.Transaction.Commit();
			var handler = new WaybillLanSourceHandler();
			handler.CreateDirectoryPath();
			handler.ProcessData();
		}

		public void PrepareLanSource(string readerClassName = "ProtekOmsk_3777_Reader")
		{
			client.Save();
			supplier.WaybillSource.SourceType = WaybillSourceType.FtpInforoom;
			supplier.WaybillSource.ReaderClassName = readerClassName;
			supplier.Save();
		}

		/// <summary>
		/// Если раскоментировать строки, будет попытка разобрать файл с заведомо неправильным кодом клиента
		/// </summary>
		[Test]
		public void Parse_waybills()
		{
			var filePath = @"..\..\Data\Waybills\890579.dbf";

			CopyFile(waybillDir, filePath);

			PrepareLanSource();
			MaitainAddressIntersection(address.Id);

			Process();

			CheckClientDirectory(1, DocType.Waybill);
			CheckDocumentLogEntry(1);
		}


		[Test]
		public void Delete_broken_file()
		{
			var filePath = @"..\..\Data\Waybills\890579.dbf";

			File.Copy(filePath, Path.Combine(waybillDir, "1.dbf"));
			CopyFile(waybillDir, filePath);

			PrepareLanSource();
			MaitainAddressIntersection(address.Id);

			Process();

			CheckClientDirectory(1, DocType.Waybill);
			CheckDocumentLogEntry(1);
			Assert.That(Directory.GetFiles(waybillDir), Is.Empty);
		}

		[Test(Description = "Проверяем корректное изменение имени файла при недопустимых символах в имени")]
		public void Parse_error_rejects()
		{
			var address = client.CreateAddress();
			client.Users[0].JoinAddress(address);
			address.Save();

			Assert.That(client.Addresses.Count, Is.GreaterThanOrEqualTo(2));

			var filePath = @"..\..\Data\Waybills\14460_Брнскфарм апт. пункт (1) (дп №20111297)5918043df.txt";

			CopyFile(rejectDir, filePath);

			PrepareLanSource("SupplierFtpReader");

			MaitainAddressIntersection(client.Addresses[0].Id);
			MaitainAddressIntersection(client.Addresses[1].Id, client.Addresses[0].Id.ToString());

			Process();

			CheckClientDirectory(1, DocType.Reject, client.Addresses[0]);
			CheckDocumentLogEntry(1, client.Addresses[0]);

			CheckClientDirectory(1, DocType.Reject, client.Addresses[1]);
			CheckDocumentLogEntry(1, client.Addresses[1]);

			var tmpFiles = Directory.GetFiles(Path.Combine(Settings.Default.TempPath, typeof(WaybillLanSourceHandler).Name), "*.*");
			Assert.That(tmpFiles.Count(), Is.EqualTo(0), "не удалили временные файлы {0}", tmpFiles.Implode());
		}

		private void CopyFile(string directory, string filePath)
		{
			File.Copy(filePath,
				Path.Combine(directory,
					String.Format("{0}_{1}", client.Addresses[0].Id, Path.GetFileName(filePath))));
		}

		[Test]
		public void Parse_waybills_Convert_Dbf_format()
		{
			var filePath = @"..\..\Data\Waybills\890579.dbf";
			CopyFile(waybillDir, filePath);

			PrepareLanSource();
			MaitainAddressIntersection(client.Addresses[0].Id);

			SetConvertDocumentSettings();

			Process();

			CheckClientDirectory(2, DocType.Waybill);

			using (new SessionScope()) {
				var logs = TestDocumentLog.Queryable.Where(l =>
					l.Client.Id == client.Id &&
						l.Supplier.Id == supplier.Id &&
						l.Address == client.Addresses[0]);

				Assert.That(logs.Count(), Is.EqualTo(2));
				Assert.That(logs.Count(l => l.IsFake), Is.EqualTo(1));
				Assert.That(logs.Count(l => !l.IsFake), Is.EqualTo(1));

				var log = logs.SingleOrDefault(l => !l.IsFake);
				var file_dbf = GetFileForAddress(DocType.Waybill).Single(f => f.IndexOf(log.Id.ToString()) > -1);

				var data = Dbf.Load(file_dbf, Encoding.GetEncoding(866));
				Assert.IsTrue(data.Columns.Contains("postid_af"));
				Assert.IsTrue(data.Columns.Contains("ttn"));
				Assert.IsTrue(data.Columns.Contains("przv_post"));
			}
		}

		[Test]
		public void TestSIAMoscow2788()
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var address = client.CreateAddress();
				client.Users[0].JoinAddress(address);
				address.Save();
				transaction.VoteCommit();
			}

			//Эти коды доставки вбиты в файлы, которые используются в качестве примера для теста (переменная files)
			//Прописываем эти коды доставки у тестового клиента, чтобы относительно него разбирались данные тестовые файлы
			var supplierClientId = "826874436";
			var supplierDeliveryId_0 = "826874888";
			var supplierDeliveryId_1 = "826874892";

			var files = new[] { "826874436_20091202030542372.zip", "826874436_20091202090615283.zip", "826874436_20091202102538565.zip" };

			PrepareLanSource("SIAMoscow_2788_Reader");

			//Очищаем supplierDeliveryId и supplierClientId для всех адресов, у которых установлены необходимые значения
			var command = new MySqlCommand(@"
update
Customers.Intersection i,
Customers.Addresses a,
Customers.AddressIntersection ai
set
ai.SupplierDeliveryId = null,
i.SupplierClientId = null
where
a.ClientId = i.ClientId
and ai.AddressId = a.Id
and ai.IntersectionId = i.Id
and ai.SupplierDeliveryId = ?supplierDeliveryId
and  i.SupplierClientId = ?supplierClientId
", (MySqlConnection)session.Connection);

			command.Parameters.AddWithValue("?supplierClientId", supplierClientId);
			command.Parameters.AddWithValue("?supplierDeliveryId", supplierDeliveryId_0);
			command.ExecuteNonQuery();

			command.Parameters["?supplierDeliveryId"].Value = supplierDeliveryId_1;
			command.ExecuteNonQuery();

			MaitainAddressIntersection(client.Addresses[0].Id, supplierDeliveryId_0, supplierClientId);
			MaitainAddressIntersection(client.Addresses[1].Id, supplierDeliveryId_1, supplierClientId);

			CopyFilesFromDataDirectory(files);

			Process();

			var path = Path.GetFullPath(Settings.Default.DocumentPath);
			var clientDirectories = Directory.GetDirectories(Path.GetFullPath(Settings.Default.DocumentPath));
			Assert.IsTrue(clientDirectories.Length > 1, "Не создано ни одной директории для клиента-получателя накладной " + path + " " + clientDirectories.Length);
		}
	}
}