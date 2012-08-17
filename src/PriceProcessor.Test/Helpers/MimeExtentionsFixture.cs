using Inforoom.PriceProcessor.Helpers;
using LumiSoft.Net.Mime;
using NUnit.Framework;

namespace PriceProcessor.Test.Helpers
{
	[TestFixture]
	public class MimeExtentionsFixture
	{
		[Test]
		public void Parse_x_real_to()
		{
			var mime = Mime.Parse(@"..\..\Data\MimeExtentionsFixture\Исходное письмо.eml");
			var recipients = mime.GetRecipients();
			Assert.That(recipients, Is.EquivalentTo(new[] { "5143@waybills.analit.net" }));
		}
	}
}