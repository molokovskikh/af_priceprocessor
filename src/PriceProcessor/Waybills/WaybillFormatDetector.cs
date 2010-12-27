using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Tools;
using ExcelLibrary.SpreadSheet;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillFormatDetector
	{
		public IDocumentParser DetectParser(string file, DocumentReceiveLog documentLog)
		{
			var extention = Path.GetExtension(file.ToLower());
			Type type = null;

			// Если это накладная в формате DBF от Авеста-Фармацевтика,
			// обрабатываем ее специальным парсером
			if ((documentLog != null) &&
				(documentLog.Supplier.Id == 6256) &&
				(extention == ".dbf"))
			{
				var table = Avesta_6256_SpecialParser.Load(file);
				if (Avesta_6256_SpecialParser.CheckFileFormat(table))
					type = typeof(Avesta_6256_SpecialParser);
			}

			if (type == null)
			{
				if (extention == ".dbf")
					type = DetectDbfParser(file);
				else if (extention == ".sst")
					type = typeof (UkonParser);
				else if (extention == ".xls")
					type = DetectXlsParser(file);
				else if ((extention == ".xml") || (extention == ".data"))
					type = DetectXmlParser(file);
				else if (extention == ".pd")
					type = typeof (ProtekParser);
				else if (extention == ".txt")
					type = DetectTxtParser(file);

				// Если поставщик - это челябинский Морон, для него отдельный парсер 
				// (вообще-то формат тот же что и у SiaParser, но в колонке PRICE цена БЕЗ Ндс)
				if ((documentLog != null) && Moron_338_SpecialParser.CheckFileFormat(file) &&
				    (documentLog.Supplier.Id == 338 || documentLog.Supplier.Id == 4001
				     || documentLog.Supplier.Id == 7146 || documentLog.Supplier.Id == 5802
				     || documentLog.Supplier.Id == 21))
					type = typeof (Moron_338_SpecialParser);

				if (type == typeof(PulsFKParser) && documentLog != null
					&& documentLog.Supplier.Id == 1581)
					type = typeof (ZdravServiceParser);
			}
			if (type == null)
			{
				log4net.LogManager.GetLogger(typeof(WaybillService)).WarnFormat("Не удалось определить тип парсера накладной. Файл {0}", file);
#if !DEBUG
				return null;
#else
				throw new Exception("Не удалось определить тип парсера");
#endif
			}

			var constructor = type.GetConstructors().Where(c => c.GetParameters().Count() == 0).FirstOrDefault();
			if (constructor == null)
				throw new Exception("У типа {0} нет конструктора без аргументов");
			return (IDocumentParser)constructor.Invoke(new object[0]);
		}

		private static Type DetectDbfParser(string file)
		{
			return DetectParser(file, "Dbf");
		}

		private static Type DetectTxtParser(string file)
		{
			return DetectParser(file, "Txt");
		}

		private static Type DetectXmlParser(string file)
		{
			return DetectParser(file, "Xml");
		}

		private static Type DetectParser(string file, string group)
		{
			var @namespace = String.Format("Waybills.Parser.{0}Parsers", group);
			var types = typeof (WaybillFormatDetector)
				.Assembly
				.GetTypes()
				.Where(t => t.Namespace.EndsWith(@namespace) 
					&& t.IsPublic
					&& !t.IsAbstract
					&& typeof(IDocumentParser).IsAssignableFrom(t))
				.ToList();

			foreach (var type in types)
			{
				var detectFormat = type.GetMethod("CheckFileFormat", BindingFlags.Static | BindingFlags.Public);
				if (detectFormat == null)
					throw new Exception(String.Format("У типа {0} нет метода для проверки формата, реализуй метод CheckFileFormat", type));
				object[] args;
				if (group == "Dbf")
				{
					var data = Dbf.Load(file);
					args = new [] {data};
				}
				else
				{
					args = new[] {file};
				}
				var result = (bool)detectFormat.Invoke(null, args);
				if (result)
					return type;
			}
			return null;
		}

		private static Type DetectXlsParser(string file)
		{
			if (BssSpbXlsParser.CheckFileFormat(file))
				return typeof (BssSpbXlsParser);
			if (Protek9Parser.CheckFileFormat(file))
				return typeof(Protek9Parser);
			if (OACXlsParser.CheckFileFormat(file))
				return typeof (OACXlsParser);
			return null;
		}

		public Document DetectAndParse(DocumentReceiveLog log, string file)
		{
			var parser = DetectParser(file, log);
			if (parser == null)
				return null;
			var document = new Document(log);
			document.Parser = parser.GetType().Name;
			return parser.Parse(file, document);
		}
	}
}