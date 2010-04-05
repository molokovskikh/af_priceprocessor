using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class UkonDbfParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = Parse("3657985_Юкон(0010235).dbf");
			Assert.That(document.Lines[0].Product, Is.EqualTo("Грандаксин табл.50 мг №60"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
		}

		private Document Parse(string file)
		{
			file = @"..\..\Data\Waybills\" + file;
			var detector = new WaybillFormatDetector();
			var parser = detector.DetectParser(file);
			var doc = new Document();
			return parser.Parse(file, doc);
		}
	}
}
