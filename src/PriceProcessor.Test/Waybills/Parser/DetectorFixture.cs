using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;
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

			var detector1 = new WaybillFormatDetector();
			var parser1 = detector1.DetectParser(@"..\..\Data\Waybills\6854217_Катрен_23157_.txt", null);
			Assert.That(parser1, Is.InstanceOf<KatrenVrnParser>());

			var detector2 = new WaybillFormatDetector();
			var parser2 = detector2.DetectParser(@"..\..\Data\Waybills\6155143_Катрен(1849).txt", null);
			Assert.That(parser2, Is.InstanceOf<KatrenVrnParser>());

			var detect = new WaybillFormatDetector();
			var parser3 = detect.DetectParser(@"..\..\Data\Waybills\6161231_Сиа Интернейшнл(Р2346542).DBF", null);
			Assert.That(parser3, Is.InstanceOf<FarmGroupParser>());

			var detect1 = new WaybillFormatDetector();
			var parser4 = detect1.DetectParser(@"..\..\Data\Waybills\761517.dbf", null);
			Assert.That(parser4, Is.InstanceOf<FarmGroupParser>());
			
			var detect2 = new WaybillFormatDetector();
			var parser5 = detect2.DetectParser(@"..\..\Data\Waybills\169976_21.dbf", null);
			Assert.That(parser5, Is.InstanceOf<GenesisNNParser>());
		}
	}
}
