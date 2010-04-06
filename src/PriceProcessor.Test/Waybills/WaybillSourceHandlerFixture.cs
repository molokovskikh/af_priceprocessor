﻿using System;
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
using Inforoom.PriceProcessor.Waybills;
using Castle.ActiveRecord;
using Inforoom.Common;

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
		private IList<string> _fileNames = new List<string> {@"..\..\Data\Waybills\0000470553.dbf"};

		private SummaryInfo _summary = new SummaryInfo();

		[SetUp]
		public void DeleteDirectories()
		{
			TestHelper.RecreateDirectories();
		}

		public void SetUp(IList<string> fileNames)
		{
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
			
			var message = TestHelper.BuildMessageWithAttachments(
				String.Format("{0}@waybills.analit.net", client.Addresses[0].Id),
				String.Format("{0}@test.test", client.Id), fileNames.ToArray());

			TestHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, message.ToByteData());

			_summary.Client = client;
			_summary.Supplier = supplier;
		}

		private void Process_waybills()
		{
			var handler = new WaybillSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.Process();
		}

		[Test, Description("Проверка вставки даты документа после разбора накладной")]
		public void Check_document_date()
		{
			SetUp(new List<string> {@"..\..\Data\Waybills\0000470553.dbf"});
			Process_waybills();
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(doc => doc.FirmCode == _summary.Supplier.Id &&
				                                                doc.ClientCode == _summary.Client.Id &&
				                                                doc.AddressId == _summary.Client.Addresses[0].Id &&
				                                                doc.DocumentDate != null);
				Assert.That(documents.Count(), Is.EqualTo(1));
			}
		}

		[Test, Description("Проверка, что накладная скопирована в папку клиенту")]
		public void Check_copy_waybill_to_client_dir()
		{
			var fileNames = new List<string> { @"..\..\Data\Waybills\0000470553.dbf" };
			SetUp(fileNames);
			Process_waybills();

			var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*(0000470553).dbf",
                SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(1));

			var tempFilePath = Path.Combine(Settings.Default.TempPath, "DownWAYBILL");
			tempFilePath = Path.Combine(tempFilePath, "0000470553.dbf");
			Assert.IsFalse(File.Exists(tempFilePath));
		}

		[Test]
		public void Parse_multifile_document()
		{
            var files = new List<string> {
                @"..\..\Data\Waybills\multifile\b271433.dbf",
                @"..\..\Data\Waybills\multifile\h271433.dbf"
			};
            SetUp(files);
            Process_waybills();

            var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
            var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
            Assert.That(savedFiles.Count(), Is.EqualTo(2));

			var bodyFile = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*(b271433).dbf",
                SearchOption.AllDirectories);
			Assert.That(bodyFile.Count(), Is.EqualTo(1));

			var headerFile = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*(h271433).dbf",
                SearchOption.AllDirectories);
            Assert.That(headerFile.Count(), Is.EqualTo(1));
            using (new SessionScope())
            {
                var documents = Document.Queryable.Where(doc => doc.FirmCode == _summary.Supplier.Id &&
                    doc.ClientCode == _summary.Client.Id && doc.AddressId == _summary.Client.Addresses[0].Id);
                Assert.That(documents.Count(), Is.EqualTo(1));
			}

			var tmpFiles = Directory.GetFiles(Path.Combine(Settings.Default.TempPath, "DownWAYBILL"), "*.*");
			Assert.That(tmpFiles.Count(), Is.EqualTo(0));
		}

		[Test]
		public void Parse_multifile_document_in_archive()
		{
			var files = new List<string> { @"..\..\Data\Waybills\multifile\apteka_holding.rar" };
			ArchiveHelper.SevenZipExePath = @".\7zip\7z.exe";
			SetUp(files);
			Process_waybills();

			var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(2));
		}

		[Test, Description("В одном архиве находятся многофайловая и однофайловая накладные")]
		public void Parse_multifile_with_single_document()
		{
			var files = new List<string> { @"..\..\Data\Waybills\multifile\multifile_with_single.zip" };
			ArchiveHelper.SevenZipExePath = @".\7zip\7z.exe";
			SetUp(files);
			Process_waybills();

			var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(3));

			var singleFile = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*(0000470553).dbf",
                SearchOption.AllDirectories);
			Assert.That(singleFile.Count(), Is.EqualTo(1));			
		}

		[Test]
		public void Parse_with_more_then_one_common_column()
		{
			var files = new List<string> {
                @"..\..\Data\Waybills\multifile\h1766399.dbf",
                @"..\..\Data\Waybills\multifile\b1766399.dbf"
			};
			SetUp(files);
			Process_waybills();

			var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(2));
		}
	}
}
