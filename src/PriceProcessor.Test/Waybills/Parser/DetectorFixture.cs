using System.Collections.Generic;
using System.Data;
using System.Linq;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
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
		private WaybillFormatDetector detector;

		[SetUp]
		public void Setup()
		{
			detector = new WaybillFormatDetector();
		}

		[Test]
		public void Detect_xml_format()
		{
			var parser = detector.DetectParser(@"..\..\Data\Waybills\8041496-001.xml", null);
			Assert.That(parser, Is.InstanceOf<ProtekXmlParser>());
		}

		[Test]
		public void Parser_not_found()
		{
			try {
				detector.DetectParser(@"..\..\Data\Waybills\3677177_0_3677175_0_3676850_Сиа Интернейшнл(1064837).db", null);
			}
			catch (Exception) {
				return;
			}
			Assert.Fail("Не бросили исключение, хотя должны были");
		}

		[Test]
		public void Detect()
		{
			var parser = detector.DetectParser(@"..\..\Data\Waybills\5189569_ФораФарм_лоджик-Москва_506462_.dbf", null);
			Assert.That(parser, Is.InstanceOf<SiaParser>());

			var parser1 = detector.DetectParser(@"..\..\Data\Waybills\6854217_Катрен_23157_.txt", null);
			Assert.That(parser1, Is.InstanceOf<KatrenVrnParser>());

			var parser2 = detector.DetectParser(@"..\..\Data\Waybills\6155143_Катрен(1849).txt", null);
			Assert.That(parser2, Is.InstanceOf<KatrenVrnParser>());

			var parser3 = detector.DetectParser(@"..\..\Data\Waybills\6161231_Сиа Интернейшнл(Р2346542).DBF", null);
			Assert.That(parser3, Is.InstanceOf<FarmGroupParser>());

			var parser4 = detector.DetectParser(@"..\..\Data\Waybills\761517.dbf", null);
			Assert.That(parser4, Is.InstanceOf<FarmGroupParser>());

			var parser5 = detector.DetectParser(@"..\..\Data\Waybills\169976_21.dbf", null);
			Assert.That(parser5, Is.InstanceOf<GenesisNNParser>());
		}

		public class WaybillFormatDetectorFake : WaybillFormatDetector
		{
			public void AddSpecParser(uint firmCode, Type parserType)
			{
				if (!specParsers.ContainsKey(firmCode))
					specParsers.Add(firmCode, new List<Type> { parserType });
				else
					specParsers[firmCode].Add(parserType);
			}
		}

		public class ParserFake1 : IDocumentParser
		{
			public Document Parse(string file, Document document)
			{
				return null;
			}

			public static bool CheckFileFormat(DataTable data)
			{
				return true;
			}
		}

		public class ParserFake2 : IDocumentParser
		{
			public Document Parse(string file, Document document)
			{
				return null;
			}

			public static bool CheckFileFormat(object data)
			{
				return true;
			}
		}

		public class ParserFake3 : IDocumentParser
		{
			public Document Parse(string file, Document document)
			{
				return null;
			}
		}

		public class ParserFake4 : IDocumentParser
		{
			public Document Parse(string file, Document document)
			{
				return null;
			}

			public static bool CheckFileFormat(DataTable data)
			{
				return true;
			}

			public DataTable Load(string file)
			{
				return new DataTable();
			}
		}

		public class ParserFake5 : BaseDbfParser
		{
			public override Document Parse(string file, Document document)
			{
				return new Document();
			}

			public override DbfParser GetParser()
			{
				return new DbfParser();
			}

			public static bool CheckFileFormat(DataTable data)
			{
				return false;
			}
		}

		public class ParserFake6 : BaseDbfParser
		{
			public override Document Parse(string file, Document document)
			{
				return new Document();
			}

			public override DbfParser GetParser()
			{
				return new DbfParser();
			}

			public static bool CheckFileFormat(DataTable data)
			{
				return true;
			}
		}

		public class ParserFake7 : BaseDbfParser
		{
			public override Document Parse(string file, Document document)
			{
				return new Document();
			}

			public override DbfParser GetParser()
			{
				return new DbfParser();
			}

			public static bool CheckFileFormat(string data)
			{
				return true;
			}
		}

		[Test]
		public void GetSpecialParserTest()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 1 }, };
			var detector = new WaybillFormatDetectorFake();
			detector.AddSpecParser(documentLog.Supplier.Id, typeof(ParserFake1));

			var type = detector.GetSpecialParser(@"..\..\Data\Waybills\KZ000130.dbf", documentLog);
			Assert.That(type.FullName.Contains("ParserFake1"), Is.True);

			detector = new WaybillFormatDetectorFake();

			detector.AddSpecParser(documentLog.Supplier.Id, typeof(ParserFake2));
			type = detector.GetSpecialParser(@"..\..\Data\Waybills\KZ000130.dbf", documentLog);
			Assert.That(type, Is.Null);

			detector.AddSpecParser(documentLog.Supplier.Id, typeof(ParserFake1));
			type = detector.GetSpecialParser(@"..\..\Data\Waybills\KZ000130.txt", documentLog);
			Assert.That(type, Is.Null);

			detector = new WaybillFormatDetectorFake();
			detector.AddSpecParser(documentLog.Supplier.Id, typeof(ParserFake3));
			bool fail = false;
			try {
				detector.GetSpecialParser(@"..\..\Data\Waybills\KZ000130.dbf", documentLog);
			}
			catch (Exception e) {
				fail = e.Message.Contains("реализуй метод CheckFileFormat");
			}
			Assert.That(fail, Is.True);

			detector = new WaybillFormatDetectorFake();
			detector.AddSpecParser(documentLog.Supplier.Id, typeof(ParserFake2));
			detector.AddSpecParser(documentLog.Supplier.Id, typeof(ParserFake1));
			type = detector.GetSpecialParser(@"..\..\Data\Waybills\KZ000130.dbf", documentLog);
			Assert.That(type.FullName.Contains("ParserFake1"), Is.True);

			detector = new WaybillFormatDetectorFake();

			detector.AddSpecParser(documentLog.Supplier.Id, typeof(ParserFake5));
			detector.AddSpecParser(documentLog.Supplier.Id, typeof(ParserFake7));
			detector.AddSpecParser(documentLog.Supplier.Id, typeof(ParserFake6));
			type = detector.GetSpecialParser(@"..\..\Data\Waybills\KZ000130.dbf", documentLog);
			Assert.That(type.FullName.Contains("ParserFake6"), Is.True);
		}
	}
}