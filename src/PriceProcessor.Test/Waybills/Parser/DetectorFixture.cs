using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
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
			var parser = detector.DetectParser(@"..\..\Data\Waybills\8041496-001.xml");
			Assert.That(parser, Is.InstanceOf<ProtekXmlParser>());
		}
	}
}
