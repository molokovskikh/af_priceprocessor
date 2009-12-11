using NUnit.Framework;

namespace PriceProcessor.Test.Handlers
{
	[SetUpFixture]
	public class HandlersSetup
	{
		[SetUp]
		public void Setup()
		{
			
			global::Test.Support.Setup.Initialize("DB");
		}
	}
}
