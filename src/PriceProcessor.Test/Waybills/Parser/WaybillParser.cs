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
			var detector = new WaybillFormatDetector();
			var parser = detector.DetectParser(filePath);
			if (parser == null)
				return null;
			return parser.Parse(filePath, new Document());
		}
	}
}
