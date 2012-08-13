using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LumiSoftTest.Extensions;
using NUnit.Framework;
using LumiSoft.Net.Mail;
using System.IO;
using LumiSoft.Net.MIME;
using LumiSoft.Net.IMAP.Client;

namespace LumiSoftTest
{
    [TestFixture]
    public class LumiSoftImapFixture
    {
		// Если нужно протестировать на большом кол-ве файлов, директорию с файлами *.eml нужно указать до запуска теста.
		// Т.к. файлов много, то не стал добавлять их все в SVN. Добавил только те, с которыми возникают проблемы.
		private static readonly string _dataDirectory = @"..\..\Data";

		private void CheckErrorList(IList<string> errorList)
		{
			if (errorList.Count > 0)
			{
				var message = String.Format("{0} Exceptions:\n", errorList.Count);
				foreach (var error in errorList)
				{
					message += error.ToString() + "\n";
				}
				Assert.Fail(message);
			}			
		}

		private void CheckMessageHeader(Action<Mail_Message> checkHeaderAction)
		{
			var tmpDir = Path.GetTempPath() + "TestGetHeaderFrom_EmlFile\\";
			var files = Directory.GetFiles(_dataDirectory, "*.eml", SearchOption.AllDirectories);
			if (Directory.Exists(tmpDir))
				Directory.Delete(tmpDir, true);
			var errorList = new List<string>();
			foreach (var filename in files)
			{
				var message = Mail_Message.ParseFromFile(filename, Encoding.UTF8);
				try
				{
					checkHeaderAction(message);
				}
				catch (Exception e)
				{
					errorList.Add(String.Format("File: {0} Exception:\n{1}", filename, e));
				}
			}
			CheckErrorList(errorList);			
		}

		private void CheckHeaderFrom(Mail_Message message)
		{
			Assert.IsTrue(message.From != null);
			Assert.IsTrue(message.From.Count > 0);			
		}

		private void CheckHeaderTo(Mail_Message message)
		{
			Assert.IsTrue(message.To != null);
			Assert.IsTrue(message.To.Count > 0);			
		}

        [Test, Ignore("Не реализовано")]
        public void TestConnectToImapServer()
        {
			using (var imapClient = new IMAP_Client())
			{
			}
        }

        [Test, Ignore("Не реализовано")]
        public void TestLoginToMailBox()
        {

        }

        [Test(Description = "Предполагается, что каждый eml файл имеет не менее 1 вложения"), Ignore]
        public void TestSaveAttachments_EmlFile()
        {
            var tmpDir = Path.GetTempPath() + @"TestSaveAttachment_EmlFile\";
            var files = Directory.GetFiles(_dataDirectory, "*.eml", SearchOption.AllDirectories);
            var index = 0;
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, true);
        	var errorList = new List<string>();
        	var failedDirectory = tmpDir + @"failed\";
			if (Directory.Exists(failedDirectory))
				Directory.Delete(failedDirectory, true);
        	Directory.CreateDirectory(failedDirectory);
            foreach (var filename in files)
            {
                var directory = tmpDir + index.ToString() + Path.DirectorySeparatorChar;
                Directory.CreateDirectory(directory);
                var message = Mail_Message.ParseFromFile(filename, Encoding.UTF8);
                IEnumerable<MIME_Entity> attachments = null;
                try
                {
                    attachments = message.GetValidAttachements();
					if ((attachments == null) || (attachments.Count() <= 0))
						throw new Exception(String.Format("Не найдено вложений в файле {0}", filename));
                }
                catch (Exception e)
                {
					errorList.Add(String.Format("File: {0} Exception:\n{1}", filename, e));
					File.Copy(filename,failedDirectory + Path.GetFileName(filename));
					continue;
                }
            	foreach (var attach in attachments)
                {
                    var filePath = directory + attach.GetFilename();
                    var messageBytes = ((MIME_b_SinglepartBase)attach.Body).Data;
                    using (var fileStream = new FileStream(filePath, FileMode.CreateNew))
                        fileStream.Write(messageBytes, 0, messageBytes.Length);
                    var fileInfo = new FileInfo(filePath);
                    Assert.Greater(fileInfo.Length, 0, String.Format("Получили файл-вложение нулевого размера при разборе файла {0}", filename));
                }
				index++;
            }
			CheckErrorList(errorList);
        }

        [Test]
        public void TestGetHeaderFrom_EmlFile()
        {
			CheckMessageHeader(CheckHeaderFrom);
        }

        [Test]
        public void TestGetHeaderTo_EmlFile()
        {
			CheckMessageHeader(CheckHeaderTo);
        }

		[Test, Ignore("Не реализовано")]
        public void TestDeleteMessageFromImapFolder()
        {
        }

		[Test, Ignore("Не реализовано")]
        public void TestStoreMessageToImapFolder()
        {

        }

        [Test]
        public void TestParseMailFromFile()
        {
            var files = Directory.GetFiles(_dataDirectory, "*.eml", SearchOption.AllDirectories);
            foreach (var filename in files)
            {
                try
                {
                    var message = Mail_Message.ParseFromFile(filename, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Assert.Fail(e.ToString());
                }
            }
        }
    }
}
