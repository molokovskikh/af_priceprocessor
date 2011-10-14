using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillFormatDetector
	{
		// словарь для специальных парсеров
		protected Dictionary<uint, IList<Type>> specParsers = new Dictionary<uint, IList<Type>>
		    {				
				{6256, new List<Type>{typeof(Avesta_6256_SpecialParser)}}, // Если это накладная в формате DBF от Авеста-Фармацевтика, обрабатываем ее специальным парсером
				{2747, new List<Type>{typeof(KazanFarmDbfParser)}}, //Накладная в формате dbf от Казань-Фарм.
				{7999, new List<Type>{typeof(TrediFarmCheboksarySpecialParser)}}, // Если накладная от Трэдифарм Чебоксары, обрабатываем ее специальным парсером
				{7957, new List<Type>{typeof(ZhdanovKazanSpecialParser)}}, // Накладная от ИП Жданов (Казань), обрабатываем специальным парсером.
				{8063, new List<Type>{typeof(ZhdanovKazanSpecialParser),
										typeof(BizonKazanSpecialParser)}}, // Накладная (dbf) от ООО "Бизон" (Казань)
				{74, new List<Type>{typeof(ImperiaFarmaSpecialParser), // Накладная от Империа-Фарма (dbf)
										typeof(ImperiaFarmaSpecialParser2)}}, // Накладная от Империа-Фарма (txt)
				{1581, new List<Type>{typeof(ZdravServiceSpecialParser),  // Накладная от Здравсервис, содержащая поля для счета-фактуры
										typeof(ZdravServiceSpecialParser2)}}, // Для поставщика Здравсервис (Тула) отдельный парсер (формат тот же, что и для PulsFKParser)
				{11427, new List<Type>{typeof(PokrevskySpecialParser)}}, // Накладная от ИП Покревский
				{7, new List<Type>{typeof(OriolaVoronezhSpecialParser)}}, // Накладная от Ориола (Воронеж)
				{182, new List<Type>{typeof(LekRusChernozemieSpecialParser)}}, // Накладная от Лекрус Центральное Черноземье
				//{4138, new List<Type>{typeof(KatrenVrnSpecialParser)}}, // Накладная от Катрен Воронеж, пока не используется
				{338, new List<Type>{typeof(Moron_338_SpecialParser)}}, // Накладная от Морон (Челябинск)
				{4001, new List<Type>{typeof(Moron_338_SpecialParser)}},
				{7146, new List<Type>{typeof(Moron_338_SpecialParser)}},
				{5802, new List<Type>{typeof(Moron_338_SpecialParser)}},
				{21, new List<Type>{typeof(Moron_338_SpecialParser)}},
				{4910, new List<Type>{typeof(FarmPartnerKalugaParser)}}, // Фармпартнер (Калуга)				
		    };
		                                             	

		public bool IsSpecialParser(IDocumentParser parser)
		{
			return specParsers.Select(item => item.Value).Any(parsers => parsers.Contains(parser.GetType()));
		}

		public Type GetSpecialParser(string file, DocumentReceiveLog documentLog)
		{
			if (documentLog == null) return null;
			var extention = Path.GetExtension(file.ToLower());
			var firmCode = documentLog.Supplier.Id;

			IList<Type> parsersTypes = specParsers.ContainsKey(firmCode) ? specParsers[firmCode] : null;
			if (parsersTypes == null) return null;

			foreach (var parserType in parsersTypes)
			{
				MethodInfo checkMethod = parserType.GetMethod("CheckFileFormat", BindingFlags.Static | BindingFlags.Public);
				if (checkMethod == null)
					throw new Exception(String.Format("У типа {0} нет метода для проверки формата, реализуй метод CheckFileFormat", parserType));

				var paramClass = checkMethod.GetParameters()[0].ParameterType.FullName;
				object[] args = null;
				bool check = false;
				if (extention == ".dbf" && paramClass.Contains("DataTable"))
				{
					MethodInfo loadMethod = parserType.GetMethod("Load", BindingFlags.Static | BindingFlags.Public);
					DataTable table;
					if (loadMethod != null)
					{						
						args = new[] { file };
						table = (DataTable)loadMethod.Invoke(null, args);						
					}
					else
					{
						table = Dbf.Load(file);
					}
					args = new[] { table };
					check = (bool)checkMethod.Invoke(null, args);
				}
				else if ((extention == ".sst" || extention == ".txt") && paramClass.Contains("String"))
				{
					args = new[] {file};
					check = (bool)checkMethod.Invoke(null, args);
				}
				if (check) return parserType;
			}
			return null;
		}

		public IDocumentParser DetectParser(string file, DocumentReceiveLog documentLog)
		{
			var extention = Path.GetExtension(file.ToLower());
			Type type = null;

			type = GetSpecialParser(file, documentLog);
			
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
				//сопоставление сертификатов для позиций накладной
				CertificateSourceDetector.DetectAndParse(doc);
			}
			return doc;
		}
	}
}