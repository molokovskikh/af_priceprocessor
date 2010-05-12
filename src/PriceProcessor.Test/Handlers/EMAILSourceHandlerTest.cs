using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom.Common;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Properties;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.SMTP.Client;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Threading;
using System.IO;
using Test.Support;

namespace PriceProcessor.Test
{
	public class EmailSourceHandlerForTesting : EMAILSourceHandler
	{
		public void Process()
		{
			CreateDirectoryPath();
			CreateWorkConnection();
			ProcessData();
		}
	}

	[TestFixture]
	public class EMAILSourceHandlerTest : EMAILSourceHandler
	{
		private string[] _archiveNames = new string[5] { "1.zip", "price_no_password.zip", "price_rar.rar", "price_zip.zip", "price_rar2.rar" };

		private string[] _pricesMasks = new string[5] { "1.zip", "price_no_*.*", "price_rar.rar", "price_zip.zip", "*_rar2.rar" };

		private string[] _pricesExtrMasks = new string[5] { "*.txt", "price_no_*.txt", "*.txt", "*.*", "*.*"};

		private string[] _archivePasswords = new string[5] { "123", "", "rar", "zip", "rar" };

		private ulong[] _sourceIds = new ulong[10] { 3, 7, 10, 13, 18, 22, 30, 37, 38, 45 };

		private static string _dataDir = @"..\..\Data\";

		private SummaryInfo _summary = new SummaryInfo();

		public void SetUp(IList<string> fileNames, string emailTo, string emailFrom)
		{
			ArchiveHelper.SevenZipExePath = @".\7zip\7z.exe";
			TestHelper.RecreateDirectories();

			var client = TestClient.CreateSimple();
			var supplier = TestOldClient.CreateTestSupplier();
			_summary.Client = client;
			_summary.Supplier = supplier;

			var email = String.Format("{0}test@test.test", supplier.Id);
			if (String.IsNullOrEmpty(emailTo))
				emailTo = email;
			if (String.IsNullOrEmpty(emailFrom))
				emailFrom = email;
			TestPriceSource.CreateEmailPriceSource(emailFrom, emailTo, "*.*", "*.*");

			TestHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);

			byte[] bytes;
			foreach (var file in fileNames)
			{
				if (Path.GetExtension(file.ToLower()) == ".eml")
				{
					bytes = File.ReadAllBytes(file);
				}
				else
				{
					var message = TestHelper.BuildMessageWithAttachments(
						String.Format(emailTo),
						String.Format(emailFrom), new[] { file });
					bytes = message.ToByteData();
				}
				TestHelper.StoreMessage(
					Settings.Default.TestIMAPUser,
					Settings.Default.TestIMAPPass,
					Settings.Default.IMAPSourceFolder, bytes);
			}
		}

		[Test]
		public void IsMailAddresTest()
		{
			Assert.AreEqual(true, IsMailAddress("test@analit.net"), "Адрес некорректен");
			Assert.AreEqual(false, IsMailAddress("zakaz"), "Адрес некорректен");
			Assert.AreEqual(false, IsMailAddress("zakaz@"), "Адрес некорректен");
			Assert.AreEqual(true, IsMailAddress("zakaz@dsds"), "Адрес некорректен");
			Assert.AreEqual(true, IsMailAddress("<'prices@spb.analit.net'>"), "Адрес некорректен");
		}

		[Test(Description = "Тест для обработки прайсов, пришедших по email в запароленных архивах"), Ignore]
		public void EmailPriceInPasswordProtectedArchiveProcessingTest()
		{
			var email = "d.dorofeev@analit.net";

			// Очищаем IMAP папку
			TestHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass,
			                           Settings.Default.IMAPSourceFolder);

			// Пересоздаем папки (чтоб в них не было файлов)
			TestHelper.RecreateDirectories();

			// Обновляем записи в таблице sources
			PrepareSourcesTable();

			var index = 0;
			foreach (var fileName in _archiveNames)
			{
				// Кладем в IMAP папку сообщение с вложениями
				TestHelper.StoreMessageWithAttachToImapFolder(Settings.Default.TestIMAPUser,
				                                              Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder,
				                                              email, email, _dataDir + fileName);
			}

			// Запускаем обработчик
			var handler = new EMAILSourceHandler();
			handler.StartWork();
			Thread.Sleep(2000);
			handler.StopWork();

			var list = GetPriceItemIdsBySourceId(_sourceIds[index]);

			index++;
			var priceItemInQueue = false;
			foreach (var item in PriceItemList.list)
			{
				if (list.Contains(item.PriceItemId))
				{
					priceItemInQueue = true;
					break;
				}
			}
			Assert.IsTrue(priceItemInQueue, "Ошибка обработки файла. Файл не поставлен в очередь на формализацию");
		}

		[Test, Description("Тест для обработки прайс-листа в письме"), Ignore("Тест для отладки обработчика писем")]
		public void Process_price_in_message()
		{
			var files = new[] { @"..\..\Data\EmailSourceHandlerTest\Price_ProgTechnologi.eml" };
			var emailTo = "prices@izh.analit.net";
			var emailFrom = "prtech@udmnet.ru";
			SetUp(files, emailTo, emailFrom);
			var handler = new EmailSourceHandlerForTesting();
			handler.Process();
		}

		[Test, Ignore("Тест для обработки всех писем-файлов находящихся в определенной директории. Проверяется что распаковали файл и что его размер > 0")]
		public void HandleAllMessagesInDirectory()
		{
			// Директория, где лежат *.eml файлы. Для каждого теста указывается своя директория.
			// Не стал добавлять все файлы в свн чтобы не увеличивать размер
			const string dataDirectory = "C:\\history_test";
			var mailboxAddress = Settings.Default.TestIMAPUser;
			var mailboxPassword = Settings.Default.TestIMAPPass;
			var imapFolder = Settings.Default.IMAPSourceFolder;

			TestHelper.ClearImapFolder(mailboxAddress, mailboxPassword, imapFolder);
			TestHelper.RecreateDirectories();

			var filePaths = Directory.GetFiles(dataDirectory, "*.eml", SearchOption.AllDirectories);
			var indexItem = 0;
			using (var imapClient = new IMAP_Client())
			{
				imapClient.Connect(Settings.Default.IMAPHost, Convert.ToInt32(Settings.Default.IMAPPort));
				imapClient.Authenticate(mailboxAddress, mailboxPassword);
				imapClient.SelectFolder(Settings.Default.IMAPSourceFolder);
				var index = 0;
				var countDownlogs = 0;
				foreach (var filePath in filePaths)
				{
					var queryDeleteLogs = @"delete from `logs`.downlogs where logtime > curdate()";
					With.Connection(connection => countDownlogs = Convert.ToInt32(MySqlHelper.ExecuteScalar(connection, queryDeleteLogs)));
					var bytes = File.ReadAllBytes(filePath);
					imapClient.StoreMessage(imapFolder, bytes);
					index++;
					var handler = new EMAILSourceHandler();
					handler.StartWork();
					Thread.Sleep(8000);
					handler.StopWork();
					TestHelper.ClearImapFolder(mailboxAddress, mailboxPassword, imapFolder);
					index = 0;
					var querySelectLogs = @"select count(*) from `logs`.downlogs where logtime > curdate()";
					With.Connection(connection => countDownlogs = Convert.ToInt32(MySqlHelper.ExecuteScalar(connection, querySelectLogs)));
					Assert.That(countDownlogs, Is.GreaterThanOrEqualTo(1), String.Format("Сбой произошел на файле {0}", filePath));
				}
			}
		}

		[Test]
		public void Delete_broken_message()
		{
			Setup.Initialize("DB");
			using (new TransactionScope())
			{
				TestPriceSource.Queryable.Where(s => s.EmailFrom == "naturpr@kursknet.ru" && s.EmailTo == "prices@kursk.analit.net")
					.Each(s => s.Delete());
			}

			var begin = DateTime.Now;
			File.Copy(@"..\..\Data\EmailSourceHandlerTest\app.config", "PriceProcessor.Test.config", true);
			Settings.Default.Reload();

			var goodPriceItem = TestPriceSource.CreateEmailPriceSource("naturpr@kursknet.ru", "prices@kursk.analit.net", "Прайс-лист.rar", "Прайс-лист.xls");
			var badPriceItem = TestPriceSource.CreateEmailPriceSource("order.moron@gmail.com", "prices@volgograd.analit.net", "price.zip", "price.dbf");

			Send(@"..\..\Data\EmailSourceHandlerTest\Bad.eml");
			Send(@"..\..\Data\EmailSourceHandlerTest\Good.eml");

			var handler = new EMAILSourceHandler();
			handler.StartWork();
			Thread.Sleep(50.Second());
			handler.StopWork();

			using (new SessionScope())
			{
				var logs = PriceDownloadLog.Queryable.Where(l => l.LogTime > begin).ToList();

				Assert.That(logs.Count, Is.EqualTo(1));

				var log = logs[0];
				Assert.That(log.PriceItemId, Is.EqualTo(goodPriceItem.Id));
				Assert.That(log.ResultCode, Is.EqualTo(2));
				Assert.That(log.Addition, Is.Null);
			}
		}

		public void Send(string email)
		{
			SmtpClientEx.QuickSendSmartHost("box.analit.net",
				25,
				Environment.MachineName,
				"service@analit.net", 
				new[] {"KvasovTest@analit.net"},
				File.OpenRead(email));
		}

		private List<ulong> GetPriceItemIdsBySourceId(ulong sourceId)
		{
			var list = new List<ulong>();

			var query = String.Format(@"
SELECT 
	Id
FROM
	usersettings.PriceItems
WHERE
	SourceId = {0}", sourceId);

			var reader = MySqlHelper.ExecuteReader(ConfigurationManager.ConnectionStrings["DB"].ConnectionString, query);
			while (reader.Read())
			{
				list.Add(reader.GetUInt64(0));
			}
			return list;
		}

		private void PrepareSourcesTable()
		{
			var queryUpdate = @"
UPDATE farm.sources
SET
  EMailTo = ?EmailTo,
  EmailFrom = ?EmailFrom,
  PriceMask = ?PriceMask,
  ExtrMask = ?ExtrMask,
  ArchivePassword = ?ArchivePassword
WHERE
  Id = ?SourceId
";

			var email = "d.dorofeev@analit.net";

			var indexSourceId = 0;

			for (var index = 0; index < _archiveNames.Length; index++)
			{
				var paramEmailTo = new MySqlParameter("?EmailTo", email);
				var paramEmailFrom = new MySqlParameter("?EmailFrom", email);
				var paramPriceMask = new MySqlParameter("?PriceMask", _pricesMasks[index]);
				var paramExtrMask = new MySqlParameter("?ExtrMask", _pricesExtrMasks[index]);
				var paramArchivePassword = new MySqlParameter("?ArchivePassword", _archivePasswords[index]);

				while (indexSourceId < _sourceIds.Length)
				{
					try
					{
						var paramSourceId = new MySqlParameter("?SourceId", _sourceIds[indexSourceId]);
						With.Connection(connection => {
							MySqlHelper.ExecuteNonQuery(connection, queryUpdate, paramEmailTo, paramEmailFrom,
                                paramPriceMask, paramExtrMask, paramSourceId, paramArchivePassword);
                            });
						break;
					}
					catch (Exception)
					{
					}
					finally
					{
						indexSourceId++;
					}
				}
			}
		}

	}
}
