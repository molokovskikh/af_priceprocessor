using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Helpers;
using Inforoom.PriceProcessor.Properties;
using log4net;
using MySql.Data.MySqlClient;
#if BUTCHER
using MySql.Data.MySqlClient.Source;
#endif

namespace Inforoom.PriceProcessor.Formalizer.New
{
	public class BasePriceParser2
	{
		//таблица с прайсом
		protected DataTable dtPrice;

		//Соедиение с базой данных
		protected MySqlConnection _connection;

		//Таблица со списком запрещенных названий
		protected MySqlDataAdapter daForbidden;
		protected DataTable dtForbidden;
		//Таблица со списком синонимов товаров
		protected MySqlDataAdapter daSynonym;
		protected DataTable dtSynonym;
		//Таблица со списоком синонимов производителей
		protected MySqlDataAdapter daSynonymFirmCr;
		protected DataTable dtSynonymFirmCr;
		protected DataTable dtNewSynonymFirmCr;
		//Таблица с ассортиментом
		protected MySqlDataAdapter daAssortment;
		protected DataTable dtAssortment;
		//Таблица с исключениями
		protected MySqlDataAdapter daExcludes;
		protected DataTable dtExcludes;
		protected MySqlCommandBuilder cbExcludes;

		Stopwatch assortmentSearchWatch;
		int assortmentSearchCount;
		Stopwatch excludesSearchWatch;
		int excludesSearchCount;

		protected MySqlDataAdapter daUnrecExp;
		protected MySqlCommandBuilder cbUnrecExp;
		protected DataTable dtUnrecExp;
		protected MySqlDataAdapter daZero;
		protected MySqlCommandBuilder cbZero;
		protected DataTable dtZero;
		protected MySqlDataAdapter daForb;
		protected MySqlCommandBuilder cbForb;
		protected DataTable dtForb;

		protected DataSet dsMyDB;

		protected string[] FieldNames;

		protected int CurrPos = -1;

		protected string priceFileName;

		private FormalizeStats _stats = new FormalizeStats();

		//Является ли обрабатываемый прайс-лист загруженным?
		public bool downloaded;

		//ключ для priceitems
		public long priceItemId;

		//родительский синоним : прайс-родитель, нужен для выбора различных параметров
		protected long parentSynonym;
		//Кол-во распознаных позиций в прошлый раз
		protected long prevRowCount;
		//производить формализацию по коду
		protected bool formByCode;

		//Маска, которая накладывается на имя позиции
		protected string nameMask;
		//Запрещенные слова, которые могут быть в имени
		protected string forbWords;
		protected string[] forbWordsList;
		//как в прайсе поставщика метятся ожидаемые позиции
		protected string awaitPos;
		//как в прайсе поставщика метятся "плохие" позиции
		protected string junkPos;
		//как в прайсе поставщика метятся жизненно-важные позиции
		protected string vitallyImportantMask;

		//Тип ценовых колонок прайса-родителя: 0 - мультиколоночный, 1 - многофайловый
		protected CostTypes costType;

		//Надо ли конвертировать полученную строку в ANSI
		protected bool convertedToANSI;

		protected ToughDate toughDate;
		protected ToughMask toughMask;

		protected readonly ILog _logger;

		protected PriceFormalizationInfo _priceInfo;
		protected PriceLoggingStat _loggingStat = new PriceLoggingStat();

		private Searcher _searcher;

		private readonly List<NewCore> _newCores = new List<NewCore>();
		private readonly List<ExistsCore> _existsCores = new List<ExistsCore>();

		private readonly IParser _parser;

		public List<CostDescription> _costDescriptions;

		public PriceFormalizationInfo PriceInfo
		{
			get { return _priceInfo; }
		}

		public PriceLoggingStat Stat
		{
			get { return _loggingStat; }
		}

		/// <summary>
		/// Конструктор парсера
		/// </summary>
		public BasePriceParser2(string priceFileName, IParser parser, MySqlConnection connection, DataTable data)
		{
			_logger = LogManager.GetLogger(GetType());
			_logger.DebugFormat("Создали класс для обработки файла {0}", priceFileName);

			_parser = parser;

			var priceInfo = data.Rows[0];
			_priceInfo = new PriceFormalizationInfo(priceInfo);

			this.priceFileName = priceFileName;
			dtPrice = new DataTable();
			_connection = connection;
			dsMyDB = new DataSet();
			FieldNames = new string[Enum.GetNames(typeof(PriceFields)).Length];
			
			priceItemId = Convert.ToInt64(priceInfo[FormRules.colPriceItemId]); 
			parentSynonym = Convert.ToInt64(priceInfo[FormRules.colParentSynonym]); 
			costType = (CostTypes)Convert.ToInt32(priceInfo[FormRules.colCostType]);

			nameMask = priceInfo[FormRules.colNameMask] is DBNull ? String.Empty : (string)priceInfo[FormRules.colNameMask];

			//Производим попытку разобрать строку с "запрещенными выражениями"
			forbWords = priceInfo[FormRules.colForbWords] is DBNull ? String.Empty : (string)priceInfo[FormRules.colForbWords];
			forbWords = forbWords.Trim();
			if (String.Empty != forbWords)
			{
				forbWordsList = forbWords.Split( new[] {' '} );
				var len = 0;
				foreach(var s in forbWordsList)
					if(String.Empty != s)
						len++;
				if (len > 0)
				{
					var newForbWordList = new string[len];
					var i = 0;
					foreach(var s in forbWordsList)
						if(String.Empty != s)
						{
							newForbWordList[i] = s;
							i++;
						}
				}
				else
					forbWordsList = null;
			}

			awaitPos = priceInfo[FormRules.colSelfAwaitPos].ToString();
			junkPos  = priceInfo[FormRules.colSelfJunkPos].ToString();
			vitallyImportantMask = priceInfo[FormRules.colSelfVitallyImportantMask].ToString();
			prevRowCount = priceInfo[FormRules.colPrevRowCount] is DBNull ? 0 : Convert.ToInt64(priceInfo[FormRules.colPrevRowCount]);

			toughDate = new ToughDate();
			if (String.Empty != nameMask)
				toughMask = new ToughMask(nameMask, _priceInfo);

			string selectCostFormRulesSQL;
			if (costType == CostTypes.MultiColumn)
				selectCostFormRulesSQL = String.Format("select * from usersettings.PricesCosts pc, farm.CostFormRules cfr where pc.PriceCode={0} and cfr.CostCode = pc.CostCode", _priceInfo.PriceCode);
			else
				selectCostFormRulesSQL = String.Format("select * from usersettings.PricesCosts pc, farm.CostFormRules cfr where pc.PriceCode={0} and cfr.CostCode = pc.CostCode and pc.CostCode = {1}", _priceInfo.PriceCode, _priceInfo.CostCode.Value);
			var daPricesCost = new MySqlDataAdapter( selectCostFormRulesSQL, _connection );
			var dtPricesCost = new DataTable("PricesCosts");
			daPricesCost.Fill(dtPricesCost);
			_costDescriptions = dtPricesCost.Rows.Cast<DataRow>().Select(r => new CostDescription(r)).ToList();
			_logger.DebugFormat("Загрузили цены {0}.{1}", _priceInfo.PriceCode, _priceInfo.CostCode);

			if (_costDescriptions.Count == 0 && !_priceInfo.IsAssortmentPrice)
				throw new WarningFormalizeException(Settings.Default.CostsNotExistsError, _priceInfo);

			//Если прайс является не ассортиментным прайсом-родителем с мультиколоночными ценами, то его надо проверить на базовую цену
			if (!_priceInfo.IsAssortmentPrice && costType == CostTypes.MultiColumn)
			{
				var baseCosts = _costDescriptions.Where(c => c.IsBaseCost).ToArray();
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

				if ((this is FixedNativeTextParser1251 || this is FixedNativeTextParser866) 
					&& ((baseCost.Begin == -1 || baseCost.End == -1) || String.Empty == baseCost.FieldName))
					throw new WarningFormalizeException(Settings.Default.FieldNameBaseCostsError, _priceInfo);
			}

			foreach(PriceFields pf in Enum.GetValues(typeof(PriceFields)))
			{
				var tmpName = (PriceFields.OriginalName == pf) ? "FName1" : "F" + pf;
				SetFieldName(pf, data.Rows[0][tmpName] is DBNull ? String.Empty : (string)data.Rows[0][tmpName]);
			}
		}

		/// <summary>
		/// Производится вставка данных в таблицу Core
		/// </summary>
		public void InsertToCore(FormalizationPosition position, Cost[] costs)
		{
			if (!position.Junk)
				position.Junk = (bool)GetFieldValueObject(PriceFields.Junk);

			var core = new NewCore {
				ProductId = (uint) position.ProductId.Value,
				CodeFirmCr = position.CodeFirmCr.HasValue ? (uint) position.CodeFirmCr.Value : 0,
				SynonymCode = (uint) position.SynonymCode.Value,
				SynonymFirmCrCode = position.SynonymFirmCrCode.HasValue ? (uint) position.SynonymFirmCrCode.Value : 0,

				Code = GetFieldValue(PriceFields.Code),
				CodeCr = GetFieldValue(PriceFields.CodeCr),
				Unit = GetFieldValue(PriceFields.Unit),
				Volume = GetFieldValue(PriceFields.Volume),
				Quantity = GetFieldValueObject(PriceFields.Quantity) is DBNull ? null : GetFieldValueObject(PriceFields.Quantity).ToString(),
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
			core.ExistsCore = _searcher.Find(core);
			_newCores.Add(core);
			_loggingStat.formCount++;
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
		/// Вставка в таблицу запрещенных предложений
		/// </summary>
		/// <param name="PosName"></param>
		public void InsertIntoForb(string PosName)
		{
			var newRow = dtForb.NewRow();
			newRow["PriceItemId"] = priceItemId;
			newRow["Forb"] = PosName;
			dtForb.Rows.Add(newRow);
			_loggingStat.forbCount++;
		}

		/// <summary>
		/// Вставка записи в Zero
		/// </summary>
		public void InsertToZero()
		{
			DataRow drZero = dtZero.NewRow();

			drZero["PriceItemId"] = priceItemId;
			drZero["Name"] = GetFieldValueObject(PriceFields.Name1);
			drZero["FirmCr"] = GetFieldValueObject(PriceFields.FirmCr);
			drZero["Code"] = GetFieldValueObject(PriceFields.Code);
			drZero["CodeCr"] = GetFieldValueObject(PriceFields.CodeCr);
			drZero["Unit"] = GetFieldValueObject(PriceFields.Unit);
			drZero["Volume"] = GetFieldValueObject(PriceFields.Volume);
			drZero["Quantity"] = GetFieldValueObject(PriceFields.Quantity);
			drZero["Note"] = GetFieldValueObject(PriceFields.Note);
			drZero["Period"] = GetFieldValueObject(PriceFields.Period);
			drZero["Doc"] = GetFieldValueObject(PriceFields.Doc);

			dtZero.Rows.Add(drZero);

			_loggingStat.zeroCount++;
		}

		/// <summary>
		/// Вставка в нераспознанные позиции
		/// </summary>
		public void InsertToUnrec(FormalizationPosition position)
		{
			DataRow drUnrecExp = dtUnrecExp.NewRow();
			drUnrecExp["PriceItemId"] = priceItemId;
			drUnrecExp["Name1"] = GetFieldValue(PriceFields.Name1);
			drUnrecExp["FirmCr"] = GetFieldValue(PriceFields.FirmCr);
			drUnrecExp["Code"] = GetFieldValue(PriceFields.Code);
			drUnrecExp["CodeCr"] = GetFieldValue(PriceFields.CodeCr);
			drUnrecExp["Unit"] = GetFieldValue(PriceFields.Unit);
			drUnrecExp["Volume"] = GetFieldValue(PriceFields.Volume);
			drUnrecExp["Quantity"] = GetFieldValueObject(PriceFields.Quantity);
			drUnrecExp["Note"] = GetFieldValue(PriceFields.Note);
			drUnrecExp["Period"] = GetFieldValueObject(PriceFields.Period);
			drUnrecExp["Doc"] = GetFieldValueObject(PriceFields.Doc);

			if (!position.Junk)
				position.Junk = (bool)GetFieldValueObject(PriceFields.Junk);
			drUnrecExp["Junk"] = Convert.ToByte(position.Junk);

			drUnrecExp["AddDate"] = DateTime.Now;

			drUnrecExp["Status"] = (byte)position.Status;
			drUnrecExp["Already"] = (byte)position.Status;

			if (position.ProductId.HasValue)
				drUnrecExp["PriorProductId"] = position.ProductId;
			if (position.CodeFirmCr.HasValue)
				drUnrecExp["PriorProducerId"] = position.CodeFirmCr;
			if (position.SynonymCode.HasValue)
				drUnrecExp["ProductSynonymId"] = position.SynonymCode;
			if (position.SynonymFirmCrCode.HasValue)
				drUnrecExp["ProducerSynonymId"] = position.SynonymFirmCrCode;
			if (position.InternalProducerSynonymId.HasValue)
				drUnrecExp["InternalProducerSynonymId"] = position.InternalProducerSynonymId;

			if (dtUnrecExp.Columns.Contains("HandMade"))
				drUnrecExp["HandMade"] = 0;

			dtUnrecExp.Rows.Add(drUnrecExp);
			_loggingStat.unrecCount++;
		}

		/// <summary>
		/// Подготовка к разбору прайса, чтение таблиц
		/// </summary>
		public void Prepare()
		{
			_logger.Debug("начало Prepare");
			daForbidden = new MySqlDataAdapter(
				String.Format("SELECT PriceCode, LOWER(Forbidden) AS Forbidden FROM farm.Forbidden WHERE PriceCode={0}", _priceInfo.PriceCode), _connection);
			daForbidden.Fill(dsMyDB, "Forbidden");
			dtForbidden = dsMyDB.Tables["Forbidden"];
			_logger.Debug("загрузили Forbidden");

			daSynonym = new MySqlDataAdapter(
				String.Format(@"
SELECT 
  Synonym.SynonymCode, 
  LOWER(Synonym.Synonym) AS Synonym, 
  Synonym.ProductId, 
  Synonym.Junk,
  products.CatalogId
FROM 
  farm.Synonym, 
  catalogs.products 
WHERE 
    (Synonym.PriceCode={0}) 
and (products.Id = Synonym.ProductId)
"
					, 
					parentSynonym), _connection);
			daSynonym.Fill(dsMyDB, "Synonym");
			dtSynonym = dsMyDB.Tables["Synonym"];
			_logger.Debug("загрузили Synonym");

			daAssortment = new MySqlDataAdapter("SELECT Id, CatalogId, ProducerId, Checked FROM catalogs.Assortment ", _connection);
			var excludesBuilder  = new MySqlCommandBuilder(daAssortment);
			daAssortment.InsertCommand = excludesBuilder.GetInsertCommand();
			daAssortment.InsertCommand.CommandTimeout = 0;
			daAssortment.Fill(dsMyDB, "Assortment");
			dtAssortment = dsMyDB.Tables["Assortment"];
			_logger.Debug("загрузили Assortment");
			dtAssortment.PrimaryKey = new[] { dtAssortment.Columns["CatalogId"], dtAssortment.Columns["ProducerId"] };
			_logger.Debug("построили индекс по Assortment");

			daExcludes = new MySqlDataAdapter(
				String.Format("SELECT Id, CatalogId, ProducerSynonymId, PriceCode, OriginalSynonymId FROM farm.Excludes where PriceCode = {0}", parentSynonym), _connection);
			cbExcludes = new MySqlCommandBuilder(daExcludes);
			daExcludes.InsertCommand = cbExcludes.GetInsertCommand();
			daExcludes.InsertCommand.CommandTimeout = 0;
			daExcludes.Fill(dsMyDB, "Excludes");
			dtExcludes = dsMyDB.Tables["Excludes"];
			_logger.Debug("загрузили Excludes");
			dtExcludes.Constraints.Add("ProducerSynonymKey", new[] { dtExcludes.Columns["CatalogId"], dtExcludes.Columns["ProducerSynonymId"] }, false);
			_logger.Debug("построили индекс по Excludes");

			assortmentSearchWatch = new Stopwatch();
			assortmentSearchCount = 0;
			excludesSearchWatch = new Stopwatch();
			excludesSearchCount = 0;


			daSynonymFirmCr = new MySqlDataAdapter(
				String.Format(@"
SELECT
  SynonymFirmCrCode,
  CodeFirmCr,
  LOWER(Synonym) AS Synonym,
  (aps.ProducerSynonymId is not null) as IsAutomatic
FROM
  farm.SynonymFirmCr
  left join farm.AutomaticProducerSynonyms aps on aps.ProducerSynonymId = SynonymFirmCr.SynonymFirmCrCode
WHERE SynonymFirmCr.PriceCode={0}
"
					, 
					parentSynonym), _connection);
			daSynonymFirmCr.Fill(dsMyDB, "SynonymFirmCr");
			daSynonymFirmCr.InsertCommand = new MySqlCommand(@"
insert into farm.SynonymFirmCr (PriceCode, CodeFirmCr, Synonym) values (?PriceCode, null, ?OriginalSynonym);
set @LastSynonymFirmCrCode = last_insert_id();
insert farm.UsedSynonymFirmCrLogs (SynonymFirmCrCode) values (@LastSynonymFirmCrCode);
insert into farm.AutomaticProducerSynonyms (ProducerSynonymId) values (@LastSynonymFirmCrCode);
select @LastSynonymFirmCrCode;");
			daSynonymFirmCr.InsertCommand.Parameters.Add("?PriceCode", MySqlDbType.Int64);
			daSynonymFirmCr.InsertCommand.Parameters.Add("?OriginalSynonym", MySqlDbType.String);
			daSynonymFirmCr.InsertCommand.Connection = _connection;
			dtSynonymFirmCr = dsMyDB.Tables["SynonymFirmCr"];
			dtSynonymFirmCr.Columns.Add("OriginalSynonym", typeof(string));
			dtSynonymFirmCr.Columns.Add("InternalProducerSynonymId", typeof(long));
			dtSynonymFirmCr.Columns["InternalProducerSynonymId"].AutoIncrement = true;
			_logger.Debug("загрузили SynonymFirmCr");

			daUnrecExp = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.UnrecExp WHERE PriceItemId={0} LIMIT 0", priceItemId), _connection);
			cbUnrecExp = new MySqlCommandBuilder(daUnrecExp);
			cbUnrecExp.ReturnGeneratedIdentifiers = false;
			daUnrecExp.AcceptChangesDuringUpdate = false;
			daUnrecExp.InsertCommand = cbUnrecExp.GetInsertCommand();
			daUnrecExp.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
			daUnrecExp.InsertCommand.CommandTimeout = 0;
			daUnrecExp.Fill(dsMyDB, "UnrecExp");
			dtUnrecExp = dsMyDB.Tables["UnrecExp"];
			dtUnrecExp.Columns["AddDate"].DataType = typeof(DateTime);
			dtUnrecExp.Columns.Add("InternalProducerSynonymId", typeof(long));
			_logger.Debug("загрузили UnrecExp");

			daZero = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Zero WHERE PriceItemId={0} LIMIT 0", priceItemId), _connection);
			cbZero = new MySqlCommandBuilder(daZero);
			daZero.InsertCommand = cbZero.GetInsertCommand();
			daZero.InsertCommand.CommandTimeout = 0;
			daZero.Fill(dsMyDB, "Zero");
			dtZero = dsMyDB.Tables["Zero"];
			_logger.Debug("загрузили Zero");

			daForb = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Forb WHERE PriceItemId={0} LIMIT 0", priceItemId), _connection);
			cbForb = new MySqlCommandBuilder(daForb);
			daForb.InsertCommand = cbForb.GetInsertCommand();
			daForb.InsertCommand.CommandTimeout = 0;
			daForb.Fill(dsMyDB, "Forb");
			dtForb = dsMyDB.Tables["Forb"];
			dtForb.Constraints.Add("ForbName", new[] {dtForb.Columns["Forb"]}, false);
			_logger.Debug("загрузили Forb");

			if (_priceInfo.IsUpdating)
			{
				var loadExistsWatch = Stopwatch.StartNew();
				LoadCore();
				_logger.Debug("Загрузили предложения");
				if (_existsCores.Count > 0)
				{
					LoadCosts();
					_logger.Debug("Загрузили цены");
				}
				_searcher = new Searcher(_existsCores);
				loadExistsWatch.Stop();
				_logger.InfoFormat("Загрузка и подготовка существующего прайса, {0}с", loadExistsWatch.Elapsed.TotalSeconds);
			}

			_logger.Debug("конец Prepare");
		}

		private void LoadCore()
		{
			string existsCoreSQL;
			var columns = String.Join(", ", typeof(ExistsCore).GetFields().Where(f => f.Name != "Costs" && f.Name != "NewCore").Select(f => f.Name).ToArray());
			if (costType == CostTypes.MultiColumn)
				existsCoreSQL = String.Format("SELECT {1} FROM farm.Core0 WHERE PriceCode={0} order by Id", _priceInfo.PriceCode, columns);
			else
				existsCoreSQL = String.Format("SELECT Core0.* FROM farm.Core0, farm.CoreCosts WHERE Core0.PriceCode={0} and CoreCosts.Core_Id = Core0.id and CoreCosts.PC_CostCode = {1} order by Core0.Id", _priceInfo.PriceCode, _priceInfo.CostCode);

			var command = new MySqlCommand(existsCoreSQL, _connection);
			using(var reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					_existsCores.Add(new ExistsCore {
						Id = reader.GetUInt64(0),
						ProductId = reader.GetUInt32(1),
						CodeFirmCr = GetUintOrDbNUll(reader, 2),
						SynonymCode = GetUintOrDbNUll(reader, 3),
						SynonymFirmCrCode = GetUintOrDbNUll(reader, 4),

						Code = reader.GetString(5),
						CodeCr = reader.GetString(6),
						Unit = reader.GetString(7),
						Volume = reader.GetString(8),
						Quantity = reader.GetString(9),
						Note = reader.GetString(10),
						Period = reader.GetString(11),
						Doc = reader.GetString(12),

						RegistryCost = GetDecimalOrDbNull(reader, 13),

						Junk = reader.GetBoolean(14),
						Await = reader.GetBoolean(15),
						VitallyImportant = reader.GetBoolean(16),

						MinBoundCost = GetDecimalOrDbNull(reader, 17),
						MaxBoundCost = GetDecimalOrDbNull(reader, 18),

						RequestRatio = GetUintOrDbNUll(reader, 19),
						OrderCost = GetDecimalOrDbNull(reader, 20),
						MinOrderCount = GetUintOrDbNUll(reader, 21)
					});
				}
			}
		}

		private void LoadCosts()
		{
			string existsCoreCostsSQL;
			if (costType == CostTypes.MultiColumn)
				existsCoreCostsSQL = String.Format(@"
select cc.Core_id, cc.PC_CostCode, cc.Cost
from farm.Core0 c
	join farm.CoreCosts cc on cc.Core_Id = c.id
where c.PriceCode = {0} 
order by c.Id", _priceInfo.PriceCode);
			else
				existsCoreCostsSQL = String.Format("SELECT CoreCosts.* FROM farm.Core0, farm.CoreCosts WHERE Core0.PriceCode={0} and CoreCosts.Core_Id = Core0.id and CoreCosts.PC_CostCode = {1} order by Core0.Id", _priceInfo.PriceCode, _priceInfo.CostCode);
			var costsCommand = new MySqlCommand(existsCoreCostsSQL, _connection);
			using(var reader = costsCommand.ExecuteReader())
			{
				var index = 0;
				var core = _existsCores[0];
				var costs = new List<Cost>();
				while (reader.Read())
				{
					var coreId = reader.GetUInt64(0);
					var costId = reader.GetUInt32(1);
					var description = _costDescriptions.First(c => c.Id == costId);
					if (coreId != core.Id)
					{
						core.Costs = costs.ToArray();
						costs = new List<Cost>();
						core = null;
						for(var i = index; i < _existsCores.Count; i++)
						{
							if (_existsCores[i].Id == coreId)
							{
								index = i;
								core = _existsCores[i];
								break;
							}
						}
						if (core == null)
							throw new Exception(String.Format("Не удалось найти позицию в Core, Id = {0}", coreId));
					}
					costs.Add(new Cost(description, reader.GetDecimal(2)));
				}
				core.Costs = costs.ToArray();
			}
			_logger.Debug("Загрузили цены");
		}

		public uint GetUintOrDbNUll(MySqlDataReader reader, int index)
		{
			if (reader.IsDBNull(index))
				return 0;
			return reader.GetUInt32(index);
		}

		public decimal GetDecimalOrDbNull(MySqlDataReader reader, int index)
		{
			if (reader.IsDBNull(index))
				return 0;
			return reader.GetDecimal(index);
		}

		public string StatCommand(MySqlCommand command)
		{
			var startTime = DateTime.UtcNow;
			var applyCount = command.ExecuteNonQuery();
			var workTime = DateTime.UtcNow.Subtract(startTime);
			return String.Format("{0};{1}", applyCount, workTime);
		}

		public string TryUpdate(MySqlDataAdapter da, DataTable dt, MySqlTransaction tran)
		{
			var startTime = DateTime.UtcNow;
			da.SelectCommand.Transaction = tran;
			var applyCount = da.Update(dt);
			var workTime = DateTime.UtcNow.Subtract(startTime);
			return String.Format("{0};{1}", applyCount, workTime);
		}

		private DataRow CheckPositionByProducerSynonym(DataRow drCore)
		{
			var drNewSynonym = dtNewSynonymFirmCr.Select("InternalProducerSynonymId = " + drCore["InternalProducerSynonymId"])[0];

			//Если это вновь созданных синоним, то возвращаем ссылку на него, иначе изменяем Core
			if (drNewSynonym.RowState == DataRowState.Added)
				return drNewSynonym;

			drCore["SynonymFirmCrCode"] = drNewSynonym["SynonymFirmCrCode"];
			drCore["InternalProducerSynonymId"] = DBNull.Value;
			if (!Convert.ToBoolean(drNewSynonym["IsAutomatic"]) && !Convert.IsDBNull(drNewSynonym["CodeFirmCr"]))
			{
				var status = GetAssortmentStatus(
					Convert.ToInt64(drCore["CatalogId"]),
					Convert.ToInt64(drNewSynonym["CodeFirmCr"]),
					Convert.ToInt64(drNewSynonym["SynonymFirmCrCode"]),
					Convert.ToInt64(drCore["SynonymCode"]));
				if (status == UnrecExpStatus.AssortmentForm)
					drCore["CodeFirmCr"] = drNewSynonym["CodeFirmCr"];
			}
			return null;
		}

		private IEnumerable<string> BuildSql()
		{
			foreach (var core in _newCores)
			{
				if (core.ExistsCore == null)
				{
					yield return SqlBuilder.InsertCoreCommand(_priceInfo, core);
					if (core.Costs != null && core.Costs.Length > 0)
						yield return SqlBuilder.InsertCostsCommand(core);
				}
				else
				{
					yield return SqlBuilder.UpdateCoreCommand(core);
					yield return SqlBuilder.UpdateCostsCommand(core);
				}
			}

			var forDelete = _existsCores.Where(c => c.NewCore == null).Select(c => c.Id.ToString()).ToArray();
			if (forDelete.Length > 0)
				yield return "delete from farm.Core0 where Core0.Id in (" + String.Join(", ", forDelete.ToArray()) + ");";

			var usedProductSynonyms = _newCores.GroupBy(c => c.SynonymCode).Select(c => c.Key.ToString()).ToArray();
			if (usedProductSynonyms.Length > 0)
				yield return "update farm.UsedSynonymLogs set LastUsed = now() where SynonymCode in (" + String.Join(", ", usedProductSynonyms) + ");";

			var usedProducerSynonyms = _newCores.Where(c => c.SynonymFirmCrCode != 0).GroupBy(c => c.SynonymFirmCrCode).Select(c => c.Key.ToString()).ToArray();
			if (usedProducerSynonyms.Length > 0)
				yield return "update farm.UsedSynonymFirmCrLogs set LastUsed = now() where SynonymFirmCrCode in (" + String.Join(", ", usedProducerSynonyms) + ");";
		}

		/// <summary>
		/// Окончание разбора прайса, с последующим логированием статистики
		/// </summary>
		public void FinalizePrice()
		{
			//Проверку и отправку уведомлений производим только для загруженных прайс-листов
			if (downloaded)
				ProcessUndefinedCost();

			if (Settings.Default.CheckZero && (_loggingStat.zeroCount > (_loggingStat.formCount + _loggingStat.unformCount + _loggingStat.zeroCount) * 0.95) )
				throw new RollbackFormalizeException(Settings.Default.ZeroRollbackError, _priceInfo, _loggingStat);

			if (_loggingStat.formCount * 1.6 < prevRowCount)
				throw new RollbackFormalizeException(Settings.Default.PrevFormRollbackError, _priceInfo, _loggingStat);

			var done = false;
			var tryCount = 0;
			do
			{
				var logMessage = new StringBuilder();

				var transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);

				try
				{
					InsertNewProducerSynonyms(transaction);
#if BUTCHER
					var buffer = new byte[10 * 1024 * 1024];
					var batcher = new Batcher(_connection);
					var encoding = Encoding.GetEncoding(1251);
					Action<string, int> withBuffer = (command, bytes) => encoding.GetBytes(command, 0, command.Length, buffer, bytes);

					foreach (var populatedBytes in PrepareData(withBuffer))
					{
						batcher.Send(buffer, populatedBytes);
					}
#endif

					Maintain(transaction, logMessage);

					transaction.Commit();
					done = true;
				}
				catch (MySqlException ex)
				{
					transaction.Rollback();

					if (!(tryCount <= Settings.Default.MaxRepeatTranCount && (1213 == ex.Number || 1205 == ex.Number || 1422 == ex.Number)))
						throw;

					tryCount++;
					_logger.InfoFormat("Try transaction: tryCount = {0}  ErrorNumber = {1}  ErrorMessage = {2}", tryCount, ex.Number, ex.Message);
					if (_priceInfo.IsUpdating)
						_stats.ResetCountersForUpdate();

					Thread.Sleep(10000 + tryCount * 1000);
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			} while (!done);

			if (tryCount > _loggingStat.maxLockCount)
				_loggingStat.maxLockCount = tryCount;
		}

		private IEnumerable<int> PrepareData(/*byte[] buffer, */Action<string, int> populateCommand)
		{
			var MaxPacketSize = 500*1024;
			var MaxCommandCount = 10;
			var index = 0;
			var populatedBytes = 0;
			foreach (var command in BuildSql().Where(c => !String.IsNullOrEmpty(c)))
			{
				if (_logger.IsDebugEnabled)
					_logger.Debug(command);

				if (command.Length > MaxPacketSize)
					throw new Exception(String.Format("Длинна комманда {0} превыщает максимальный размер пакета {1}", command.Length, MaxPacketSize));

				if (index > MaxCommandCount || populatedBytes + command.Length > MaxPacketSize)
				{
					yield return populatedBytes;
					populatedBytes = 0;
					index = 0;
				}
				populateCommand(command, populatedBytes);
				populatedBytes += command.Length;
				index++;
			}
			if (populatedBytes > 0)
				yield return populatedBytes;
			yield break;
		}

		private void Maintain(MySqlTransaction finalizeTransaction, StringBuilder logMessage)
		{
			var cleanUpCommand = new MySqlCommand {
				Connection = _connection,
				CommandTimeout = 0
			};
			cleanUpCommand.CommandText = String.Format("delete from farm.Zero where PriceItemId={0}", priceItemId);
			logMessage.AppendFormat("DelFromZero={0}  ", StatCommand(cleanUpCommand));

			cleanUpCommand.CommandText = String.Format("delete from farm.Forb where PriceItemId={0}", priceItemId);
			logMessage.AppendFormat("DelFromForb={0}  ", StatCommand(cleanUpCommand));

			var daBlockedPrice = new MySqlDataAdapter(String.Format("SELECT * FROM farm.blockedprice where PriceItemId={0} limit 1", priceItemId), _connection);
			daBlockedPrice.SelectCommand.Transaction = finalizeTransaction;
			var dtBlockedPrice = new DataTable();
			daBlockedPrice.Fill(dtBlockedPrice);

			if (dtBlockedPrice.Rows.Count == 0)
			{
				cleanUpCommand.CommandText = String.Format("delete from farm.UnrecExp where PriceItemId={0}", priceItemId);
				logMessage.AppendFormat("DelFromUnrecExp={0}  ", StatCommand(cleanUpCommand));
			}

			logMessage.AppendFormat("UpdateForb={0}  ", TryUpdate(daForb, dtForb.Copy(), finalizeTransaction));
			logMessage.AppendFormat("UpdateZero={0}  ", TryUpdate(daZero, dtZero.Copy(), finalizeTransaction));
			logMessage.AppendFormat("UpdateUnrecExp={0}  ", UnrecExpUpdate(finalizeTransaction));
			//Исключения обновляем после нераспознанных, т.к. все может измениться
			logMessage.AppendFormat("UpdateExcludes={0}  ", TryUpdate(daExcludes, dtExcludes.Copy(), finalizeTransaction));
			logMessage.AppendFormat("UpdateAssortment={0}", TryUpdate(daAssortment, dtAssortment.Copy(), finalizeTransaction));

			//Производим обновление PriceDate и LastFormalization в информации о формализации
			//Если прайс-лист загружен, то обновляем поле PriceDate, если нет, то обновляем данные в intersection_update_info
			cleanUpCommand.Parameters.Clear();
			if (downloaded)
			{
				cleanUpCommand.CommandText = String.Format(
					"UPDATE usersettings.PriceItems SET RowCount={0}, PriceDate=now(), LastFormalization=now(), UnformCount={1} WHERE Id={2};", _loggingStat.formCount, _loggingStat.unformCount, priceItemId);
			}
			else
			{
				cleanUpCommand.CommandText = String.Format(
					"UPDATE usersettings.PriceItems SET RowCount={0}, LastFormalization=now(), UnformCount={1} WHERE Id={2};", _loggingStat.formCount, _loggingStat.unformCount, priceItemId);
			}
			cleanUpCommand.CommandText += String.Format(@"
UPDATE usersettings.AnalitFReplicationInfo A, usersettings.PricesData P
SET
  a.ForceReplication = 1
where
    p.PriceCode = {0}
and a.FirmCode = p.FirmCode;", _priceInfo.PriceCode);

			logMessage.AppendFormat("UpdatePriceItemsAndIntersections={0}  ", StatCommand(cleanUpCommand));

			_logger.InfoFormat("Statistica: {0}", logMessage.ToString());
			_logger.DebugFormat(
				"Statistica search: assortment = {0} excludes = {1}  during assortment = {2} during excludes = {3}",
				(assortmentSearchCount > 0) ? assortmentSearchWatch.ElapsedMilliseconds / assortmentSearchCount : 0,
				(excludesSearchCount > 0) ? excludesSearchWatch.ElapsedMilliseconds / excludesSearchCount : 0,
				assortmentSearchWatch.ElapsedMilliseconds,
				excludesSearchWatch.ElapsedMilliseconds);
		}

		private string UnrecExpUpdate(MySqlTransaction finalizeTransaction)
		{
			DateTime startTime = DateTime.UtcNow;
			TimeSpan workTime;
			int applyCount = 0;

			daUnrecExp.SelectCommand.Transaction = finalizeTransaction;
			try
			{
				foreach (DataRow drUnrecExp in dtUnrecExp.Rows)
				{
					var drsProducerSynonyms = dtNewSynonymFirmCr.Select("OriginalSynonym = '" + drUnrecExp["FirmCr"].ToString().Replace("'", "''") + "'");

					if ((drsProducerSynonyms.Length == 0) && !Convert.IsDBNull(drUnrecExp["InternalProducerSynonymId"]))
						throw new Exception(String.Format("Не нашли новых синонимов хотя ссылка существует: {0}  {1}", drUnrecExp["FirmCr"], drUnrecExp));
					else
						if (drsProducerSynonyms.Length == 1)
						{
							drUnrecExp["ProducerSynonymId"] = drsProducerSynonyms[0]["SynonymFirmCrCode"];
							//Если найденный синоним новый и был обновлен при сохранении прайс-листа в базу
							//и если не сбрасывали ссылку на новый синоним
							if ((drsProducerSynonyms[0].RowState == DataRowState.Unchanged) && !Convert.IsDBNull(drUnrecExp["InternalProducerSynonymId"]))
							{
								drUnrecExp["InternalProducerSynonymId"] = DBNull.Value;
								//Если синоним не автоматический, то будем выставлять CodeFirmCr
								if (!Convert.ToBoolean(drsProducerSynonyms[0]["IsAutomatic"]))
								{
									//Если CodeFirmCr не установлен, то синоним производителя сопоставлен с "производитель не известен"
									if (Convert.IsDBNull(drsProducerSynonyms[0]["CodeFirmCr"]))
									{
										if (!Convert.IsDBNull(drUnrecExp["PriorProductId"]))
										{
											//Если сопоставлено по наименованию, то она полностью сопоставлена и удаляем из нераспознанных
											drUnrecExp["Already"] = (byte)UnrecExpStatus.FullForm;
											drUnrecExp["Status"] = (byte)UnrecExpStatus.FullForm;
											continue;
										}
										else
										{
											drUnrecExp["Already"] = (byte)UnrecExpStatus.FirmForm;
											drUnrecExp["Status"] = (byte)UnrecExpStatus.FirmForm;
										}
									}
									else
									{
										if (Convert.IsDBNull(drUnrecExp["PriorProductId"]))
										{
											drUnrecExp["PriorProducerId"] = drsProducerSynonyms[0]["CodeFirmCr"];
											drUnrecExp["Already"] = (byte)((UnrecExpStatus)((byte)drUnrecExp["Already"]) | UnrecExpStatus.FirmForm);
											drUnrecExp["Status"] = (byte)((UnrecExpStatus)((byte)drUnrecExp["Status"]) | UnrecExpStatus.FirmForm);
										}
										else
										{
											drUnrecExp["PriorProducerId"] = drsProducerSynonyms[0]["CodeFirmCr"];
											var CatalogId = Convert.ToInt64(MySqlHelper.ExecuteScalar(
												_connection,
												"select CatalogId from catalogs.products p where Id = ?Productid",
												new MySqlParameter("?Productid", drUnrecExp["PriorProductId"])));
											long? synonymId = null;
											if (!Convert.IsDBNull(drUnrecExp["ProductSynonymId"]))
												synonymId = Convert.ToInt64(drUnrecExp["ProductSynonymId"]);
											var status = GetAssortmentStatus(CatalogId, Convert.ToInt64(drUnrecExp["PriorProducerId"]), Convert.ToInt64(drUnrecExp["ProducerSynonymId"]), synonymId);
											drUnrecExp["Already"] = (byte)(UnrecExpStatus.NameForm | UnrecExpStatus.FirmForm | status);
											drUnrecExp["Status"] = (byte)(UnrecExpStatus.NameForm | UnrecExpStatus.FirmForm | status);
											continue;
										}

									}
								}
							}
						}
						else
							if (drsProducerSynonyms.Length > 1)
								throw new Exception(String.Format("Получили новых синонимов больше 1: {0}  {1}", drUnrecExp["FirmCr"], drUnrecExp));

					//Если не получилось, что позиция из-за вновь созданных синонимов была полностью распознана, то обновляем ее в базе
					if ((((UnrecExpStatus)((byte)drUnrecExp["Status"]) & UnrecExpStatus.FullForm) != UnrecExpStatus.FullForm) &&
						(((UnrecExpStatus)((byte)drUnrecExp["Status"]) & UnrecExpStatus.ExcludeForm) != UnrecExpStatus.ExcludeForm))
					{
						daUnrecExp.Update(new DataRow[] { drUnrecExp });
						applyCount++;
					}
				}
			}
			finally
			{
				workTime = DateTime.UtcNow.Subtract(startTime);
			}
			return String.Format("{0};{1}", applyCount, workTime);
		}

		private void InsertNewProducerSynonyms(MySqlTransaction finalizeTransaction)
		{
			daSynonymFirmCr.InsertCommand.Connection = _connection;
			daSynonymFirmCr.InsertCommand.Transaction = finalizeTransaction;

			dtNewSynonymFirmCr = null;
			dtSynonymFirmCr.DefaultView.RowFilter = "InternalProducerSynonymId is not null";
			dtNewSynonymFirmCr = dtSynonymFirmCr.DefaultView.ToTable();

			if (!_stats.CanCreateProducerSynonyms())
				return;

			foreach (DataRow drNewProducerSynonym in dtNewSynonymFirmCr.Rows)
			{
				if (!Convert.IsDBNull(drNewProducerSynonym["SynonymFirmCrCode"]))
					//Если код синонима производителя существует, то он был создан не PriceProcessor и 
					//получен из базы при сохранении прайса
					drNewProducerSynonym.AcceptChanges();
				else
				{
					var dsExistsProducerSynonym = MySqlHelper.ExecuteDataset(_connection, @"
SELECT
  SynonymFirmCrCode,
  CodeFirmCr,
  LOWER(Synonym) AS Synonym,
  (aps.ProducerSynonymId is not null) as IsAutomatic
FROM
  farm.SynonymFirmCr
  left join farm.AutomaticProducerSynonyms aps on aps.ProducerSynonymId = SynonymFirmCr.SynonymFirmCrCode
WHERE 
    (SynonymFirmCr.PriceCode = ?PriceCode)
and (SynonymFirmCr.Synonym = ?OriginalSynonym)"
						,
						new MySqlParameter("?PriceCode", parentSynonym),
						new MySqlParameter("?OriginalSynonym", drNewProducerSynonym["OriginalSynonym"]));
					if ((dsExistsProducerSynonym.Tables.Count == 1) && (dsExistsProducerSynonym.Tables[0].Rows.Count == 1))
					{
						//Если уже синоним существует, то обноляем его у себя
						drNewProducerSynonym["SynonymFirmCrCode"] = dsExistsProducerSynonym.Tables[0].Rows[0]["SynonymFirmCrCode"];
						drNewProducerSynonym["CodeFirmCr"] = dsExistsProducerSynonym.Tables[0].Rows[0]["CodeFirmCr"];
						drNewProducerSynonym["IsAutomatic"] = dsExistsProducerSynonym.Tables[0].Rows[0]["IsAutomatic"];
						drNewProducerSynonym.AcceptChanges();
						var drExistsProducerSynonym = dtSynonymFirmCr.Select("InternalProducerSynonymId = " + drNewProducerSynonym["InternalProducerSynonymId"])[0];
						drExistsProducerSynonym["SynonymFirmCrCode"] = drNewProducerSynonym["SynonymFirmCrCode"];
						drExistsProducerSynonym["CodeFirmCr"] = drNewProducerSynonym["CodeFirmCr"];
						drExistsProducerSynonym["IsAutomatic"] = drNewProducerSynonym["IsAutomatic"];
						drExistsProducerSynonym.AcceptChanges();
					}
					else
					{ 
						daSynonymFirmCr.InsertCommand.Parameters["?PriceCode"].Value = parentSynonym;
						daSynonymFirmCr.InsertCommand.Parameters["?OriginalSynonym"].Value = drNewProducerSynonym["OriginalSynonym"];
						drNewProducerSynonym["SynonymFirmCrCode"] = Convert.ToInt64(daSynonymFirmCr.InsertCommand.ExecuteScalar());
					}
				}
			}
		}

		/// <summary>
		/// анализируем цены и формируем список, если ценовая колонка имеет более 5% позиций с неустановленной ценой
		/// </summary>
		private void ProcessUndefinedCost()
		{
			var stringBuilder = new StringBuilder();
			foreach (var cost in _costDescriptions)
				if (cost.UndefinedCostCount > _loggingStat.formCount * 0.05)
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
				new MySqlParameter("?PriceItemId", priceItemId));
			subject = String.Format(subject, drProvider["PriceName"], drProvider["ShortFirmName"]);
			body = String.Format(
				body,
				drProvider["PriceName"],
				drProvider["ShortFirmName"],
				stringBuilder);

			_logger.DebugFormat("Сформировали предупреждение о настройках формализации прайс-листа: {0}", body);
			Mailer.SendUserFail(subject, body);
		}

		public void Open()
		{
			dtPrice = _parser.Parse(priceFileName);
			//Проверку и отправку уведомлений производим только для загруженных прайс-листов
			if (!downloaded)
				return;
			var sb = new StringBuilder();

			foreach (PriceFields pf in Enum.GetValues(typeof(PriceFields)))
				if ((pf != PriceFields.OriginalName) && !String.IsNullOrEmpty(GetFieldName(pf)) && !dtPrice.Columns.Contains(GetFieldName(pf)))
					sb.AppendFormat("\"{0}\" настроено на {1}\n", GetDescription(pf), GetFieldName(pf));

			
			foreach (var cost in _costDescriptions)
				if (!String.IsNullOrEmpty(cost.FieldName) && !dtPrice.Columns.Contains(cost.FieldName))
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
		}

		protected static string GetDescription(PriceFields value)
		{
			var descriptions = value.GetType().GetField(value.ToString()).GetCustomAttributes(false);
			return ((System.ComponentModel.DescriptionAttribute)descriptions[0]).Description;
		}

		/// <summary>
		/// Формализование прайса
		/// </summary>
		public void Formalize()
		{
			using(NDC.Push(String.Format("{0}.{1}", _priceInfo.PriceCode, _priceInfo.CostCode)))
			{
				_logger.Debug("начало Formalize");

				using (Timer("Чтение прайс листа"))
					Open();

				if (dtPrice.Rows.Count == 0)
					throw new FormalizeException("В полученом прайс листе не удалось найти ни одной позиции", _priceInfo);

				try
				{
					_connection.Open();

					using(Timer("Загрузка данных"))
						Prepare();

					using(Timer("Формализация"))
						InternalFormalize();

					using (Timer("Применение изменений в базу"))
						FinalizePrice();
				}
				finally
				{
					_connection.Close();
				}
				_logger.Debug("конец Formalize");
			}
		}

		public IDisposable Timer(string message)
		{
			var watch = Stopwatch.StartNew();
			return new DisposibleAction(() => {
				watch.Stop();
				_logger.InfoFormat("{0}, {1}с", message, watch.Elapsed.TotalSeconds);
			});
		}

		private void InternalFormalize()
		{
			if (null != toughMask)
				toughMask.Analyze(GetFieldRawValue(PriceFields.Name1));

			do
			{
				var posName = GetFieldValue(PriceFields.Name1, true);

				if (String.IsNullOrEmpty(posName))
					continue;

				if (IsForbidden(posName))
				{
					InsertIntoForb(posName);
					continue;
				}

				Cost[] costs = null;
				if (!_priceInfo.IsAssortmentPrice)
				{
					costs = ProcessCosts(_costDescriptions);
					var currentQuantity = GetFieldValueObject(PriceFields.Quantity);

					//Если кол-во ненулевых цен = 0, то тогда производим вставку в Zero
					//или если количество определенно и оно равно 0
					if (costs.Length == 0 || (currentQuantity is int && (int)currentQuantity == 0))
					{
						InsertToZero();
						continue;
					}
				}

				var position = new FormalizationPosition {
					PositionName = posName,
					Code = GetFieldValue(PriceFields.Code),
					OriginalName = GetFieldValue(PriceFields.OriginalName, true),
					FirmCr = GetFieldValue(PriceFields.FirmCr, true)
				};

				GetProductId(position);
				GetCodeFirmCr(position);

				//Если не получили CodeFirmCr, то считаем, 
				//что позиция формализована по ассортименту, т.к. производитель не опознан и проверить по ассортименту нельзя
				/*
				Возможны ситуации:
				  UnrecExpStatus.NameForm UnrecExpStatus.FirmForm UnrecExpStatus.AssortmentForm
				  UnrecExpStatus.NameForm                         UnrecExpStatus.AssortmentForm
				*/
				//проверям ассортимент
				if (position.ProductId.HasValue && position.CodeFirmCr.HasValue)
					GetAssortmentStatus(position);

				//Получается, что если формализовали по наименованию, то это позиция будет отображена клиенту
				if (position.IsSet(UnrecExpStatus.NameForm))
				{
					if (!position.IsHealth())
						throw new Exception(String.Format("Не верное состояние формализуемой позиции {0}, программист допустил ошибку", position.PositionName));

					InsertToCore(position, costs);
				}
				else
					_loggingStat.unformCount++;

				if (position.IsNotSet(UnrecExpStatus.FullForm) && position.IsNotSet(UnrecExpStatus.ExcludeForm))
					InsertToUnrec(position);

			}
			while (Next());
		}

		/// <summary>
		/// Установить название поля, которое будет считано из набора данных
		/// </summary>
		public void SetFieldName(PriceFields PF, string Value)
		{
			FieldNames[(int)PF] = Value;
		}

		/// <summary>
		/// Получить название поля
		/// </summary>
		public string GetFieldName(PriceFields PF)
		{
			return FieldNames[(int)PF];
		}

		/// <summary>
		/// Перейти на следующую позици набора данных
		/// </summary>
		/// <returns>Удачно ли выполнен переход?</returns>
		public virtual bool Next()
		{
			CurrPos++;
			if (CurrPos < dtPrice.Rows.Count)
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

				var value = dtPrice.Rows[CurrPos][GetFieldName(field)].ToString();
				if (convertedToANSI)
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
				if (dtPrice.Columns.IndexOf(GetFieldName(PriceFields.Name2) ) > -1)
					res = UnSpace(String.Format("{0} {1}", res, RemoveForbWords(GetFieldRawValue(PriceFields.Name2))));
				if (dtPrice.Columns.IndexOf(GetFieldName(PriceFields.Name3) ) > -1)
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
		/// <param name="Value"></param>
		/// <returns></returns>
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
		/// <param name="Value"></param>
		/// <returns></returns>
		public string RemoveForbWords(string Value)
		{
			if (null != Value)
			{
				if (String.Empty != forbWords)
				{
					foreach(string s in forbWordsList)
					{
						Value = Value.Replace(s, "");
					}
					if (String.Empty == Value)
						return null;
					return Value;
				}
				return Value;
			}
			return null;
		}

		//Содержится ли название в таблице запрещенных слов
		public bool IsForbidden(string PosName)
		{
			DataRow[] dr = dtForbidden.Select(String.Format("Forbidden = '{0}'", PosName.Replace("'", "''")));
			return dr.Length > 0;
		}

		//Смогли ли мы распознать позицию по коду, имени и оригинальному названию?
		public void GetProductId(FormalizationPosition position)
		{
			DataRow[] dr = null;
			if (formByCode)
			{
				if (!String.IsNullOrEmpty(position.Code))
					dr = dtSynonym.Select(String.Format("Code = '{0}'", position.Code.Replace("'", "''")));
			}
			else
			{
				if (!String.IsNullOrEmpty(position.PositionName))
					dr = dtSynonym.Select(String.Format("Synonym = '{0}'", position.PositionName.Replace("'", "''")));
				if ((null == dr) || (0 == dr.Length))
					if (!String.IsNullOrEmpty(position.OriginalName))
						dr = dtSynonym.Select(String.Format("Synonym = '{0}'", position.OriginalName.Replace("'", "''")));
			}

			if ((null != dr) && (dr.Length > 0))
			{
				position.ProductId = Convert.ToInt64(dr[0]["ProductId"]);
				position.CatalogId = Convert.ToInt64(dr[0]["CatalogId"]);
				position.SynonymCode = Convert.ToInt64(dr[0]["SynonymCode"]);
				position.Junk = Convert.ToBoolean(dr[0]["Junk"]);
				position.AddStatus(UnrecExpStatus.NameForm);
			}
		}

		public void GetAssortmentStatus(FormalizationPosition position)
		{
			var assortmentStatus = GetAssortmentStatus(position.CatalogId, position.CodeFirmCr, position.SynonymFirmCrCode, position.SynonymCode);
			//Если получили исключение, то сбрасываем CodeFirmCr
			if (assortmentStatus == UnrecExpStatus.MarkExclude)
				position.CodeFirmCr = null;
			position.AddStatus(assortmentStatus);
		}

		public UnrecExpStatus GetAssortmentStatus(long? CatalogId, long? ProducerId, long? ProducerSynonymId, long? synonymId)
		{
			DataRow[] dr;

			assortmentSearchWatch.Start();
			try
			{
				dr = dtAssortment.Select(
					String.Format("CatalogId = {0} and ProducerId = {1}",
						CatalogId,
						ProducerId));
				assortmentSearchCount++;
			}
			finally
			{
				assortmentSearchWatch.Stop();
			}

			if (dr != null && dr.Length == 1)
				return UnrecExpStatus.AssortmentForm;

			excludesSearchWatch.Start();
			try
			{
				dr = dtExcludes.Select(
					String.Format("CatalogId = {0} and ProducerSynonymId = {1}",
						CatalogId,
						ProducerSynonymId));
				excludesSearchCount++;
			}
			finally
			{
				excludesSearchWatch.Stop();
			}

			//Если мы ничего не нашли, то добавляем в исключение
			if (dr == null || dr.Length == 0)
				CreateExcludeOrAssortment(CatalogId, ProducerId, ProducerSynonymId, synonymId);

			return UnrecExpStatus.MarkExclude;
		}

		private void CreateExcludeOrAssortment(long? catalogId, long? producerId, long? producerSynonymId, long? synonymId)
		{
			if (CanCreateAssortment(catalogId, producerId))
			{
				var assortment = dtAssortment.NewRow();
				assortment["CatalogId"] = catalogId;
				assortment["ProducerId"] = producerId;
				assortment["Checked"] = false;
				dtAssortment.Rows.Add(assortment);
			}
			else
			{
				try
				{
					var drExclude = dtExcludes.NewRow();
					drExclude["PriceCode"] = parentSynonym;
					drExclude["CatalogId"] = catalogId;
					drExclude["ProducerSynonymId"] = producerSynonymId;
					drExclude["OriginalSynonymId"] = synonymId;
					dtExcludes.Rows.Add(drExclude);
				}
				catch (ConstraintException)
				{
				}
			}
		}

		private bool CanCreateAssortment(long? catalogId, long? producerId)
		{
			var query = String.Format(@"
select count(*) FROM catalogs.Assortment A
  join catalogs.Producers P on P.Id = {1}
where CatalogId = {0} and (A.Checked = 1 or P.Checked = 1)", catalogId, producerId);
			var count = Convert.ToInt32(MySqlHelper.ExecuteScalar(_connection, query));
			return (count == 0);
		}

		//Смогли ли мы распознать производителя по названию?
		public void GetCodeFirmCr(FormalizationPosition position)
		{
			if (String.IsNullOrEmpty(position.FirmCr))
			{
				//Если в производителе ничего не написано, то устанавливаем все в null, и говорим, что распознано по производителю
				position.AddStatus(UnrecExpStatus.FirmForm);
			}
			else
			{
				var dr = dtSynonymFirmCr.Select(String.Format("Synonym = '{0}'", position.FirmCr.Replace("'", "''")));
				if ((null != dr) && (dr.Length > 0))
				{
					//Если значение CodeFirmCr не установлено, то устанавливаем в null, иначе берем значение кода
					position.CodeFirmCr = Convert.IsDBNull(dr[0]["CodeFirmCr"]) ? null : (long?)Convert.ToInt64(dr[0]["CodeFirmCr"]);
					position.IsAutomaticProducerSynonym = Convert.ToBoolean(dr[0]["IsAutomatic"]);
					if (Convert.IsDBNull(dr[0]["InternalProducerSynonymId"]))
					{
						_stats.ProducerSynonymUsedExistCount++;
						position.SynonymFirmCrCode = Convert.ToInt64(dr[0]["SynonymFirmCrCode"]);
					}
					else
						position.InternalProducerSynonymId = Convert.ToInt64(dr[0]["InternalProducerSynonymId"]);

					if (!position.IsAutomaticProducerSynonym)
						position.AddStatus(UnrecExpStatus.FirmForm);
				}
				else
				{
					//Если позиция распознано по наименованию, то добавляем автоматический синоним, если его нет
					if (position.IsSet(UnrecExpStatus.NameForm))
					{
						position.IsAutomaticProducerSynonym = true;
						position.InternalProducerSynonymId = InsertSynonymFirm(GetFieldValue(PriceFields.FirmCr, false));
					}
				}

			}
		}

		private long InsertSynonymFirm(string firmCr)
		{
			var drInsert = dtSynonymFirmCr.NewRow();
			drInsert["CodeFirmCr"] = DBNull.Value;
			drInsert["SynonymFirmCrCode"] = DBNull.Value;
			drInsert["IsAutomatic"] = 1;
			drInsert["Synonym"] = firmCr.ToLower();
			drInsert["OriginalSynonym"] = firmCr.Trim();
			dtSynonymFirmCr.Rows.Add(drInsert);
			_stats.ProducerSynonymCreatedCount++;
			return (long)drInsert["InternalProducerSynonymId"];
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
				
				var costValue = dtPrice.Rows[CurrPos][description.FieldName];
				var value = Cost.Parse(costValue);

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