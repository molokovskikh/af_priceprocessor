using System;
using System.IO;
using System.Text;
using Inforoom.PriceProcessor;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;

namespace PriceProcessor.Test.TestHelpers
{
	public class ImapHelper
	{
		public static void ClearImapFolder()
		{
			ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
		}

		public static void StoreMessage(string mailbox, string password, string folder, byte[] messageBytes)
		{
			using (var imapClient = new IMAP_Client())
			{
				imapClient.Connect(Settings.Default.IMAPHost, Convert.ToInt32(Settings.Default.IMAPPort));
				imapClient.Authenticate(mailbox, password);
				imapClient.SelectFolder(Settings.Default.IMAPSourceFolder);
				imapClient.StoreMessage(folder, messageBytes);
			}
		}

		/// <summary>
		/// Удаляет все сообщения из IMAP папки
		/// </summary>
		public static void ClearImapFolder(string mailbox, string password, string folder)
		{
			using (var imapClient = new IMAP_Client())
			{
				imapClient.Connect(Settings.Default.IMAPHost, Convert.ToInt32(Settings.Default.IMAPPort));
				imapClient.Authenticate(mailbox, password);
				imapClient.SelectFolder(folder);
				var sequenceSet = new IMAP_SequenceSet();
				sequenceSet.Parse("1:*", Int64.MaxValue);
				var items = imapClient.FetchMessages(sequenceSet, IMAP_FetchItem_Flags.UID, false, false);
				if ((items != null) && (items.Length > 0))
				{
					foreach (var item in items)
					{
						var sequenceMessages = new IMAP_SequenceSet();
						sequenceMessages.Parse(item.UID.ToString());
						imapClient.DeleteMessages(sequenceMessages, true);
					}
				}
			}
		}

		public static void StoreMessage(byte[] messageBytes)
		{
			StoreMessage(Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder,
				messageBytes);
		}

		public static void StoreMessage(string filename)
		{
			StoreMessage(File.ReadAllBytes(filename));
		}

		public static Mime BuildMessageWithAttachments(string to, string from, string[] files)
		{
			var fromAddresses = new AddressList();
			fromAddresses.Parse(@from);
			var responseMime = new Mime();
			responseMime.MainEntity.From = fromAddresses;
			var toList = new AddressList { new MailboxAddress(to) };
			responseMime.MainEntity.To = toList;
			responseMime.MainEntity.Subject = "[Debug message]";
			responseMime.MainEntity.ContentType = MediaType_enum.Multipart_mixed;

			foreach (var fileName in files)
			{
				var testEntity = responseMime.MainEntity.ChildEntities.Add();
				testEntity.ContentType = MediaType_enum.Text_plain;
				testEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
				testEntity.DataText = "";

				var attachEntity = responseMime.MainEntity.ChildEntities.Add();
				attachEntity.ContentType = MediaType_enum.Application_octet_stream;
				attachEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
				attachEntity.ContentDisposition = ContentDisposition_enum.Attachment;
				attachEntity.ContentDisposition_FileName = Path.GetFileName(fileName);

				using (var fileStream = File.OpenRead(fileName))
				{
					var fileBytes = new byte[fileStream.Length];
					fileStream.Read(fileBytes, 0, (int) (fileStream.Length));
					attachEntity.Data = fileBytes;
				}
			}
			return responseMime;
		}

		/// <summary>
		/// Кладет сообщение с файлом-вложением в IMAP папку.
		/// Ящик, пароль и название IMAP папки берутся из конфигурационного файла.
		/// </summary>
		/// <param name="to">Адрес, который будет помещен в поле TO</param>
		/// <param name="from">Адрес, который будет помещен в поле FROM</param>
		/// <param name="attachFilePath">Путь к файлу, который будет помещен во вложение к этому письму</param>
		public static void StoreMessageWithAttachToImapFolder(string mailbox, string password, string folder, 
		                                                      string to, string from, string attachFilePath)
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
				fileStream.Read(fileBytes, 0, (int)(fileStream.Length));
				var messageText = String.Format(templateMessageText, to, @from,
					Path.GetFileName(attachFilePath), Convert.ToBase64String(fileBytes));
				var messageBytes = new UTF8Encoding().GetBytes(messageText);
				StoreMessage(mailbox, password, folder, messageBytes);
			}
		}

		public static void StoreMessageWithAttachToImapFolder(string to, string from, string attachFilePath)
		{
			StoreMessageWithAttachToImapFolder(Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder,
				to,
				@from,
				attachFilePath);
		}
	}
}