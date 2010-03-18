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
using System.Net;
using System.Reflection;

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
					imapClient.Connect("box.analit.net", 143);
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
					sc = new SmtpClient("box.analit.net");
					/*
					 * Проблема с задержкой при отправке писем с помощью SmtpClient не лечится ничем, кроме таймаута
					 * Если SmtpClient создается только в тесте, то настройки ServicePoint, которые закомментированны ниже
					 * помогают решить проблему. Если перед тестом происходит создание SmtpClient без изменения настроек,
					 * то изменение настроект в тесте не решает проблему.
					 * Настройки:
					 * sc.DeliveryMethod = SmtpDeliveryMethod.Network;
					 * sc.ServicePoint.MaxIdleTime = 1000;
					 * sc.ServicePoint.ConnectionLimit = 1;
					 * sc.ServicePoint.UseNagleAlgorithm = true;
					 * Источники:
					 * http://stackoverflow.com/questions/930236/net-best-method-to-send-email-system-net-mail-has-issues
					 * http://social.msdn.microsoft.com/Forums/en-US/netfxnetcom/thread/6ce868ba-220f-4ff1-b755-ad9eb2e2b13d
					 * http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=146711
					 */
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
						sc = new SmtpClient("box.analit.net");
						sc.Send(mailMessage);
					}

					File.Delete(fileName);

					//Ждем две минуты, чтобы отработал pooling SmtpClient и письма все были отправлены.
					Thread.Sleep(120000);

					imapClient.SelectFolder("INBOX");

					IMAP_SequenceSet newSet = new IMAP_SequenceSet();
					newSet.Parse("1:*");
					items = imapClient.FetchMessages(newSet, IMAP_FetchItem_Flags.UID | IMAP_FetchItem_Flags.Envelope, false, false);

					//Если следующий ассерт будет срабатывать, то надо увеличить время ожидания перед запросом списка писем (несколько строчек выше)
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
