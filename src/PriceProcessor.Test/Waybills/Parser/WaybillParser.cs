using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.Tools;
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

		public static Document Parse(string filePath, DocumentReceiveLog documentLog)
		{
			var detector = new WaybillFormatDetector();
			if (!File.Exists(filePath))
				filePath = Path.Combine(@"..\..\Data\Waybills\", filePath);
			CheckUniqueDbfParser(filePath);
			var parser = detector.DetectParser(filePath, documentLog);
			if (parser == null)
				return null;
			return parser.Parse(filePath, new Document());
		}

		private static void CheckUniqueDbfParser(string file)
		{
			if (Path.GetExtension(file.ToLower()) != ".dbf")
				return;
			var types = typeof(WaybillFormatDetector)
				.Assembly
				.GetTypes()
				.Where(t => t.Namespace.EndsWith("Waybills.Parser.DbfParsers") && t.IsPublic)
				.ToList();

			var count = 0;
			foreach (var type in types)
			{
				var detectFormat = type.GetMethod("CheckFileFormat", BindingFlags.Static | BindingFlags.Public);
				if (detectFormat == null)
					throw new Exception(String.Format("У типа {0} нет метода для проверки формата, реализуй метод CheckFileFormat", type));
				var data = Dbf.Load(file);
				var result = (bool)detectFormat.Invoke(null, new object[] { data });
				if (result)
					count++;
			}
			if (count != 1)
				throw new Exception("Для разбора данного формата подходит более одного парсера");
		}

		public static IDocumentParser GetParserType(string filePath, DocumentReceiveLog documentLog)
		{
			var detector = new WaybillFormatDetector();
			return detector.DetectParser(filePath, documentLog);
		}
	}
}
