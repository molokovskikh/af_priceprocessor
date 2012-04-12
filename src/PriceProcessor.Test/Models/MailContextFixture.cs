using System.Collections.Generic;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
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
			context.Suppliers.Add(new Supplier {Payer = 921});
			Assert.That(context.IsValidExtension(".html"), Is.True);
		}
	}
}