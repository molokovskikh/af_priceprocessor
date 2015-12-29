using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	internal class KatrenVrnSpecialParserFixture
	{
		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(ImperiaFarmaSpecialParser.CheckFileFormat(ImperiaFarmaSpecialParser.Load(@"..\..\Data\Waybills\KZ000130.dbf")));
		}
	}
}