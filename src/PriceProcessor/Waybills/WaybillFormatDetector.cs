using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;
using log4net;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillFormatDetector
	{
		public IDocumentParser DetectParser(string file, DocumentReceiveLog documentLog)
		{
			var extention = Path.GetExtension(file.ToLower());
			Type type = null;

			if ((documentLog != null) && (extention == ".dbf"))
			{
				switch (documentLog.Supplier.Id)
				{
					// Если это накладная в формате DBF от Авеста-Фармацевтика,
					// обрабатываем ее специальным парсером
					case 6256:
						{
							var table = Avesta_6256_SpecialParser.Load(file);
							if (Avesta_6256_SpecialParser.CheckFileFormat(table))
								type = typeof(Avesta_6256_SpecialParser);
							break;
						}
					//Накладная в формате dbf от Казань-Фарм.
					case 2747:
						{
							var table = KazanFarmDbfParser.Load(file);
							if (KazanFarmDbfParser.CheckFileFormat(table))
								type = typeof (KazanFarmDbfParser);
							break;
						}
					// Если накладная от Трэдифарм Чебоксары, обрабатываем ее специальным парсером
					case 7999:
						{
							var table = TrediFarmCheboksarySpecialParser.Load(file);
							if (TrediFarmCheboksarySpecialParser.CheckFileFormat(table))
								type = typeof(TrediFarmCheboksarySpecialParser);
							break;
						}
					case 7957: // Накладная от ИП Жданов (Казань), обрабатываем специальным парсером.
					case 8063: // Накладная (dbf) от ООО "Бизон" (Казань)
						{
							var table = ZhdanovKazanSpecialParser.Load(file);
							if (ZhdanovKazanSpecialParser.CheckFileFormat(table))
								type = typeof(ZhdanovKazanSpecialParser);
							break;
						}
					case 74:
						{
							var table = ImperiaFarmaSpecialParser.Load(file);
							if (ImperiaFarmaSpecialParser.CheckFileFormat(table))
								type = typeof(ImperiaFarmaSpecialParser);
							break;
						}
                    case 1581: // Накладная от Здравсервис, содержащая поля для счета-фактуры
				        {
				            var table = ZdravServiceSpecialParser.Load(file);
                            if (ZdravServiceSpecialParser.CheckFileFormat(table))
                                type = typeof (ZdravServiceSpecialParser);
				            break;
				        }
                    
					default: break;
				}
			}

            if ((documentLog != null) && (extention == ".sst"))
            {
                switch (documentLog.Supplier.Id)
                {
                    case 4910: // Фармпартнер (Калуга)
                        {
                            if (FarmPartnerKalugaParser.CheckFileFormat(file))
                                type = typeof(FarmPartnerKalugaParser);
                            break;
                        }
                    default: break;
                }
            }

            if ((documentLog != null) && (extention == ".txt"))
            {
                switch (documentLog.Supplier.Id)
                {
                    case 8063: // Накладная (txt) от  "Бизон" (Казань)
                        {
                            if (BizonKazanSpecialParser.CheckFileFormat(file))
                                type = typeof (BizonKazanSpecialParser);
                            break;
                        }
                    default: break;
                }
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
                    type = typeof(ProtekParser);
                else if (extention == ".txt")
                    type = DetectTxtParser(file);

				// Если поставщик - это челябинский Морон, для него отдельный парсер 
				// (вообще-то формат тот же что и у SiaParser, но в колонке PRICE цена БЕЗ Ндс)
				if ((documentLog != null) && Moron_338_SpecialParser.CheckFileFormat(file) &&
					(documentLog.Supplier.Id == 338 || documentLog.Supplier.Id == 4001
					 || documentLog.Supplier.Id == 7146 || documentLog.Supplier.Id == 5802
					 || documentLog.Supplier.Id == 21))
					type = typeof (Moron_338_SpecialParser);

				// Для поставщика Здравсервис (Тула) отдельный парсер (формат тот же, что и для PulsFKParser)
                if (type == typeof(PulsFKParser) && documentLog != null
                    && documentLog.Supplier.Id == 1581)
                {                  
                    type = typeof(ZdravServiceParser);                 
                }
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

		private static Type DetectXlsParser(string file)
		{
			return DetectParser(file, "Xls");
		}

		private static Type DetectParser(string file, string group)
		{
			return GetSuitableParsers(file, group).FirstOrDefault();
		}

		public static IEnumerable<Type> GetSuitableParsers(string file, string group)
		{
			var @namespace = String.Format("Waybills.Parser.{0}Parsers", group);
			var types = typeof(WaybillFormatDetector)
				.Assembly
				.GetTypes()
				.Where(t => t.Namespace.EndsWith(@namespace)
					&& t.IsPublic
					&& !t.IsAbstract
					&& typeof(IDocumentParser).IsAssignableFrom(t))
				.ToList();

			types = types.OrderBy(t => t.Name).ToList();
			
			object[] args;
			if (group == "Dbf")
			{
				var data = Dbf.Load(file);
				args = new[] { data };
			}
			else
			{
				args = new[] { file };
			}

			foreach (var type in types)
			{
				var detectFormat = type.GetMethod("CheckFileFormat", BindingFlags.Static | BindingFlags.Public);
				if (detectFormat == null)
					throw new Exception(String.Format("У типа {0} нет метода для проверки формата, реализуй метод CheckFileFormat", type));
				
				var result = (bool)detectFormat.Invoke(null, args);
				if (result)
					yield return type;
			}
			yield break;
		}
			
		public Document DetectAndParse(DocumentReceiveLog log, string file)
		{
			var parser = DetectParser(file, log);
			if (parser == null)
				return null;
			var document = new Document(log);
			document.Parser = parser.GetType().Name;
			var doc = parser.Parse(file, document);
            if (doc != null)
            {
				doc.SetProductId(); // сопоставляем идентификаторы названиям продуктов в накладной
				doc.CalculateValues(); // расчет недостающих значений 
                if (!doc.DocumentDate.HasValue) doc.DocumentDate = DateTime.Now;                				
            }
		    return doc;
		}
	}
}