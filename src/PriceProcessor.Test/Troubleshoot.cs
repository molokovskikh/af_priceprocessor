using LumiSoft.Net.Mime;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("Тест что бы разбирать проблемные ситуации")]
	public class Troubleshoot
	{
		[Test]
		public void shoot_it()
		{
			var mime = Mime.Parse(@"C:\Unparse.eml");
		}
	}
}
