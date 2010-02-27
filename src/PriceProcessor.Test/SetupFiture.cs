using NUnit.Framework;

namespace PriceProcessor.Test
{
	[SetUpFixture]
	public class SetupFiture
	{
		[SetUp]
		public void Setup()
		{
			global::Test.Support.Setup.Initialize("DB");
		}
	}
}