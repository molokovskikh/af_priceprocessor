using System;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
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

		interface ITestCall
		{
			 void ImplementationMethod();
		}

		class BaseClass : ITestCall
		{
			public int T = 0;

			public virtual void ImplementationMethod()
			{
				T = 1;
			}

			public ITestCall GetInterface()
			{
				return this;
			}
		}

		class ChildClass : BaseClass
		{
			public int C = 2;

			public override void ImplementationMethod()
			{
				C = 3;
			}
		}

		[Test(Description = "проверка того, какой метод будет вызываться, если метод интефейса переопределен потомком")]
		public void InterfaceCallMethod()
		{
			var parent = new BaseClass();
			Assert.That(parent.T, Is.EqualTo(0));
			var testCall = parent.GetInterface();
			testCall.ImplementationMethod();
			Assert.That(parent.T, Is.EqualTo(1));

			var child = new ChildClass();
			Assert.That(child.T, Is.EqualTo(0));
			Assert.That(child.C, Is.EqualTo(2));
			testCall = child.GetInterface();
			testCall.ImplementationMethod();
			Assert.That(child.T, Is.EqualTo(0));
			Assert.That(child.C, Is.EqualTo(3));
		}

		[Test(Description = "простая проверка работоспособности класс IMAPHandler")]
		public void SimpleProcessImap()
		{
			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			ImapHelper.StoreMessage(@"..\..\Data\Unparse.eml");

			var imapReader = MockRepository.GenerateStub<IIMAPReader>();
			imapReader.Stub(s => s.IMAPAuth(null))
				.IgnoreArguments()
				.Do(new Action<IMAP_Client>(client => {client.Authenticate(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);}) );

			var handler = new IMAPHandler(imapReader);

			handler.ProcessIMAPFolder();

			imapReader.AssertWasCalled(r => r.IMAPAuth(Arg<IMAP_Client>.Is.Anything));
			imapReader.AssertWasCalled(r => r.Ping());
			imapReader.AssertWasCalled(r => r.ProcessMime(Arg<Mime>.Is.Anything));
			imapReader.AssertWasNotCalled(r => r.ProcessBrokenMessage(Arg<IMAP_FetchItem>.Is.Anything, Arg<IMAP_FetchItem[]>.Is.Anything, Arg<Exception>.Is.Anything));

			var existsMessages = ImapHelper.CheckImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			Assert.That(existsMessages.Count, Is.EqualTo(0), "Существуют письма в IMAP-папками с темами: {0}", existsMessages.Select(m => m.Envelope.Subject).Implode());
		}
		 
	}
}