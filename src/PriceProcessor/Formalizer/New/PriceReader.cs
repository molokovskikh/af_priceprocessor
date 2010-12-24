using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Common.Tools;
using System.Linq;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Properties;
using log4net;
using MySql.Data.MySqlClient;
using Enumerable = System.Linq.Enumerable;

namespace Inforoom.PriceProcessor.Formalizer.New
{
	public interface IReader
	{
		IEnumerable<FormalizationPosition> Read();
		List<CostDescription> CostDescriptions { get; set; }
		void SendWaring(PriceLoggingStat stat);
	}

	public class PriceReader : IReader
	{
		private DataTable _priceData;
		private uint _priceItemId;
		private int _index = -1;

		private string junkPos;
		private string awaitPos;
		private string vitallyImportantMask;
		private string buyingMatrix;

		private string[] fieldNames;
		private ToughDate toughDate;
		private ToughMask toughMask;
		private string[] forbWordsList;

		private IParser _parser;
		private string _filename;
		private PriceFormalizationInfo _priceInfo;

		public List<CostDescription> CostDescriptions { get; set; }

		private readonly ILog _logger = LogManager.GetLogger(typeof (PriceReader));

		public PriceReader(DataRow priceInfo, IParser parser, string filename, PriceFormalizationInfo info)
		{
			_logger.DebugFormat("Создали класс для обработки файла {0}", filename);

			_filename = filename;
			_parser = parser;

			_priceItemId = Convert.ToUInt32(priceInfo[FormRules.colPriceItemId]);
			_priceInfo = info;

			fieldNames = new string[Enum.GetNames(typeof(PriceFields)).Length];
			var nameMask = priceInfo[FormRules.colNameMask] is DBNull ? String.Empty : (string)priceInfo[FormRules.colNameMask];

			awaitPos = priceInfo[FormRules.colSelfAwaitPos].ToString();
			junkPos  = priceInfo[FormRules.colSelfJunkPos].ToString();
			vitallyImportantMask = priceInfo[FormRules.colSelfVitallyImportantMask].ToString();
			buyingMatrix = priceInfo[FormRules.colBuyingMatrix].ToString();
			toughDate = new ToughDate();
			if (String.Empty != nameMask)
				toughMask = new ToughMask(nameMask, info);

			//Производим попытку разобрать строку с "запрещенными выражениями"
			var forbWords = priceInfo[FormRules.colForbWords] is DBNull ? String.Empty : (string)priceInfo[FormRules.colForbWords];
			forbWords = forbWords.Trim();
			if (String.Empty != forbWords)
			{
				forbWordsList = forbWords.Split(new[] {' '}).Where(w => !String.IsNullOrWhiteSpace(w)).ToArray();
				if (forbWordsList.Length == 0)
					forbWordsList = null;
			}

			foreach(PriceFields pf in Enum.GetValues(typeof(PriceFields)))
			{
				var tmpName = (PriceFields.OriginalName == pf) ? "FName1" : "F" + pf;
				SetFieldName(pf, priceInfo[tmpName] is DBNull ? String.Empty : (string)priceInfo[tmpName]);
			}
		}

		public string GetFieldName(PriceFields PF)
		{
			return fieldNames[(int)PF];
		}

		protected static string GetDescription(PriceFields value)
		{
			var descriptions = value.GetType().GetField(value.ToString()).GetCustomAttributes(false);
			return ((DescriptionAttribute)descriptions[0]).Description;
		}


		public void SendWaring(PriceLoggingStat stat)
		{
			ProcessUndefinedCost(stat);
			ProcessZeroCost(stat);
			CheckColumnPresents();
		}

		/// <summary>
		/// анализируем цены и формируем список, если ценовая колонка имеет более 5% позиций с неустановленной ценой
		/// </summary>
		private void ProcessUndefinedCost(PriceLoggingStat stat)
		{
			var stringBuilder = new StringBuilder();
			foreach (var cost in CostDescriptions)
				if (cost.UndefinedCostCount > stat.formCount * 0.05)
					stringBuilder.AppendFormat("ценовая колонка \"{0}\" имеет {1} позиций с незаполненной ценой\n", cost.Name, cost.UndefinedCostCount);

			if (stringBuilder.Length > 0)
				SendAlertToUserFail(
					stringBuilder,
					"PriceProcessor: В прайс-листе {0} поставщика {1} имеются позиции с незаполненными ценами",
					@"
Здравствуйте!
  В прайс-листе {0} поставщика {1} имеются позиции с незаполненными ценами.
  Список ценовых колонок:
{2}

С уважением,
  PriceProcessor.");

		}

		/// <summary>
		/// анализируем цены и формируем сообщение, если ценовая колонка имеет все позиции установленными в 0
		/// </summary>
		private void ProcessZeroCost(PriceLoggingStat stat)
		{
			var stringBuilder = new StringBuilder();
			foreach (var cost in CostDescriptions)
				if ((cost.ZeroCostCount > 0 && stat.formCount == 0) || cost.ZeroCostCount == stat.formCount)
					stringBuilder.AppendFormat("ценовая колонка \"{0}\" полностью заполнена '0'\n", cost.Name);

			if (stringBuilder.Length > 0)
				SendAlertToUserFail(
					stringBuilder,
					"PriceProcessor: В прайс-листе {0} поставщика {1} имеются ценовые колонки, полностью заполненные ценой \"0\"",
					@"
Здравствуйте!
  В прайс-листе {0} поставщика {1} имеются ценовые колонки, полностью заполненные ценой '0'.
  Список ценовых колонок:
{2}

С уважением,
  PriceProcessor.");

		}

		protected void SendAlertToUserFail(StringBuilder stringBuilder, string subject, string body)
		{
			var drProvider = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(), @"
select
  if(pd.CostType = 1, concat('[Колонка] ', pc.CostName), pd.PriceName) PriceName,
  concat(cd.ShortName, ' - ', r.Region) ShortFirmName
from
usersettings.pricescosts pc,
usersettings.pricesdata pd,
usersettings.clientsdata cd,
farm.regions r
where
    pc.PriceItemId = ?PriceItemId
and pd.PriceCode = pc.PriceCode
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and cd.FirmCode = pd.FirmCode
and r.RegionCode = cd.RegionCode",
				new MySqlParameter("?PriceItemId", _priceItemId));
			subject = String.Format(subject, drProvider["PriceName"], drProvider["ShortFirmName"]);
			body = String.Format(
				body,
				drProvider["PriceName"],
				drProvider["ShortFirmName"],
				stringBuilder);

			_logger.DebugFormat("Сформировали предупреждение о настройках формализации прайс-листа: {0}", body);
			Mailer.SendUserFail(subject, body);
		}

		/// <summary>
		/// Установить название поля, которое будет считано из набора данных
		/// </summary>
		public void SetFieldName(PriceFields PF, string Value)
		{
			fieldNames[(int)PF] = Value;
		}

		public void Open()
		{
			_priceData = _parser.Parse(_filename);
		}

		private void CheckColumnPresents()
		{
			var sb = new StringBuilder();
			foreach (PriceFields pf in Enum.GetValues(typeof(PriceFields)))
				if ((pf != PriceFields.OriginalName) && !String.IsNullOrEmpty(GetFieldName(pf)) && !_priceData.Columns.Contains(GetFieldName(pf)))
					sb.AppendFormat("\"{0}\" настроено на {1}\n", GetDescription(pf), GetFieldName(pf));

			
			foreach (var cost in CostDescriptions)
				if (!String.IsNullOrEmpty(cost.FieldName) && !_priceData.Columns.Contains(cost.FieldName))
					sb.AppendFormat("ценовая колонка \"{0}\" настроена на {1}\n", cost.Name, cost.FieldName);

			if (sb.Length > 0)
				SendAlertToUserFail(
					sb,
					"PriceProcessor: В прайс-листе {0} поставщика {1} отсутствуют настроенные поля",
					@"
Здравствуйте!
В прайс-листе {0} поставщика {1} отсутствуют настроенные поля.
Следующие поля отсутствуют:
{2}

С уважением,
PriceProcessor.");

			if (_priceData.Rows.Count == 0)
				throw new FormalizeException("В полученом прайс листе не удалось найти ни одной позиции", _priceInfo);
		}

		public IEnumerable<FormalizationPosition> Read()
		{
			ValidateCosts();
			Open();
			if (null != toughMask)
				toughMask.Analyze(GetFieldRawValue(PriceFields.Name1));

			do
			{
				var name = GetFieldValue(PriceFields.Name1, true);

				if (String.IsNullOrEmpty(name))
					continue;

				var costs = ProcessCosts(CostDescriptions);

				var position = new FormalizationPosition {
					PositionName = name,
					Code = GetFieldValue(PriceFields.Code),
					OriginalName = GetFieldValue(PriceFields.OriginalName, true),
					FirmCr = GetFieldValue(PriceFields.FirmCr)
				};

				//Получается, что если формализовали по наименованию, то это позиция будет отображена клиенту
				InsertToCore(position, costs);
				yield return position;
			}
			while (Next());
		}

		private void ValidateCosts()
		{
			if (CostDescriptions.Count == 0 && !_priceInfo.IsAssortmentPrice)
				throw new WarningFormalizeException(Settings.Default.CostsNotExistsError, _priceInfo);

			//Если прайс является не ассортиментным прайсом-родителем с мультиколоночными ценами, то его надо проверить на базовую цену
			if (!_priceInfo.IsAssortmentPrice &&  _priceInfo.CostType == CostTypes.MultiColumn)
			{
				var baseCosts = CostDescriptions.Where(c => c.IsBaseCost).ToArray();
				if (baseCosts.Length == 0)
					throw new WarningFormalizeException(Settings.Default.BaseCostNotExistsError, _priceInfo);

				if (baseCosts.Length > 1)
				{
					throw new WarningFormalizeException(
						String.Format(Settings.Default.DoubleBaseCostsError,
							baseCosts[0].Id,
							baseCosts[1].Id),
						_priceInfo);
				}
				var baseCost = baseCosts.Single();

				if (baseCost.Begin == -1 && baseCost.End == -1 && String.Empty == baseCost.FieldName)
					throw new WarningFormalizeException(Settings.Default.FieldNameBaseCostsError, _priceInfo);
			}
		}

		/// <summary>
		/// Производится вставка данных в таблицу Core
		/// </summary>
		public void InsertToCore(FormalizationPosition position, Cost[] costs)
		{
			if (!position.Junk)
				position.Junk = (bool)GetFieldValueObject(PriceFields.Junk);

			var quantity = GetFieldValueObject(PriceFields.Quantity);
			var core = new NewCore {
				Code = GetFieldValue(PriceFields.Code),
				CodeCr = GetFieldValue(PriceFields.CodeCr),
				Unit = GetFieldValue(PriceFields.Unit),
				Volume = GetFieldValue(PriceFields.Volume),
				Quantity = quantity is DBNull ? null : quantity.ToString(),
				Note = GetFieldValue(PriceFields.Note),
				Doc = (string)GetFieldValueObject(PriceFields.Doc),

				Junk = position.Junk,
				Await = (bool)GetFieldValueObject(PriceFields.Await),
				VitallyImportant = (bool)GetFieldValueObject(PriceFields.VitallyImportant),

				MinBoundCost = GetDecimalValue(PriceFields.MinBoundCost),
				MaxBoundCost = GetDecimalValue(PriceFields.MaxBoundCost),
				OrderCost = GetDecimalValue(PriceFields.OrderCost),
				MinOrderCount = GetFieldValueObject(PriceFields.MinOrderCount) is DBNull ? 0 : (uint)GetFieldValueObject(PriceFields.MinOrderCount),
				RequestRatio = GetFieldValueObject(PriceFields.RequestRatio) is DBNull ? 0 : Convert.ToUInt32(GetFieldValueObject(PriceFields.RequestRatio)),
				RegistryCost = GetDecimalValue(PriceFields.RegistryCost),
			};

			if (quantity is int)
				core.QuantityAsInt = (int) quantity;
/*
			if (position.InternalProducerSynonymId.HasValue)
				drCore["InternalProducerSynonymId"] = position.InternalProducerSynonymId;*/

			var rawPeriodValue = GetFieldValueObject(PriceFields.Period);
			string periodValue;
			//если получилось преобразовать в дату, то сохраняем в формате даты
			if (rawPeriodValue is DateTime)
				periodValue = ((DateTime)rawPeriodValue).ToString("dd'.'MM'.'yyyy");
			else
			{
				//Если не получилось преобразовать, то смотрим на "сырое" значение поле, если оно не пусто, то пишем в базу
				periodValue = GetFieldRawValue(PriceFields.Period);
				if (String.IsNullOrEmpty(periodValue))
					periodValue = null;
			}
			core.Period = periodValue;
			core.Costs = costs;
			position.Core = core;
		}

		public decimal GetDecimalValue(PriceFields field)
		{
			var value = GetFieldValueObject(field);
			if (value is DBNull)
				return 0;
			if (Cost.IsZeroOrLess((decimal)value))
				return 0;
			return (decimal) value;
		}

		/// <summary>
		/// Перейти на следующую позици набора данных
		/// </summary>
		/// <returns>Удачно ли выполнен переход?</returns>
		public virtual bool Next()
		{
			_index++;
			if (_index < _priceData.Rows.Count)
			{
				if (null != toughMask)
					toughMask.Analyze( GetFieldRawValue(PriceFields.Name1) );
				return true;
			}
			return false;
		}

		/// <summary>
		/// Получить сырое значение текущего поля
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public virtual string GetFieldRawValue(PriceFields field)
		{
			try
			{
				//Если имя столбца для поля не определено, то возвращаем null
				if (String.IsNullOrEmpty(GetFieldName(field)))
					return null;

				var value = _priceData.Rows[_index][GetFieldName(field)].ToString();
				value = CleanupCharsThatNotFitIn1251(value);
				return value;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// строки в базе данных хранятся в 1251, если мы засунем в базу символ которого нет в 1251 
		/// то назад мы получим не этот же символ а что то "близкое" по этому строки нужно очищать от 
		/// таких символов
		/// </summary>
		public string CleanupCharsThatNotFitIn1251(string value)
		{
			var ansi = Encoding.GetEncoding(1251);
			var unicodeBytes = Encoding.Unicode.GetBytes(value);
			var ansiBytes = Encoding.Convert(Encoding.Unicode, ansi, unicodeBytes);
			return ansi.GetString(ansiBytes);
		}

		/// <summary>
		/// Получить значение поля в обработанном виде
		/// </summary>
		public virtual string GetFieldValue(PriceFields field)
		{
			string res = null;

			//Сначала пытаемся вытянуть данные из toughMask
			if (null != toughMask)
			{
				res = toughMask.GetFieldValue(field);
				if (null != res)
				{
					//Удаляем опасные слова только из наименований
					if ((PriceFields.Name1 == field) || (PriceFields.Name2 == field) || (PriceFields.Name2 == field) || (PriceFields.OriginalName == field))
						res = RemoveForbWords(res);
					if ((PriceFields.Note != field) && (PriceFields.Doc != field))
						res = UnSpace(res);
				}
			}

			//Если у нас это не получилось, что пытаемся вытянуть данные из самого поля
			if ((null == res) || ("" == res.Trim()))
			{
				res = GetFieldRawValue(field);
				if (null != res)
				{
					if ((PriceFields.Name1 == field) || (PriceFields.Name2 == field) || (PriceFields.Name2 == field))
						res = RemoveForbWords(res);
					res = UnSpace(res);
				}
			}

			if ((PriceFields.Name1 == field) || (PriceFields.Name2 == field) || (PriceFields.Name2 == field) || (PriceFields.OriginalName == field) ||(PriceFields.FirmCr == field))
			{
				if (null != res && res.Length > 255)
				{
					res = res.Remove(255, res.Length - 255);
					res = res.Trim();
				}
			}

			if (PriceFields.Name1 == field || PriceFields.OriginalName == field)
			{
				if (_priceData.Columns.IndexOf(GetFieldName(PriceFields.Name2) ) > -1)
					res = UnSpace(String.Format("{0} {1}", res, RemoveForbWords(GetFieldRawValue(PriceFields.Name2))));
				if (_priceData.Columns.IndexOf(GetFieldName(PriceFields.Name3) ) > -1)
					res = UnSpace(String.Format("{0} {1}", res, RemoveForbWords(GetFieldRawValue(PriceFields.Name3))));

				if (null != res && res.Length > 255)
				{
					res = res.Remove(255, res.Length - 255);
					res = res.Trim();
				}

				return res;
			}

			return res;
		}

		/// <summary>
		/// Получить значение поле в нижнем регистре
		/// </summary>
		/// <param name="PF"></param>
		/// <param name="LowerCase"></param>
		/// <returns></returns>
		public virtual string GetFieldValue(PriceFields PF, bool LowerCase)
		{
			string Value = GetFieldValue(PF);
			if ((null != Value) && LowerCase)
				return Value.ToLower();
			return Value;
		}

		/// <summary>
		/// Получить значение поля как объект
		/// </summary>
		/// <param name="PF"></param>
		/// <returns></returns>
		public virtual object GetFieldValueObject(PriceFields PF)
		{
			switch((int)PF)
			{
				case (int)PriceFields.Await:
					return GetBoolValue(PriceFields.Await, awaitPos);

				case (int)PriceFields.Junk:
					return GetJunkValue();

				case (int)PriceFields.VitallyImportant:
					return GetBoolValue(PriceFields.VitallyImportant, vitallyImportantMask);

				case (int)PriceFields.RequestRatio:
					return ProcessInt(GetFieldRawValue(PF));

				case (int)PriceFields.Code:
				case (int)PriceFields.CodeCr:
				case (int)PriceFields.Doc:
				case (int)PriceFields.FirmCr:
				case (int)PriceFields.Name1:
				case (int)PriceFields.Name2:
				case (int)PriceFields.Name3:
				case (int)PriceFields.Note:
				case (int)PriceFields.Unit:
				case (int)PriceFields.Volume:
				case (int)PriceFields.OriginalName:
					return GetFieldValue(PF);

				case (int)PriceFields.MinBoundCost:
				case (int)PriceFields.RegistryCost:
				case (int)PriceFields.MaxBoundCost:
				case (int)PriceFields.OrderCost:
					return ProcessCost(GetFieldRawValue(PF));

				case (int)PriceFields.Quantity:
				case (int)PriceFields.MinOrderCount:
					return ProcessInt(GetFieldRawValue(PF));

				case (int)PriceFields.Period:
					return ProcessPeriod(GetFieldRawValue(PF));

				default:
					return DBNull.Value;
			}
		}

		/// <summary>
		/// Обработать значение цены
		/// </summary>
		/// <param name="CostValue"></param>
		/// <returns></returns>
		public virtual object ProcessCost(string CostValue)
		{
			if (!String.IsNullOrEmpty(CostValue))
			{
				try
				{
					var nfi = CultureInfo.CurrentCulture.NumberFormat;
					var res = String.Empty;
					foreach (var c in CostValue)
					{
						if (Char.IsDigit(c))
							res = String.Concat(res, c);
						else
						{
							if ((!Char.IsWhiteSpace(c)) && (res != String.Empty) && (-1 == res.IndexOf(nfi.CurrencyDecimalSeparator)))
								res = String.Concat(res, nfi.CurrencyDecimalSeparator);
						}
					}

					//Если результирующая строка пуста, то возвращаем DBNull
					if (String.IsNullOrEmpty(res))
						return DBNull.Value;

					var value = Decimal.Parse(res, NumberStyles.Currency);
					value = Math.Round(value, 6);
					return value;
				}
				catch
				{
					return DBNull.Value;
				}
			}
			return DBNull.Value;
		}

		/// <summary>
		/// Обработать значение IntValue и получить результать как целое число
		/// </summary>
		/// <param name="IntValue"></param>
		/// <returns></returns>
		public virtual object ProcessInt(string IntValue)
		{
			if (!String.IsNullOrEmpty(IntValue))
			{
				try
				{
					var cost = ProcessCost(IntValue);
					if (cost is decimal)
						return Convert.ToInt32(decimal.Truncate((decimal) cost));
					return cost;
				}
				catch
				{
					return DBNull.Value;
				}
			}
			return DBNull.Value;
		}

		/// <summary>
		/// Обработать значение срока годности
		/// </summary>
		/// <param name="PeriodValue"></param>
		/// <returns></returns>
		public virtual object ProcessPeriod(string PeriodValue)
		{
			DateTime res = DateTime.MaxValue;
			string pv;
			if (null != toughMask)
			{
				pv = toughMask.GetFieldValue(PriceFields.Period);
				if (!String.IsNullOrEmpty(pv))
				{
					res = toughDate.Analyze( pv );
					if (!DateTime.MaxValue.Equals(res))
						return res;
				}
			}
			if (!String.IsNullOrEmpty(PeriodValue))
			{
				res = toughDate.Analyze( PeriodValue );
				if (DateTime.MaxValue.Equals(res))
				{
					return DBNull.Value;
				}
				return res;
			}
			return DBNull.Value;
		}

		/// <summary>
		/// Убрать лишние пробелы в имени
		/// </summary>
		public string UnSpace(string Value)
		{
			if (null != Value)
			{
				Value = Value.Trim(); 
				while(Value.IndexOf("  ") > -1)
					Value = Value.Replace("  ", " ");
				return Value;
			}
			return null;
		}

		/// <summary>
		/// Удалить запрещенные слова
		/// </summary>
		public string RemoveForbWords(string value)
		{
			if (value == null)
				return null;
			if (forbWordsList == null)
				return value;
			value = Enumerable.Aggregate(forbWordsList, value, (current, s) => current.Replace(s, ""));

			if (String.Empty == value)
				return null;
			return value;
		}

		protected bool GetBoolValue(PriceFields priceField, string mask)
		{
			bool value = false;

			var trueValues = new[] { "истина", "true"};
			var falseValues = new[] { "ложь", "false" };

			string[] selectedValues = null;

			try
			{
				foreach (string boolValue in trueValues)
					if (boolValue.Equals(mask, StringComparison.OrdinalIgnoreCase))
						selectedValues = trueValues;

				foreach (string boolValue in falseValues)
					if (boolValue.Equals(mask, StringComparison.OrdinalIgnoreCase))
						selectedValues = falseValues;

				string fieldValue = GetFieldValue(priceField);

				//Если в столбце значение пусто, то возвращаем значение по умолчанию
				if (String.IsNullOrEmpty(fieldValue))
					return value;

				if (selectedValues != null)
				{
					Regex reRussian = new Regex(selectedValues[0], RegexOptions.IgnoreCase);
					Match mRussian = reRussian.Match(fieldValue);
					Regex reEnglish = new Regex(selectedValues[1], RegexOptions.IgnoreCase);
					Match mEnglish = reEnglish.Match(fieldValue);
					value = (mRussian.Success || mEnglish.Success);
				}
				else
				{
					Regex re = new Regex(mask);
					Match m = re.Match(fieldValue);
					value = (m.Success);
				}
			}
			catch
			{
			}

			return value;
		}

		/// <summary>
		/// Получить значение поля Junk
		/// </summary>
		/// <returns></returns>
		public bool GetJunkValue()
		{
			var JunkValue = false;
			var t = GetFieldValueObject(PriceFields.Period);
			if (t is DateTime)
			{
				var dt = (DateTime)t;
				var ts = SystemTime.Now().Subtract(dt);
				JunkValue = (Math.Abs(ts.Days) < 180);
			}
			if (!JunkValue)
			{
				JunkValue = GetBoolValue(PriceFields.Junk, junkPos);
			}

			return JunkValue;
		}

		/// <summary>
		/// Обрабатывает цены и возврашает кол-во не нулевых цен
		/// </summary>
		public Cost[] ProcessCosts(List<CostDescription> descriptions)
		{
			var costs = new List<Cost>();
			for(var i = 0; i < descriptions.Count; i++)
			{
				var description = descriptions[i];
				
				var costValue = _priceData.Rows[_index][description.FieldName];
				var value = Cost.Parse(costValue);

				if (value == 0)
					description.ZeroCostCount++;
				if (Cost.IsZeroOrLess(value))
				{
					description.UndefinedCostCount++;
					continue;
				}
				
				costs.Add(new Cost(description, value));
			}
			return costs.ToArray();
		}
	}
}