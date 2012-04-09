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
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.Mime;
using PriceProcessor.Test.TestHelpers;
using PriceProcessor.Test.Waybills;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using System.IO;
using MySql.Data.MySqlClient;
using Test.Support;
using Test.Support.Suppliers;
using WaybillSourceType = Test.Support.WaybillSourceType;
using FileHelper = Common.Tools.FileHelper;

namespace PriceProcessor.Test
{
	public class WaybillLANSourceHandlerForTesting : WaybillLANSourceHandler
	{
		public void Process()
		{
			CreateDirectoryPath();
			ProcessData();
		}
	}

	public class FakeSIAMoscow_2788_Reader1 : SIAMoscow_2788_Reader
	{		
		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			throw new Exception("Не получилось сформировать SupplierClientId(FirmClientCode) и SupplierDeliveryId(FirmClientCode2) из документа.");
		}
	}

	public class FakeSIAMoscow_2788_Reader2 : SIAMoscow_2788_Reader
	{
		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
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
		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			return new List<ulong>{ 0 };
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
		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			return new List<ulong>{ 0 };
		}
		public override string FormatOutputFile(string InputFile, DataRow drSource)
		{
			return "test";
		}
		public override void ImportDocument(DocumentReceiveLog log, string filename)
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback))
			{
				log.Save();
				transaction.VoteCommit();
			}
		}
	}

	public class FakeWaybillLANSourceHandler : WaybillLANSourceHandler
	{
		private readonly DataRow drLanSource;
		private readonly BaseDocumentReader reader;

		public  FakeWaybillLANSourceHandler()
		{}

		public FakeWaybillLANSourceHandler(string readerClassName)
		{
			FillSourcesTable();
			drLanSource = dtSources.Rows.Cast<DataRow>().FirstOrDefault(r => r["ReaderClassName"].ToString() == "SIAMoscow_2788_Reader");
			Type result;
			var types = Assembly.GetExecutingAssembly()
								.GetModules()[0]
								.FindTypes(Module.FilterTypeNameIgnoreCase, readerClassName);
			result = types[0];
			reader = (BaseDocumentReader)Activator.CreateInstance(result);
			_currentDocumentType = new WaybillType();
		}

		public bool MoveWaybill(string archFileName, string fileName)
		{
			return MoveWaybill(archFileName, fileName, drLanSource, reader);
		}
	}

	[TestFixture]
	public class WaybillLANSourceHandlerFixture
	{
		private string waybillDir;
		private string rejectDir;

		private TestClient client;
		private TestAddress address;
		private TestSupplier supplier;

		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();
			client = TestClient.Create();
			address = client.Addresses[0];
			supplier = TestSupplier.Create();

			waybillDir = CreateSupplierDir(DocType.Waybill);
			rejectDir = CreateSupplierDir(DocType.Reject);
		}

		private void Process_waybills()
		{
			var handler = new WaybillLANSourceHandlerForTesting();
			handler.Process();
		}

		public void PrepareLanSource(string readerClassName = "ProtekOmsk_3777_Reader")
		{
			client.Settings.ParseWaybills = true;
			client.Save();
			supplier.WaybillSource.SourceType = WaybillSourceType.FtpInforoom;
			supplier.WaybillSource.ReaderClassName = readerClassName;
			supplier.Save();
		}

		private void MaitainAddressIntersection(uint addressId, string supplierDeliveryId = null, string supplierClientId = null)
		{
			if (String.IsNullOrEmpty(supplierDeliveryId))
				supplierDeliveryId = addressId.ToString();

			With.Connection(connection =>
			{
				var command = new MySqlCommand(@"
insert into Customers.AddressIntersection(AddressId, IntersectionId, SupplierDeliveryId)
select a.Id, i.Id, ?supplierDeliveryId
from Customers.Intersection i
	join Customers.Addresses a on a.ClientId = i.ClientId
	left join Customers.AddressIntersection ai on ai.AddressId = a.Id and ai.IntersectionId = i.Id
where 
	a.Id = ?AddressId
and ai.Id is null", connection);

				command.Parameters.AddWithValue("?AddressId", addressId);
				command.Parameters.AddWithValue("?supplierDeliveryId", supplierDeliveryId);
				var insertCount = command.ExecuteNonQuery();
				if (insertCount == 0) {
					command.CommandText = @"
update
  Customers.Intersection i,
  Customers.Addresses a,
  Customers.AddressIntersection ai
set
  ai.SupplierDeliveryId = ?supplierDeliveryId,
  i.SupplierClientId = ?supplierClientId
where
	a.ClientId = i.ClientId
and ai.AddressId = a.Id 
and ai.IntersectionId = i.Id
and a.Id = ?AddressId
";
					command.Parameters.AddWithValue("?supplierClientId", supplierClientId);
					command.ExecuteNonQuery();
				}
			});
		}

		public string CreateSupplierDir(DocType type)
		{
			var directory = Path.Combine(Settings.Default.FTPOptBoxPath, supplier.Id.ToString());
			directory = Path.Combine(directory, type + "s");

			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
			Directory.CreateDirectory(directory);
			return directory;
		}

		private void CheckClientDirectory(int waitingFilesCount, DocType documentsType, TestAddress address = null)
		{
			var savedFiles = GetFileForAddress(documentsType, address);
			Assert.That(savedFiles.Count(), Is.EqualTo(waitingFilesCount));
		}

		private string[] GetFileForAddress(DocType documentsType, TestAddress address = null)
		{
			if (address == null)
				address = client.Addresses[0];
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, address.Id.ToString().PadLeft(3, '0'));
			return Directory.GetFiles(Path.Combine(clientDirectory, documentsType + "s"), "*.*", SearchOption.AllDirectories);
		}

		private void CheckDocumentLogEntry(int waitingCountEntries, TestAddress address = null)
		{
			if (address == null)
				address = client.Addresses[0];

			using (new SessionScope())
			{
				var logs = TestDocumentLog.Queryable.Where(log =>
					log.Client.Id == client.Id &&
					log.Supplier.Id == supplier.Id &&
					log.AddressId == address.Id);
				Assert.That(logs.Count(), Is.EqualTo(waitingCountEntries));
			}
		}

		[Test]
		public void Parse_waybills()
		{
			var filePath = @"..\..\Data\Waybills\890579.dbf";

			File.Copy(filePath, Path.Combine(waybillDir, String.Format("{0}_{1}", client.Addresses[0].Id, Path.GetFileName(filePath))));
			PrepareLanSource();
			MaitainAddressIntersection(client.Addresses[0].Id);

			Process_waybills();

			CheckClientDirectory(1, DocType.Waybill);
			CheckDocumentLogEntry(1);
		}

		[Test(Description = "Проверяем корректное изменение имени файла при недопустимых символах в имени")]
		public void Parse_error_rejects()
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var address = client.CreateAddress();
				client.Users[0].JoinAddress(address);
				address.Save();
				transaction.VoteCommit();
			}

			Assert.That(client.Addresses.Count, Is.GreaterThanOrEqualTo(2));

			var directory = CreateSupplierDir(DocType.Reject);
			var filePath = @"..\..\Data\Waybills\14460_Брнскфарм апт. пункт (1) (дп №20111297)5918043df.txt";

			File.Copy(filePath, Path.Combine(directory, String.Format("{0}_{1}", client.Addresses[0].Id, Path.GetFileName(filePath))));
			
			PrepareLanSource("SupplierFtpReader");

			MaitainAddressIntersection(client.Addresses[0].Id);
			MaitainAddressIntersection(client.Addresses[1].Id, client.Addresses[0].Id.ToString());

			Process_waybills();

			CheckClientDirectory(1, DocType.Reject, client.Addresses[0]);
			CheckDocumentLogEntry(1, client.Addresses[0]);

			CheckClientDirectory(1, DocType.Reject, client.Addresses[1]);
			CheckDocumentLogEntry(1, client.Addresses[1]);

			var tmpFiles = Directory.GetFiles(Path.Combine(Settings.Default.TempPath, typeof(WaybillLANSourceHandlerForTesting).Name), "*.*");
			Assert.That(tmpFiles.Count(), Is.EqualTo(0), "не удалили временные файлы {0}", tmpFiles.Implode());
		}

		[Test]
		public void Parse_waybills_Convert_Dbf_format()
		{
			var filePath = @"..\..\Data\Waybills\890579.dbf";

			File.Copy(filePath, Path.Combine(waybillDir, String.Format("{0}_{1}", client.Addresses[0].Id, Path.GetFileName(filePath))));
			PrepareLanSource();
			MaitainAddressIntersection(client.Addresses[0].Id);

			SetConvertDocumentSettings();

			Process_waybills();

			CheckClientDirectory(1, DocType.Waybill);

			using (new SessionScope())
			{
				var logs = TestDocumentLog.Queryable.Where(l =>
					l.Client.Id == client.Id &&
					l.Supplier.Id == supplier.Id &&
					l.AddressId == client.Addresses[0].Id);
				
				Assert.That(logs.Count(), Is.EqualTo(2));
				Assert.That(logs.Count(l => l.IsFake), Is.EqualTo(1));
				Assert.That(logs.Count(l => !l.IsFake), Is.EqualTo(1));
				
				var log = logs.SingleOrDefault(l => !l.IsFake);
				var file_dbf = GetFileForAddress(DocType.Waybill).Single(f => f.IndexOf(Path.GetFileNameWithoutExtension(log.FileName)) > -1);
				
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
			With.Connection(connection =>
			{
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
"
					, 
					connection);

				command.Parameters.AddWithValue("?supplierClientId", supplierClientId);
				command.Parameters.AddWithValue("?supplierDeliveryId", supplierDeliveryId_0);
				command.ExecuteNonQuery();

				command.Parameters["?supplierDeliveryId"].Value = supplierDeliveryId_1;
				command.ExecuteNonQuery();
			}
			);

			MaitainAddressIntersection(client.Addresses[0].Id, supplierDeliveryId_0, supplierClientId);
			MaitainAddressIntersection(client.Addresses[1].Id, supplierDeliveryId_1, supplierClientId);

			CopyFilesFromDataDirectory(files);

			Process_waybills();
		
			var path = Path.GetFullPath(Settings.Default.DocumentPath);
			var clientDirectories = Directory.GetDirectories(Path.GetFullPath(Settings.Default.DocumentPath));
			Assert.IsTrue(clientDirectories.Length > 1, "Не создано ни одной директории для клиента-получателя накладной " + path + " " + clientDirectories.Length);
		}

		[Test]
		public void Process_message_if_from_contains_more_than_one_address()
		{
			supplier.WaybillSource.EMailFrom = String.Format("edata{0}@msk.katren.ru", supplier.Id);
			supplier.WaybillSource.SourceType = WaybillSourceType.Email;
			supplier.Save();

			FileHelper.DeleteDir(Settings.Default.DocumentPath);

			ImapHelper.ClearImapFolder();
			var mime = PatchTo(@"..\..\Data\Unparse.eml",
				String.Format("{0}@waybills.analit.net", address.Id),
				String.Format("edata{0}@msk.katren.ru,vbskript@katren.ru", supplier.Id)
			);
			ImapHelper.StoreMessage(mime.ToByteData());

			Process();

			var files = GetFileForAddress(DocType.Waybill);
			Assert.That(files.Length, Is.EqualTo(1), "не обработали документ");
		}

		[Test]
		public void Parse_waybill_if_parsing_enabled()
		{
			var beign = DateTime.Now;
			//Удаляем миллисекунды из даты, т.к. они не сохраняются в базе данных
			beign = beign.AddMilliseconds(-beign.Millisecond);

			var email = String.Format("edata{0}@msk.katren.ru", supplier.Id);
			supplier.WaybillSource.EMailFrom = email;
			supplier.WaybillSource.SourceType = WaybillSourceType.Email;
			supplier.Save();
			client.Settings.ParseWaybills = true;
			client.Save();

			ImapHelper.ClearImapFolder();
			ImapHelper.StoreMessageWithAttachToImapFolder(
				String.Format("{0}@waybills.analit.net", client.Addresses[0].Id),
				email,
				@"..\..\Data\Waybills\8916.dbf");

			Process();

			var files = GetFileForAddress(DocType.Waybill);
			Assert.That(files.Length, Is.EqualTo(1));

			using (new SessionScope())
			{
				var logs = TestDocumentLog.Queryable.Where(d => d.Client.Id == client.Id).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				var log = logs.Single();
				Assert.That(log.LogTime, Is.GreaterThanOrEqualTo(beign));
				Assert.That(log.DocumentSize, Is.GreaterThan(0));

				var documents = Document.Queryable.Where(d => d.Log.Id == log.Id).ToList();
				Assert.That(documents.Count, Is.EqualTo(1));
				Assert.That(documents.Single().Lines.Count, Is.EqualTo(7));
			}
		}

		private void SetConvertDocumentSettings()
		{
			var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == client.Id).SingleOrDefault();
			//запоминаем начальное состояние настройки
			var isConvertFormat = settings.IsConvertFormat;
			//и если оно не включено, то включим принудительно для теста
			if (!isConvertFormat)
			{
				using (new TransactionScope())
				{
					settings.IsConvertFormat = true;
					settings.AssortimentPriceId = supplier.Prices.First().Id;
					settings.SaveAndFlush();
				}
			}
		}

		private Mime PatchTo(string file, string to, string from)
		{
			var mime = Mime.Parse(file);
			var main = mime.MainEntity;
			main.To.Clear();
			main.To.Parse(to);
			main.From.Clear();
			main.From.Parse(from);
			return mime;
		}

		private void Process()
		{
			var filter = new EventFilter<WaybillSourceHandler>();
			var handler = new WaybillSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.Process();
			Assert.That(filter.Events.Count, Is.EqualTo(0), "во премя обработки произошли ошибки, {0}", filter.Events.Implode(m => m.ExceptionObject.ToString()));
		}

		private void CopyFilesFromDataDirectory(string[] fileNames)
		{
			var dataDirectory = Path.GetFullPath(Settings.Default.TestDataDirectory);
			// Копируем файлы в папку поставщика
			foreach (var fileName in fileNames)
				File.Copy(Path.Combine(dataDirectory, fileName), Path.Combine(waybillDir, fileName));
		}
	}
}
