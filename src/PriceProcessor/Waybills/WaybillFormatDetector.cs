using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Tools;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NHibernate.Criterion;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillFormatDetector
	{
		// словарь для специальных парсеров
		protected Dictionary<uint, IList<Type>> specParsers = new Dictionary<uint, IList<Type>> {
			{ 6256, new List<Type> { typeof(Avesta_6256_SpecialParser) } }, // Если это накладная в формате DBF от Авеста-Фармацевтика, обрабатываем ее специальным парсером
			{ 2747, new List<Type> { typeof(KazanFarmDbfParser) } }, //Накладная в формате dbf от Казань-Фарм.
			{ 257, new List<Type> { typeof(MedServiceParser) } }, //Накладная в формате dbf от МедСервис

			{
				8063, new List<Type> {
					typeof(BizonKazanSpecialParser)
				}
			}, // Накладная (dbf) от ООО "Бизон" (Казань)
			{
				74, new List<Type> {
					typeof(ImperiaFarmaSpecialParser), // Накладная от Империа-Фарма (dbf)
					typeof(ImperiaFarmaSpecialParser2)
				}
			}, // Накладная от Империа-Фарма (txt)
			{
				1581, new List<Type> {
					typeof(ZdravServiceSpecialParser), // Накладная от Здравсервис, содержащая поля для счета-фактуры
					typeof(ZdravServiceSpecialParser2)
				}
			}, // Для поставщика Здравсервис (Тула) отдельный парсер (формат тот же, что и для PulsFKParser)
			{ 11427, new List<Type> { typeof(PokrevskySpecialParser) } }, // Накладная от ИП Покревский
			{ 7, new List<Type> { typeof(OriolaVoronezhSpecialParser) } }, // Накладная от Ориола (Воронеж)
			{ 182, new List<Type> { typeof(LekRusChernozemieSpecialParser) } }, // Накладная от Лекрус Центральное Черноземье
			{ 338, new List<Type> { typeof(Moron_338_SpecialParser) } }, // Накладная от Морон (Челябинск)
			{ 4001, new List<Type> { typeof(Moron_338_SpecialParser) } },
			{ 7146, new List<Type> { typeof(Moron_338_SpecialParser) } },
			{ 5802, new List<Type> { typeof(Moron_338_SpecialParser) } },
			{ 21, new List<Type> { typeof(Moron_338_SpecialParser) } },
			{ 4910, new List<Type> { typeof(FarmPartnerKalugaParser) } }, // Фармпартнер (Калуга)
			{ 7949, new List<Type> { typeof(MarimedsnabSpecialParser) } }, // Маримедснаб (Йошкар-Ола)
			{ 2754, new List<Type> { typeof(KatrenKazanSpecialParser) } }, // Катрен (Казань)
			{ 2109, new List<Type> { typeof(BelaLtdParser) } }, // БЕЛА ЛТД, Код 2109
			{ 7524, new List<Type> { typeof(KronikaLtdParser) } }, //Кроника-Фарм, Код 7524
		};

		public Type GetSpecialParser(string file, DocumentReceiveLog documentLog)
		{
			if (documentLog == null)
				return null;
			var extention = Path.GetExtension(file.ToLower());
			var firmCode = documentLog.Supplier.Id;

			var parsersTypes = specParsers.ContainsKey(firmCode) ? specParsers[firmCode] : null;
			if (parsersTypes == null)
				return null;

			foreach (var parserType in parsersTypes) {
				var checkMethod = parserType.GetMethod("CheckFileFormat", BindingFlags.Static | BindingFlags.Public);
				if (checkMethod == null)
					throw new Exception(String.Format("У типа {0} нет метода для проверки формата, реализуй метод CheckFileFormat", parserType));

				var paramClass = checkMethod.GetParameters()[0].ParameterType.FullName;
				object[] args;
				var check = false;
				if (extention == ".dbf" && paramClass.Contains("DataTable")) {
					var loadMethod = parserType.GetMethod("Load", BindingFlags.Static | BindingFlags.Public);
					DataTable table;
					if (loadMethod != null) {
						args = new[] { file };
						table = (DataTable)loadMethod.Invoke(null, args);
					}
					else {
						table = Dbf.Load(file);
					}
					args = new[] { table };
					check = (bool)checkMethod.Invoke(null, args);
				}
				else if ((extention == ".sst" || extention == ".txt") && paramClass.Contains("String")) {
					args = new[] { file };
					check = (bool)checkMethod.Invoke(null, args);
				}
				if (check)
					return parserType;
			}
			return null;
		}

		public virtual IDocumentParser DetectParser(string file, DocumentReceiveLog documentLog)
		{
			var type = GetSuitableParsers(file, documentLog).FirstOrDefault();
			if (type == null) {
				log4net.LogManager.GetLogger("InfoLog").InfoFormat("Не удалось определить тип парсера накладной. Файл {0}", file);
#if !DEBUG
				return null;
#else
				throw new Exception("Не удалось определить тип парсера");
#endif
			}

			var constructor = type.GetConstructors().FirstOrDefault(c => c.GetParameters().Count() == 0);
			if (constructor == null)
				throw new Exception("У типа {0} нет конструктора без аргументов");
			return (IDocumentParser)constructor.Invoke(new object[0]);
		}

		public IEnumerable<Type> GetSuitableParsers(string file, DocumentReceiveLog documentLog)
		{
			var extention = Path.GetExtension(file.ToLower());

			var type = GetSpecialParser(file, documentLog);
			if (type != null)
				return new[] { type };

			if (extention == ".dbf") {
				return GetSuitableParsers(file, "Dbf");
			}
			else if (extention == ".sst") {
				return GetSuitableParsers(file, "Sst");
			}
			else if (extention == ".xls") {
				return GetSuitableParsers(file, "Xls");
			}
			else if ((extention == ".xml") || (extention == ".data")) {
				return GetSuitableParsers(file, "Xml");
			}
			else if (extention == ".pd") {
				return GetSuitableParsers(file, "Pd");
			}
			else if (extention == ".txt") {
				return GetSuitableParsers(file, "Txt");
			}
			return Enumerable.Empty<Type>();
		}

		private IEnumerable<Type> GetSuitableParsers(string file, string group)
		{
			var @namespace = String.Format("Waybills.Parser.{0}Parsers", group);
			var types = typeof(WaybillFormatDetector)
				.Assembly
				.GetTypes()
				.Where(t => t.Namespace != null
					&& t.Namespace.EndsWith(@namespace)
					&& t.IsPublic
					&& !t.IsAbstract
					&& typeof(IDocumentParser).IsAssignableFrom(t))
				.Except(specParsers.SelectMany(item => item.Value))
				.ToArray();

			types = types.OrderBy(t => t.Name).ToArray();
			object[] args;
			if (group == "Dbf") {
				var data = Dbf.Load(file);
				args = new[] { data };
			}
			else {
				args = new[] { file };
			}

			var normal = types.Where(t => !typeof(BaseDbfParser2).IsAssignableFrom(t));
			var smart = types.Where(t => typeof(BaseDbfParser2).IsAssignableFrom(t));

			var found = false;
			foreach (var type in normal) {
				var detectFormat = type.GetMethod("CheckFileFormat", BindingFlags.Static | BindingFlags.Public);
				if (detectFormat == null)
					throw new Exception(String.Format("У типа {0} нет метода для проверки формата, реализуй метод CheckFileFormat", type));

				var result = (bool)detectFormat.Invoke(null, args);
				if (result) {
					found = true;
					yield return type;
				}
			}

			if (!found) {
				var maxHipoints = 0;
				Type leader = null;
				foreach (var type in smart) {
					var parser = (BaseDbfParser2)Activator.CreateInstance(type);
					var hitpoints = parser.CalculateHitPoints((DataTable)args[0]);
					if (hitpoints > maxHipoints) {
						maxHipoints = hitpoints;
						leader = type;
					}
				}
				if (leader != null)
					yield return leader;
			}
		}

		public Document DetectAndParse(string file, DocumentReceiveLog log)
		{
			var parser = DetectParser(file, log);
			if (parser == null)
				return null;
			var document = new Document(log, parser.GetType().Name);
			var doc = parser.Parse(file, document);
			if (doc == null)
				return null;

			var orders = doc.Lines.Where(l => l.OrderId != null)
				.Select(l => OrderHead.TryFind(l.OrderId.Value))
				.Where(o => o != null)
				.Distinct()
				.ToList();
			return ProcessDocument(doc, orders);
		}

		public static Document ProcessDocument(Document doc, IList<OrderHead> orders)
		{
			if (doc == null)
				return null;

			//сопоставляем идентификаторы названиям продуктов в накладной
			doc.SetProductId();
			//расчет недостающих значений
			doc.CalculateValues();
			if (!doc.DocumentDate.HasValue)
				doc.DocumentDate = DateTime.Now;

			//сопоставляем позиции в накладной с позициями в заказе
			var totalQuantity = doc.Lines.Sum(l => l.Quantity);
			var lineCount = doc.Lines.Count;
			var isDuplicate = SessionHelper.WithSession(s => {
				return s.CreateCriteria<Document>("d")
					.Add(Expression.Eq("d.ProviderDocumentId", doc.ProviderDocumentId))
					.Add(Expression.Eq("d.FirmCode", doc.FirmCode))
					.Add(Expression.Eq("d.DocumentDate", doc.DocumentDate))
					.Add(Subqueries.Eq(totalQuantity,
						DetachedCriteria.For<DocumentLine>("l")
							.Add(Expression.EqProperty("d.Id", "l.Document.Id"))
							.SetProjection(Projections.Sum("Quantity"))))
					.Add(Subqueries.Eq(lineCount,
						DetachedCriteria.For<DocumentLine>("l")
							.Add(Expression.EqProperty("d.Id", "l.Document.Id"))
							.SetProjection(Projections.Count("l.Id"))))
					.SetProjection(Projections.Count("d.Id"))
					.UniqueResult<int>() > 0;
			});
			if (!isDuplicate)
				WaybillOrderMatcher.SafeComparisonWithOrders(doc, orders.SelectMany(o => o.Items).ToList());

			//сопоставление сертификатов для позиций накладной
			CertificateSourceDetector.DetectAndParse(doc);

			return doc;
		}
	}
}