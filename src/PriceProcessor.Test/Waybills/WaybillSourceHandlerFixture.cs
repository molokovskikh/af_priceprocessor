using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using LumiSoft.Net.IMAP;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Properties;
using System.Threading;
using System.IO;
using LumiSoft.Net.IMAP.Client;
using Inforoom.Downloader.Documents;
using MySql.Data.MySqlClient;
using Test.Support;

namespace PriceProcessor.Test
{
	public class SummaryInfo
	{
		public TestClient Client { get; set; }

		public TestOldClient Supplier { get; set; }
	}

	public class WaybillSourceHandlerForTesting : WaybillSourceHandler
	{
		public WaybillSourceHandlerForTesting(string mailbox, string password)
			: base(mailbox, password)
		{
		}

		public void Process()
		{
			CreateDirectoryPath();
			CreateWorkConnection();
			ProcessData();
		}
	}

	[TestFixture]
	public class WaybillSourceHandlerFixture
	{
		private string _fileName = @"..\..\Data\Waybills\0000470553.dbf";

		private SummaryInfo _summary = new SummaryInfo();

		[SetUp]
		public void SetUp()
		{
			var fileName = _fileName;
			var client = TestClient.CreateSimple();
			var supplier = TestOldClient.CreateTestSupplier();

			With.Connection(connection => {
				var command = new MySqlCommand(@"
INSERT INTO `documents`.`waybill_sources` (FirmCode, EMailFrom, SourceId) VALUES (?FirmCode, ?EmailFrom, ?SourceId);
UPDATE usersettings.RetClientsSet SET ParseWaybills = 1 WHERE ClientCode = ?ClientCode
", connection);
				command.Parameters.AddWithValue("?FirmCode", supplier.Id);
				command.Parameters.AddWithValue("?EmailFrom", String.Format("{0}@test.test", client.Id));
				command.Parameters.AddWithValue("?ClientCode", client.Id);
				command.Parameters.AddWithValue("?SourceId", 1);
				command.ExecuteNonQuery();
			});

			TestHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			
			var message = TestHelper.BuildMessageWithAttach(
				String.Format("{0}@waybills.analit.net", client.Addresses[0].Id),
				String.Format("{0}@test.test", client.Id), fileName);

			TestHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, message.ToByteData());

			_summary.Client = client;
			_summary.Supplier = supplier;
		}

		private void Process_waybill(string filePath)
		{
			_fileName = filePath;;
			var handler = new WaybillSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.Process();
		}

		[Test, Description("Проверка вставки даты документа после разбора накладной")]
		public void Check_document_date()
		{
			Process_waybill(@"..\..\Data\Waybills\0000470553.dbf");

			With.Connection(connection => {
				var command = new MySqlCommand(@"
SELECT
	count(*) 
FROM
	`documents`.`DocumentHeaders` 
WHERE 
	FirmCode = ?SupplierId and 
	ClientCode = ?ClientId and 
	AddressId = ?AddressId and
	DocumentDate is not null", connection);
				command.Parameters.AddWithValue("?SupplierId", _summary.Supplier.Id);
				command.Parameters.AddWithValue("?ClientId", _summary.Client.Id);
				command.Parameters.AddWithValue("?AddressId", _summary.Client.Addresses[0].Id);
				var count = Convert.ToInt32(command.ExecuteScalar());
				Assert.That(count, Is.EqualTo(1));
			});

		}
	}
}
