using System.Collections.Generic;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using LumiSoft.Net.Mime;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class MailContextFixture : IntegrationFixture
	{
		[Test]
		public void Accept_html_extension_for_vip_sender()
		{
			var context = new MailContext {
				Suppliers = new List<Supplier>()
			};
			TemplateHolder.Values.AllowedMiniMailExtensions = "doc, git, xls";
			context.Suppliers.Add(new Supplier { Payer = 921 });
			Assert.That(context.IsValidExtension(".html"), Is.True);
		}

		[Test(Description = "при проверке письма должно возникнуть исключение FromParseException")]
		public void FromAddressParseProblem()
		{
			var message = Mime.Parse(@"..\..\Data\MailContextFixture\Unparse_Protek.eml");
			var fromList = MimeEntityExtentions.GetAddressList(message);
			Assert.That(fromList.Count, Is.EqualTo(0), "Список разборанных адресов отправителя должен быть пустым");

			var context = new MailContext();

			var exception = Assert.Throws<FromParseException>(() => context.ParseMime(message, fromList));

			Assert.That(exception.Message, Is.StringStarting("Не смогли разобрать список отправителей письма для сопоставления с поставщиками"));
		}

		[Test(Description = "Тест проверяет, что находится отправитель, если контакт задан у персоны")]
		public void Person_contact_from_supplier()
		{
			var message = Mime.Parse(@"..\..\Data\MailContextFixture\Unparse_Protek.eml");
			var context = new MailContext();
			var name = Generator.Name();
			var email = name + "@test.ru";
			var fromList = new AddressList();
			fromList.Add(new MailboxAddress(email));

			var supplier = TestSupplier.CreateNaked(session);
			supplier.Name = name;
			supplier.Save();
			var group = supplier.ContactGroupOwner.AddContactGroup(ContactGroupType.MiniMails);
			session.Save(group);
			group.AddPerson("Tестовая персона");
			session.Save(group.Persons[0]);
			var contact = group.Persons[0].AddContact(ContactType.Email, email);
			session.Save(contact);

			FlushAndCommit();
			context.ParseMime(message, fromList);

			Assert.AreEqual(context.Suppliers.Count, 1);
			Assert.AreEqual(context.Suppliers[0].Name, name);
		}
	}
}