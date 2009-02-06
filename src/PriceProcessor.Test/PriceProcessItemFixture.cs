using Inforoom.PriceProcessor;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceProcessItemFixture
	{
		[Test]
		public void IsSynonymEqualTest()
		{
			Assert.That(new PriceProcessItem(false, 1, 1, 1, "", null)
			            	.IsSynonymEqual(new PriceProcessItem(false, 2, 2, 1, "", 1)), Is.True);

			Assert.That(new PriceProcessItem(false, 1, 1, 1, "", 4)
			            	.IsSynonymEqual(new PriceProcessItem(false, 2, 2, 2, "", 4)), Is.True);

			Assert.That(new PriceProcessItem(false, 1, 1, 1, "", 2)
			            	.IsSynonymEqual(new PriceProcessItem(false, 2, 2, 2, "", null)), Is.True);

			Assert.That(new PriceProcessItem(false, 1, 1, 1, "", null)
			            	.IsSynonymEqual(new PriceProcessItem(false, 2, 2, 2, "", null)), Is.False);
		}
	}
}
