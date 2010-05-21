using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillFormatDetector
	{
		public IDocumentParser DetectParser(string file, DocumentLog documentLog)
		{
			var extention = Path.GetExtension(file.ToLower());
			Type type = null;
			if (extention == ".dbf")
				type = DetectDbfParser(file);
			else if (extention == ".sst")
				type = typeof (UkonParser);
			else if ((extention == ".xml") || (extention == ".data"))
			{
				if (new SiaXmlParser().IsInCorrectFormat(file))
					type = typeof (SiaXmlParser);
				else if (new ProtekXmlParser().IsInCorrectFormat(file))
					type = typeof (ProtekXmlParser);
			}
			else if (extention == ".pd")
				type = typeof (ProtekParser);
			else if (extention == ".txt")
			{
				if (KatrenOrelTxtParser.CheckFileFormat(file))
					type = typeof (KatrenOrelTxtParser);
			}

			// Если поставщик - это челябинский Морон, для него отдельный парсер 
			// (вообще-то формат тот же что и у SiaParser, но в колонке PRICE цена БЕЗ Ндс)
			if ((documentLog != null) && Moron_338_SpecialParser.CheckFileFormat(file) &&
				(documentLog.Supplier.Id == 338 || documentLog.Supplier.Id == 4001 || documentLog.Supplier.Id == 7146))
				type = typeof (Moron_338_SpecialParser);

			//Юкон посылает в формате как сиа но в кодировке 1251, пидарасы
			if (documentLog != null 
				&& documentLog.Supplier.Id == 105 
				&& type == typeof(SiaParser))
				type = typeof (UkonDbfParser);

			if (type == null)
				throw new Exception("Не удалось определить тип парсера");

			var constructor = type.GetConstructors().Where(c => c.GetParameters().Count() == 0).FirstOrDefault();
			if (constructor == null)
				throw new Exception("У типа {0} нет конструктора без аргументов");
			return (IDocumentParser)constructor.Invoke(new object[0]);
		}

		private static Type DetectDbfParser(string file)
		{
			var types = typeof (WaybillFormatDetector)
				.Assembly
				.GetTypes()
				.Where(t => t.Namespace.EndsWith("Waybills.Parser.DbfParsers") && t.IsPublic)
				.ToList();

			foreach (var type in types)
			{
				var detectFormat = type.GetMethod("CheckFileFormat", BindingFlags.Static | BindingFlags.Public);
				if (detectFormat == null)
					throw new Exception(String.Format("У типа {0} нет метода для проверки формата, реализуй метод CheckFileFormat", type));
				var data = Dbf.Load(file);
				var result = (bool)detectFormat.Invoke(null, new object[] {data});
				if (result)
					return type;
			}
			return null;
		}
	}
}