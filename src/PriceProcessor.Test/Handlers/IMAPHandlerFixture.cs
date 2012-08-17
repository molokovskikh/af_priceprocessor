using System;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Rhino.Mocks;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class IMAPHandlerFixture
	{
		[Test(Description = "простая проверка работоспособности класс IMAPHandler")]
		public void SimpleProcessImap()
		{
			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			ImapHelper.StoreMessage(@"..\..\Data\Unparse.eml");

			var imapReader = MockRepository.GenerateStub<IIMAPReader>();
			imapReader.Stub(s => s.IMAPAuth(null))
				.IgnoreArguments()
				.Do(new Action<IMAP_Client>(client => client.Authenticate(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass)));

			var handler = new IMAPHandler(imapReader);

			handler.ProcessIMAPFolder();

			imapReader.AssertWasCalled(r => r.IMAPAuth(Arg<IMAP_Client>.Is.Anything));
			imapReader.AssertWasCalled(r => r.PingReader());
			imapReader.AssertWasCalled(r => r.ProcessMime(Arg<Mime>.Is.Anything));
			imapReader.AssertWasNotCalled(r => r.ProcessBrokenMessage(Arg<IMAP_FetchItem>.Is.Anything, Arg<IMAP_FetchItem[]>.Is.Anything, Arg<Exception>.Is.Anything));

			var existsMessages = ImapHelper.CheckImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			Assert.That(existsMessages.Count, Is.EqualTo(0), "Существуют письма в IMAP-папками с темами: {0}", existsMessages.Select(m => m.Envelope.Subject).Implode());
		}

		[Test(Description = "обрабатываем исключения при разборе письма")]
		public void ProcessReaderException()
		{
			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			ImapHelper.StoreMessage(@"..\..\Data\Unparse.eml");

			var imapReader = MockRepository.GenerateStub<IIMAPReader>();
			imapReader.Stub(s => s.IMAPAuth(null))
				.IgnoreArguments()
				.Do(new Action<IMAP_Client>(client => client.Authenticate(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass)));

			var exception = new Exception("ошибка при разборе письма в reader'е");

			imapReader.Stub(s => s.ProcessMime(null))
				.IgnoreArguments()
				.Do(new Action<Mime>(mime => { throw exception; }));
			var handler = new IMAPHandler(imapReader);

			handler.ProcessIMAPFolder();

			imapReader.AssertWasCalled(r => r.IMAPAuth(Arg<IMAP_Client>.Is.Anything));
			imapReader.AssertWasCalled(r => r.PingReader());
			imapReader.AssertWasCalled(r => r.ProcessMime(Arg<Mime>.Is.Anything));
			imapReader.AssertWasNotCalled(r => r.ProcessBrokenMessage(Arg<IMAP_FetchItem>.Is.Anything, Arg<IMAP_FetchItem[]>.Is.Anything, Arg<Exception>.Is.Equal(exception)));

			var existsMessages = ImapHelper.CheckImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			Assert.That(existsMessages.Count, Is.EqualTo(1), "Письмо было удалено сразу же после возникнования ошибок");
		}

		public class UIDInfoForTesting : UIDInfo
		{
			public UIDInfoForTesting() : base(1)
			{
			}
		}

		[Test(Description = "проверка срабатывания таймаута при обработке писем")]
		public void CheckUIDTimeout()
		{
			var createTime = DateTime.Now;
			var info = MockRepository.GenerateMock<UIDInfoForTesting>();

			var imapReader = MockRepository.GenerateStub<IIMAPReader>();
			var handler = new IMAPHandler(imapReader);

			info.Stub(i => i.CreateTime).Return(createTime);

			Assert.That(handler.UIDTimeout(info), Is.False);

			createTime = createTime.AddMinutes(-(Settings.Default.UIDProcessTimeout + 1));

			info.BackToRecord(BackToRecordOptions.PropertyBehavior);
			SetupResult.For(info.CreateTime).Return(createTime);
			info.Replay();

			Assert.That(handler.UIDTimeout(info), Is.True);
		}

		[Test(Description = "при возникновении ошибок письма должны обрабатываться несколько раз пока не возникнет таймаут")]
		public void ProcessMessageWithTimeoutOnError()
		{
			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			ImapHelper.StoreMessage(@"..\..\Data\Unparse.eml");

			var imapReader = MockRepository.GenerateStub<IIMAPReader>();
			imapReader.Stub(s => s.IMAPAuth(null))
				.IgnoreArguments()
				.Do(new Action<IMAP_Client>(client => client.Authenticate(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass)));

			var exception = new Exception("ошибка при разборе письма в reader'е");

			imapReader.Stub(s => s.ProcessMime(null))
				.IgnoreArguments()
				.Do(new Action<Mime>(mime => { throw exception; }));

			var handler = new IMAPHandler(imapReader);

			//Обрабатываем письмо первый раз: оно должно попасть в ErrorInfos
			Assert.That(handler.ErrorInfos.Count, Is.EqualTo(0));

			handler.ProcessIMAPFolder();

			imapReader.AssertWasCalled(r => r.IMAPAuth(Arg<IMAP_Client>.Is.Anything));
			imapReader.AssertWasCalled(r => r.PingReader());
			imapReader.AssertWasCalled(r => r.ProcessMime(Arg<Mime>.Is.Anything));
			imapReader.AssertWasNotCalled(r => r.ProcessBrokenMessage(Arg<IMAP_FetchItem>.Is.Anything, Arg<IMAP_FetchItem[]>.Is.Anything, Arg<Exception>.Is.Equal(exception)));

			var existsMessages = ImapHelper.CheckImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			Assert.That(existsMessages.Count, Is.EqualTo(1), "Письмо было удалено");

			Assert.That(handler.ErrorInfos.Count, Is.EqualTo(1));

			//Обрабатываем письмо второй раз: оно не должно быть обработано
			handler.ProcessIMAPFolder();

			imapReader.AssertWasCalled(r => r.ProcessMime(Arg<Mime>.Is.Anything), options => options.Repeat.Times(2));
			imapReader.AssertWasNotCalled(r => r.ProcessBrokenMessage(Arg<IMAP_FetchItem>.Is.Anything, Arg<IMAP_FetchItem[]>.Is.Anything, Arg<Exception>.Is.Equal(exception)));

			existsMessages = ImapHelper.CheckImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			Assert.That(existsMessages.Count, Is.EqualTo(1), "Письмо было удалено");

			Assert.That(handler.ErrorInfos.Count, Is.EqualTo(1));

			//Подменяем информацию о письме, чтобы сработал таймаут и в третий раз обрабатываем письмо: оно должно обработаться
			var realInfo = handler.ErrorInfos[0];
			var info = MockRepository.GenerateMock<UIDInfoForTesting>();
			info.Stub(i => i.UID).Return(realInfo.UID);
			info.Stub(i => i.CreateTime).Return(DateTime.Now.AddMinutes(-(Settings.Default.UIDProcessTimeout + 1)));
			handler.ErrorInfos.Clear();
			handler.ErrorInfos.Add(info);

			handler.ProcessIMAPFolder();

			imapReader.AssertWasCalled(r => r.ProcessMime(Arg<Mime>.Is.Anything), options => options.Repeat.Times(3));
			imapReader.AssertWasCalled(r => r.ProcessBrokenMessage(Arg<IMAP_FetchItem>.Is.Anything, Arg<IMAP_FetchItem[]>.Is.Anything, Arg<Exception>.Is.Equal(exception)));

			existsMessages = ImapHelper.CheckImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			Assert.That(existsMessages.Count, Is.EqualTo(0), "Существуют письма в IMAP-папками с темами: {0}", existsMessages.Select(m => m.Envelope.Subject).Implode());

			Assert.That(handler.ErrorInfos.Count, Is.EqualTo(0));
		}
	}
}