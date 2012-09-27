using System.Collections.Generic;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using LumiSoft.Net.Mime;
using NUnit.Framework;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class MailContextFixture
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
	}
}