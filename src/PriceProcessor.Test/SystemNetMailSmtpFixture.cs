using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Net.Mail;
using System.IO;
using System.Threading;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Client;
using System.Collections;
using System.ServiceProcess;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class SystemNetMailSmtpFixture
	{
		//Остановили ли мы сервис вручную?
		bool serviceStopped = false;
		//Ссылка на сервис Symantec Antivirus
		ServiceController symantecAntiVirus = null;

		[TestFixtureSetUp]
		public void TestInit()
		{
			//Попытка найти сервис "Symantec AntiVirus" и остановить его, чтобы он не проксировал
			ServiceController[] services = ServiceController.GetServices();
			symantecAntiVirus = Array.Find<ServiceController>(
				services,
				delegate(ServiceController value) { return value.ServiceName.Equals("Symantec AntiVirus", StringComparison.OrdinalIgnoreCase); });

			if ((symantecAntiVirus != null) && (symantecAntiVirus.Status == ServiceControllerStatus.Running))
			{
				symantecAntiVirus.Stop();
				symantecAntiVirus.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
				Assert.That(symantecAntiVirus.Status, Is.EqualTo(ServiceControllerStatus.Stopped), "Не удалось остановить сервис Symantec AntiVirus");
				serviceStopped = true;
			}
		}

		[TestFixtureTearDown]
		public void TestEnd()
		{
			if (serviceStopped)
			{
				symantecAntiVirus.Start();
				symantecAntiVirus.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
			}
		}

		private void CreateFileForSmtpWithAttach(string fileName)
		{
			FileStream _tempStream = File.Create(fileName);
			using (TextWriter _textWriter = new StreamWriter(_tempStream))
			{
				_textWriter.WriteLine("This is test!");
				_textWriter.WriteLine("End test.");
			}
			_tempStream.Close();
		}

		[Test(Description = "стандартный клиент Smtp лочит файлы после отправки")]
		public void SmtpWithAttachLockedFileTest()
		{
			string _rootTempPath = Path.GetTempFileName();
			if (File.Exists(_rootTempPath))
				File.Delete(_rootTempPath);
			_rootTempPath = Path.GetDirectoryName(_rootTempPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(_rootTempPath);

			if (!Directory.Exists(_rootTempPath))
				Directory.CreateDirectory(_rootTempPath);

			try
			{
				using (IMAP_Client imapClient = new IMAP_Client())
				{
					imapClient.Connect("mail.adc.analit.net", 143);
					imapClient.Authenticate("prccopy@analit.net", "123");
					imapClient.SelectFolder("INBOX");
					IMAP_SequenceSet sequence_set = new IMAP_SequenceSet();
					sequence_set.Parse("1:*", long.MaxValue);
					IMAP_FetchItem[] items = imapClient.FetchMessages(sequence_set, IMAP_FetchItem_Flags.UID, false, false);
					if ((items != null) && (items.Length > 0))
					{
						string[] uids = Array.ConvertAll<IMAP_FetchItem, string>(items, delegate(IMAP_FetchItem value) { return value.UID.ToString(); });
						IMAP_SequenceSet deletedSet = new IMAP_SequenceSet();
						deletedSet.Parse(String.Join(",", uids));
						imapClient.DeleteMessages(deletedSet, true);
					}

					string fileName = _rootTempPath + "\\file1.txt";
					CreateFileForSmtpWithAttach(fileName);

					MailMessage mailMessage;
					SmtpClient sc;


					mailMessage = new MailMessage(
						"report@analit.net",
						"prccopy@analit.net",
						"тестовая тема SmtpWithAttachLockedFile " + Path.GetFileName(fileName),
						"Тестовое тело сообщения SmtpWithAttachLockedFile");
					mailMessage.Attachments.Add(new Attachment(fileName));
					sc = new SmtpClient("mail.adc.analit.net");
					sc.Send(mailMessage);

					try
					{
						File.Delete(fileName);
						Assert.Fail("Получилось удалить файл, заблокированный SmtpClient'ом.");
					}
					catch (System.IO.IOException exception)
					{
						Assert.That(
							System.Runtime.InteropServices.Marshal.GetHRForException(exception),
							Is.EqualTo((int)-2147024864),
							"Код ошибки отличается от ожидаемого.");
						mailMessage.Dispose();
						mailMessage = null;
						Thread.Sleep(2000);
						File.Delete(fileName);
					}


					fileName = _rootTempPath + "\\file2.txt";
					CreateFileForSmtpWithAttach(fileName);

					using (mailMessage = new MailMessage(
						"report@analit.net",
						"prccopy@analit.net",
						"тестовая тема SmtpWithAttachLockedFile " + Path.GetFileName(fileName),
						"Тестовое тело сообщения SmtpWithAttachLockedFile"))
					{
						mailMessage.Attachments.Add(new Attachment(fileName));
						sc = new SmtpClient("mail.adc.analit.net");
						sc.Send(mailMessage);
					}

					File.Delete(fileName);

					Thread.Sleep(5000);

					imapClient.SelectFolder("INBOX");

					IMAP_SequenceSet newSet = new IMAP_SequenceSet();
					newSet.Parse("1:*");
					items = imapClient.FetchMessages(newSet, IMAP_FetchItem_Flags.UID | IMAP_FetchItem_Flags.Envelope, false, false);

					Assert.That((items != null) && (items.Length == 2), "После теста в папке нет писем");
					Assert.That(items[0].Envelope.Subject.EndsWith("file1.txt"), "Первое письмо не было доставлено");
					Assert.That(items[1].Envelope.Subject.EndsWith("file2.txt"), "Второе письмо не было доставлено");

					string[] uidsFiles = Array.ConvertAll<IMAP_FetchItem, string>(items, delegate(IMAP_FetchItem value) { return value.UID.ToString(); });
					IMAP_SequenceSet deletedFilesSet = new IMAP_SequenceSet();
					deletedFilesSet.Parse(String.Join(",", uidsFiles));
					imapClient.DeleteMessages(deletedFilesSet, true);
				}
			}
			finally
			{
				Directory.Delete(_rootTempPath);
			}
		}
	}
}
