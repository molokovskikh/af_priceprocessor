using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;

namespace PriceProcessor.Test.Waybills.Parser
{
	// Класс для тестов, чтобы не повторять ошибок вроде "забыл добавить новый парсер в DetectParser()"
	public class WaybillParser
	{
		public static Document Parse(string filePath)
		{
			return Parse(filePath, null);
		}

		public static Document Parse(string filePath, DocumentLog documentLog)
		{
			var detector = new WaybillFormatDetector();
			var parser = detector.DetectParser(filePath, documentLog);
			if (parser == null)
				return null;
			return parser.Parse(filePath, new Document());
		}

		public static IDocumentParser GetParserType(string filePath, DocumentLog documentLog)
		{
			var detector = new WaybillFormatDetector();
			return detector.DetectParser(filePath, documentLog);
		}
	}
}
