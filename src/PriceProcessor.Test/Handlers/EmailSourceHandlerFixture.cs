using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.Mime;
using LumiSoft.Net.SMTP.Client;
using NUnit.Framework;
using System.IO;
using PriceProcessor.Test.TestHelpers;
using PriceProcessor.Test.Waybills;
using Test.Support;
using FileHelper = Common.Tools.FileHelper;
using PriceSourceType = Test.Support.PriceSourceType;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class EmailSourceHandlerFixture : BaseHandlerFixture<EMAILSourceHandler>
	{
		private static string _dataDir = @"..\..\Data\";
		private TestClient client;
		private TestAddress address;

		[SetUp]
		public void Setup()
		{
			client = TestClient.Create(2, 2);
			address = client.Addresses[0];

			source.SourceType = PriceSourceType.Email;
			source.Save();

			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
		}

		[Test]
		public void IsMailAddresTest()
		{
			Assert.AreEqual(true, MimeEntityExtentions.IsMailAddress("test@analit.net"), "Адрес некорректен");
			Assert.AreEqual(false, MimeEntityExtentions.IsMailAddress("zakaz"), "Адрес некорректен");
			Assert.AreEqual(false, MimeEntityExtentions.IsMailAddress("zakaz@"), "Адрес некорректен");
			Assert.AreEqual(true, MimeEntityExtentions.IsMailAddress("zakaz@dsds"), "Адрес некорректен");
			Assert.AreEqual(true, MimeEntityExtentions.IsMailAddress("<'prices@spb.analit.net'>"), "Адрес некорректен");
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

		public void Send(string email)
		{
			SmtpClientEx.QuickSendSmartHost("box.analit.net",
				25,
				Environment.MachineName,
				"service@analit.net",
				new[] { "KvasovTest@analit.net" },
				File.OpenRead(email));
		}
	}
}