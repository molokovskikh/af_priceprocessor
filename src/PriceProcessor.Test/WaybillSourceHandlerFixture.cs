using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LumiSoft.Net.IMAP;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Properties;
using System.Threading;
using System.IO;
using LumiSoft.Net.IMAP.Client;
using Inforoom.Downloader.Documents;
using MySql.Data.MySqlClient;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class WaybillSourceHandlerFixture
	{
		private string _imapServer = Settings.Default.IMAPHost;

		private int _imapServerPort = 143;

		private string _imapUser = Settings.Default.TestIMAPUser;

		private string _imapPassword = Settings.Default.TestIMAPPass;

		private WaybillType _waybillType = new WaybillType();

		private RejectType _rejectType = new RejectType();

		// Имена файлов-архивов, в которых находятся накладные и отказы
		private static string[] _archiveNames = new string[3] { "e32wd.zip", "hys38.zip", "wwwe2.zip" };

		// Имена файлов, находящихся в архиве. Каждому архиву соответствует имя файла (это собственно сами накладные и отказы)
		private static string[] _fileNamesInArchives = new string[3] { "we3y.txt", "yw6.xls", "w3q2.dbf" };

		// Имена файлов, которые находятся вне архивов (например, прислали накладную не в архиве, а просто файлом)
		private static string[] _fileNames = new string[1] { "gg2.xls" };

		// Email-Ы поставщиков
		private static string[] _suppliersEmails = new string[2] { "test@protek.ru", "test@katren.com" };

		// Коды поставщиков
		private static int[] _supplierCodes = new int[2] { 10, 7 };

		// Коды клиентов, соответствующие поставщикам (для каждого поставщика берется два клиента)
		private static int[,] _clientsCodes = new int[2, 2] { {123, 212}, {11, 21} };

		private static string _dataDir = @"..\..\Data\";

		//[TestFixtureSetUp]
		[Test]
		public void InitTest()
		{
			DeleteFolders();
			// Удаляем все сообщения из папки Inbox
			DeleteAllMessages();
			// Формируем в папке Inbox письма с накладными
			CreateDocumentsInMailbox();
			// Настраиваем поставщиков в таблицах usersettings.ClientsData и documents.waybill_sources
			UpdateSuppliers();

			CreateClients();
		}

		private void DeleteFolders()
		{
			if (Directory.Exists(Settings.Default.TempPath))
				Directory.Delete(Settings.Default.TempPath, true);
			if (Directory.Exists(Settings.Default.FTPOptBoxPath))
				Directory.Delete(Settings.Default.FTPOptBoxPath, true);
			if (Directory.Exists(Settings.Default.DownWaybillsPath))
				Directory.Delete(Settings.Default.DownWaybillsPath, true);
		}

		private void UpdateSuppliers()
		{
			var queryUpdate = @"
UPDATE documents.waybill_sources w
SET w.EMailFrom = ?EmailFrom
WHERE w.FirmCode = ?FirmCode";
			var queryDeleteWaybillSource = @"
DELETE FROM documents.waybill_sources
WHERE FirmCode = ?FirmCode
";
			var queryInsertFirm = @"
INSERT INTO documents.waybill_sources
VALUES (?FirmCode, ?EmailFrom, 1, NULL)
";
			var querySetActiveSupplier = @"
UPDATE usersettings.ClientsData cd
SET cd.FirmStatus = 1 
WHERE cd.FirmCode = ?FirmCode
";
			// Проходим по адресам поставщиков
			for (var i = 0; i < _suppliersEmails.Length; i++)
			{
				var paramEmailFrom = new MySqlParameter("?EmailFrom", _suppliersEmails[i]);
				var paramFirmCode = new MySqlParameter("?FirmCode", _supplierCodes[i]);
				// Пробуем удалить поставщика из таблицы documents.waybill_sources
				With.Connection(connection => {
					MySqlHelper.ExecuteNonQuery(connection, queryDeleteWaybillSource, paramFirmCode);
				});
				// Обновляем поле usersettings.ClientsData.FirmStatus, чтобы поставщик был точно включен
				With.Connection(connection => {
					MySqlHelper.ExecuteNonQuery(connection, querySetActiveSupplier, paramFirmCode);
				});
				try
				{
					// Пытаемся вставить поставщика в таблицу documents.waybill_sources
					With.Connection(connection => {
						MySqlHelper.ExecuteNonQuery(connection, queryInsertFirm, paramEmailFrom, paramFirmCode);
					});
				}
				catch (Exception)
				{
					// Если не получилось вставить строку, предполагаем что строка с таким FirmCode уже есть и пытаемся ее изменить
					With.Connection(connection => {
						MySqlHelper.ExecuteNonQuery(connection, queryUpdate, paramEmailFrom, paramFirmCode);
					});
				}
			}
		}

		// Создает в папке Inbox почтового ящика письма с вложениями
		// (письма от поставщика с накладными)
		private void CreateDocumentsInMailbox()
		{
			IEnumerable<string> att = _archiveNames.Concat<string>(_fileNames);
			List<string> attaches = att.ToList<string>();
			// Проверяем, что нужные нам файлы лежат в нужной директории
			foreach (var fileName in attaches)
			{
				var files = Directory.GetFiles(_dataDir, fileName);
				Assert.IsTrue(files.Length > 0, String.Format(
					"Не найден файл {0} в директории {1}", fileName, _dataDir));
			}

			var countSendedAttaches = 0;
			for (var i = 0; i < _suppliersEmails.Length; i++)
			{
				for (var j = 0; j < _clientsCodes.Rank; j++)
				{
					if (countSendedAttaches < attaches.Count)
					{
						var to = String.Empty;
						var attachFileName = _dataDir + attaches[countSendedAttaches];
						if ((j % 2) == 0)
							// Отправляем накладную
							to = String.Format("{0}@{1}", _clientsCodes[i,j], _waybillType.Domen);							
						else
							// Отправляем отказ
							to = String.Format("{0}@{1}", _clientsCodes[i,j], _rejectType.Domen);
						StoreMessageToMailbox(to, _suppliersEmails[i], attachFileName);
						countSendedAttaches++;
					}
				}
			}
		}

		// Кладет сообщение с вложением-накладной (или отказом) в папку Inbox
		private void StoreMessageToMailbox(string to, string from, string attachFilePath)
		{
			var templateMessageText = @"To: {0}
From: {1}
Subject: TestWaybillSourceHandler
Content-Type: multipart/mixed;
 boundary=""------------060602000201050608050809""

This is a multi-part message in MIME format.
--------------060602000201050608050809
Content-Type: text/plain; charset=UTF-8; format=flowed
Content-Transfer-Encoding: 7bit



--------------060602000201050608050809
Content-Type: application/octet-stream;
 name=""{2}""
Content-Transfer-Encoding: base64
Content-Disposition: attachment;
 filename=""{2}""

{3}
--------------060602000201050608050809--

";
			using (var fileStream = File.OpenRead(attachFilePath))
			{
				var fileBytes = new byte[fileStream.Length];
				fileStream.Read(fileBytes, 0, (int) (fileStream.Length));
				var messageText = String.Format(templateMessageText, to, from,
				                                Path.GetFileName(attachFilePath), Convert.ToBase64String(fileBytes));
				byte[] messageBytes = new UTF8Encoding().GetBytes(messageText);
				using (var imapClient = new IMAP_Client())
				{
					imapClient.Connect(_imapServer, _imapServerPort);
					imapClient.Authenticate(_imapUser, _imapPassword);
					imapClient.StoreMessage("Inbox", messageBytes);
				}
			}
		}

		// Удаляет все сообщения из Inbox
		private void DeleteAllMessages()
		{
			using (var imapClient = new IMAP_Client())
			{
				imapClient.Connect(_imapServer, _imapServerPort);
				imapClient.Authenticate(_imapUser, _imapPassword);
				imapClient.SelectFolder("INBOX");
				var sequenceSet = new IMAP_SequenceSet();
				sequenceSet.Parse("1:*", long.MaxValue);
				var items = imapClient.FetchMessages(sequenceSet, IMAP_FetchItem_Flags.UID, false, false);
				if ((items != null) && (items.Length > 0))
				{
					foreach (IMAP_FetchItem item in items)
					{
						var sequenceMessages = new IMAP_SequenceSet();
						sequenceMessages.Parse(item.UID.ToString(), long.MaxValue);
						imapClient.DeleteMessages(sequenceMessages, true);
					}
				}
			}
		}

		private void CreateClients()
		{
			var queryCreateAddress = @"
INSERT INTO future.Addresses 
VALUES (?AddressId, 0, 12, 'WaybillSourceHandler test address')
";
			var queryCreateNewClient = @"
INSERT INTO future.Clients 
VALUES (12, 1, 1, 1, 3, 1, 1, ""Тестовый клиент"", ""Клиент тест"", NULL, NOW(), 1)
";
			var queryCreateOldClient = @"
INSERT INTO usersettings.ClientsData
VALUES (?AddressId, 1, 1, 1, 1, 23, 1, 1, 1, ""Тестовая Аптека"", ""Аптека123"", """", ""Тестовый адрес"", NULL, NOW(), 1)
";
			var queryUpdateOldClient = @"
UPDATE usersettings.ClientsData
SET FirmStatus = 1, FirmType = 1
WHERE FirmCode = ?AddressId
";
			// Пробуем создать нового клиента в таблице future.Clients
			try
			{
				With.Connection(connection => {
					MySqlHelper.ExecuteNonQuery(connection, queryCreateNewClient);
				});
			}
			catch (Exception) {}
			var index = 0;
			// Проходим по всем кодам клиентов
			foreach (var clientCode in _clientsCodes)
			{
				var paramAddressId = new MySqlParameter("?AddressId", clientCode);
				try
				{
					if ((index % 2) == 0)
					{
						try
						{
							// Создаем старого клиента (в таблице usersettings.ClientsData)
							With.Connection(connection => {
								MySqlHelper.ExecuteNonQuery(connection, queryCreateOldClient, paramAddressId);
							});
						}
						catch (Exception)
						{
							// Если не получилось вставить новую строку, обновляем существующую
							With.Connection(connection => {
								MySqlHelper.ExecuteNonQuery(connection, queryUpdateOldClient, paramAddressId);
							});							
						}
					}
					else
					{
						// Создаем новый адрес (в таблице future.Addresses)
						With.Connection(connection => {
							MySqlHelper.ExecuteNonQuery(connection, queryCreateAddress, paramAddressId);
						});
					}
				}
				catch (Exception) {}
				index++;
			}
		}

		[Test]
		public void TestWaybillSourceHandler()
		{
			var handler = new WaybillSourceHandler(_imapUser, _imapPassword);
			// Запускаем обработчик
			handler.StartWork();
			// Немного ждем, чтобы он успел сделать работу
			Thread.Sleep(3000);
			// Останавливаем обработчик
			handler.StopWork();
			var index = 0;
			// Сливаем в один список имена накладных (отказов). 
			// Берутся имена файлов внутри архивов, а также имена файлов, которые не были в архивах
			IEnumerable<string> waybillFiles = _fileNamesInArchives.Concat(_fileNames);
			IList<string> processedFiles = waybillFiles.ToList();
			// Проходим по всем кодам клиентов
			foreach (var clientCode in _clientsCodes)
			{
				var documentDir = String.Empty;
				if ((index % 2) == 0)
					documentDir = _waybillType.FolderName;
				else
					documentDir = _rejectType.FolderName;
				// Получаем путь к папке, в которой лежат накладные (отказы)
				var dir = Settings.Default.FTPOptBoxPath + Path.DirectorySeparatorChar +
					clientCode.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + documentDir;
				index++;
				// Проходим по файлам, которые должны бать помещены в спец. папку (по накладным)
				for (var i = 0; i < processedFiles.Count; i++)
				{
					// Если мы нашли в папке файл накладной
					var files = Directory.GetFiles(dir, "*" + Path.GetFileNameWithoutExtension(processedFiles[i]) + "*");
					if (files.Length > 0)
					{
						// Удаляем его из списка, переходим к другому клиенту
						processedFiles.RemoveAt(i);
						break;
					}
				}
			}
			// Если в списке остались элементы, значит мы не все обработали, будет ошибка			
			var listNotProcessedFiles = String.Empty;			
			foreach (var file in processedFiles)
			{
				listNotProcessedFiles += file + " ";
			}
			Assert.IsTrue(processedFiles.Count == 0, String.Format("Не все файлы обработаны: {0}", listNotProcessedFiles));
		}
	}
}
