using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using LumiSoft.Net.SMTP.Client;
using NUnit.Framework;
using System.IO;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using PriceSourceType = Test.Support.PriceSourceType;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class EmailSourceHandlerTest : BaseHandlerFixture<EMAILSourceHandler>
	{
		private static string _dataDir = @"..\..\Data\";

		[SetUp]
		public void Setup()
		{
			source.SourceType = PriceSourceType.Email;
			source.Save();

			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
		}

		[Test]
		public void IsMailAddresTest()
		{
			Assert.AreEqual(true, handler.IsMailAddress("test@analit.net"), "Адрес некорректен");
			Assert.AreEqual(false, handler.IsMailAddress("zakaz"), "Адрес некорректен");
			Assert.AreEqual(false, handler.IsMailAddress("zakaz@"), "Адрес некорректен");
			Assert.AreEqual(true, handler.IsMailAddress("zakaz@dsds"), "Адрес некорректен");
			Assert.AreEqual(true, handler.IsMailAddress("<'prices@spb.analit.net'>"), "Адрес некорректен");
		}

		[Test(Description = "Тест для обработки прайсов, пришедших по email в запароленных архивах")]
		public void EmailPriceInPasswordProtectedArchiveProcessingTest()
		{
			var email = "d.dorofeev@analit.net";
			source.EmailFrom = email;
			source.EmailTo = email;
			source.PriceMask = "1.zip";
			source.ExtrMask = "*.txt";
			source.ArchivePassword = "123";
			source.Save();

			// Кладем в IMAP папку сообщение с вложениями
			ImapHelper.StoreMessageWithAttachToImapFolder(Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder,
				email, email, Path.Combine(_dataDir, "1.zip"));

			// Запускаем обработчик
			Process();

			Assert.IsTrue(PriceItemList.list.Any(l => l.PriceItemId == priceItem.Id), "Ошибка обработки файла. Файл не поставлен в очередь на формализацию");
		}

		[Test, Description("Тест для обработки прайс-листа в письме ()")]
		public void Process_price_in_message()
		{
			var file = @"..\..\Data\EmailSourceHandlerTest\Price_ProgTechnologi.eml";
			source.EmailTo = "prices@izh.analit.net";
			source.EmailFrom = "prtech@udmnet.ru";
			source.PriceMask = "*.*";
			source.ExtrMask = "*.*";
			source.Save();

			ImapHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, File.ReadAllBytes(file));

			handler.ProcessData();
		}

		[Test, Ignore]
		public void Delete_broken_message()
		{
			using (new TransactionScope())
			{
				TestPriceSource.Queryable
					.Where(s => s.EmailFrom == "naturpr@kursknet.ru" && s.EmailTo == "prices@kursk.analit.net")
					.Each(s => s.Delete());
			}

			var begin = DateTime.Now;

			source.EmailFrom = "naturpr@kursknet.ru";
			source.EmailTo = "prices@kursk.analit.net";
			source.PriceMask = "Прайс-лист.rar";
			source.ExtrMask = "Прайс-лист.xls";
			source.Save();

			Send(@"..\..\Data\EmailSourceHandlerTest\Bad.eml");
			Send(@"..\..\Data\EmailSourceHandlerTest\Good.eml");

			handler.ProcessData();

			using (new SessionScope())
			{
				var logs = PriceDownloadLog.Queryable.Where(l => l.LogTime > begin).ToList();

				Assert.That(logs.Count, Is.EqualTo(1));

				var log = logs[0];
				Assert.That(log.PriceItemId, Is.EqualTo(priceItem.Id));
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
	}
}
