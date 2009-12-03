using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Properties;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Threading;
using System.IO;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class EMAILSourceHandlerTest : EMAILSourceHandler
	{
		private string[] _archiveNames = new string[5] { "1.zip", "price_no_password.zip", "price_rar.rar", "price_zip.zip", "price_rar2.rar" };

		private string[] _pricesMasks = new string[5] { "1.zip", "price_no_*.*", "price_rar.rar", "price_zip.zip", "*_rar2.rar" };

		private string[] _pricesExtrMasks = new string[5] { "*.txt", "price_no_*.txt", "*.txt", "*.*", "*.*"};

		private string[] _archivePasswords = new string[5] { "123", "", "rar", "zip", "rar" };

		private ulong[] _sourceIds = new ulong[10] { 3, 7, 10, 13, 18, 22, 30, 37, 38, 45 };

		private static string _dataDir = @"..\..\Data\";

		[Test]
		public void IsMailAddresTest()
		{
			Assert.AreEqual(true, IsMailAddress("test@analit.net"), "Адрес некорректен");
			Assert.AreEqual(false, IsMailAddress("zakaz"), "Адрес некорректен");
			Assert.AreEqual(false, IsMailAddress("zakaz@"), "Адрес некорректен");
			Assert.AreEqual(true, IsMailAddress("zakaz@dsds"), "Адрес некорректен");
			Assert.AreEqual(true, IsMailAddress("<'prices@spb.analit.net'>"), "Адрес некорректен");
		}

		[Test(Description = "Тест для обработки прайсов, пришедших по email в запароленных архивах")]
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
