using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using Inforoom.PriceProcessor.Waybills.Parser.XmlParsers;
using NUnit.Framework;
using System;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class DetectorFixture
	{
		[Test]
		public void Detect_xml_format()
		{
			var detector = new WaybillFormatDetector();
			var parser = detector.DetectParser(@"..\..\Data\Waybills\8041496-001.xml", null);
			Assert.That(parser, Is.InstanceOf<ProtekXmlParser>());
		}

		[Test]
		public static void Parser_not_found()
		{
			try
			{
				var detector = new WaybillFormatDetector();
				detector.DetectParser(@"..\..\Data\Waybills\3677177_0_3677175_0_3676850_Сиа Интернейшнл(1064837).db", null);
			}
			catch(Exception) { return; }
			Assert.Fail("Не бросили исключение, хотя должны были");
		}

		[Test]
		public void detect()
		{
			var detector = new WaybillFormatDetector();
			var parser = detector.DetectParser(@"..\..\Data\Waybills\5189569_ФораФарм_лоджик-Москва_506462_.dbf", null);
			Assert.That(parser, Is.InstanceOf<SiaParser>());
		}
	}
}
