using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Text;
using Inforoom.PriceProcessor;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using Inforoom.PriceProcessor.Properties;
using System.ComponentModel;
using System.Diagnostics;
using log4net;

namespace Inforoom.Formalizer
{

	//Исключение, которое генерируется парсерами во время работы
	public class FormalizeException : Exception
	{
		public readonly long clientCode = -1;
		public readonly long priceCode = -1;
		public readonly string clientName = "";
		public readonly string priceName = "";

		public FormalizeException(string message) : base(message)
		{}

		public FormalizeException(string message, Int64 ClientCode, Int64 PriceCode, string ClientName, string PriceName) 
			: base(message)
		{
			clientCode = ClientCode;
			priceCode = PriceCode;
			clientName = ClientName;
			priceName = PriceName;
		}

		public string FullName
		{
			get
			{
				return String.Format("{0} ({1})", clientName, priceName);
			}
		}
	}

	//Исключение, которые требуют вмешательства людей, но не являются критическими
	public class WarningFormalizeException : FormalizeException
	{

		public WarningFormalizeException(string message) : base(message)
		{}

		public WarningFormalizeException(string message, Int64 ClientCode, Int64 PriceCode, string ClientName, string PriceName) 
			: base(message, ClientCode, PriceCode, ClientName, PriceName)
		{}
	}

	//Исключения, которые возникают из-за откатов прайса
	public class RollbackFormalizeException : WarningFormalizeException
	{
		public readonly int FormCount = -1;
		//Кол-во "нулей"
		public readonly int ZeroCount = -1;
		//Кол-во нераспознанных событий
		public readonly int UnformCount = -1;
		//Кол-во "запрещенных" позиций
		public readonly int ForbCount = -1;

		public RollbackFormalizeException(string message) : base(message)
		{}
		
		public RollbackFormalizeException(string message, Int64 ClientCode, Int64 PriceCode, string ClientName, string PriceName) : base(message, ClientCode, PriceCode, ClientName, PriceName)
		{}

		public RollbackFormalizeException(string message, Int64 ClientCode, Int64 PriceCode, string ClientName, string PriceName, int FormCount, int ZeroCount, int UnformCount, int ForbCount) 
			: base(message, ClientCode, PriceCode, ClientName, PriceName)
		{
			this.FormCount = FormCount;
			this.ZeroCount = ZeroCount;
			this.UnformCount = UnformCount;
			this.ForbCount = ForbCount;
		}
	}

	//Все возможные поля прайса
	public enum PriceFields : int 
	{
		[Description("Код")]
		Code,
		[Description("Код производителя")]
		CodeCr,
		[Description("Наименование 1")]
		Name1,
		[Description("Наименование 2")]
		Name2,
		[Description("Наименование 3")]
		Name3,
		[Description("Производитель")]
		FirmCr,
		[Description("Единица измерения")]
		Unit,
		[Description("Цеховая упаковка")]
		Volume,
		[Description("Количество")]
		Quantity,
		[Description("Примечание")]
		Note,
		[Description("Срок годности")]
		Period,
		[Description("Документ")]
		Doc,
		[Description("Цена минимальная")]
		MinBoundCost,
		[Description("Срок")]
		Junk,
		[Description("Ожидается")]
		Await,
		[Description("Оригинальное наименование")]
		OriginalName,
		[Description("Жизненно важный")]
		VitallyImportant,
		[Description("Кратность")]
		RequestRatio,
		[Description("Реестровая цена")]
		RegistryCost,
		[Description("Цена максимальная")]
		MaxBoundCost,
		[Description("Минимальная сумма")]
		OrderCost,
		[Description("Минимальное количество")]
		MinOrderCount
	}

	public enum CostTypes : int
	{ 
		MultiColumn = 0,
		MiltiFile = 1
	}

	//Статистические счетчики для формализации
	public enum FormalizeStats : int
	{ 
		//найдены по первичным полям
		FirstSearch,
		//найдены по остальным полям
		SecondSearch,
		//кол-во обновленных записей
		UpdateCount,
		//кол-во вставленных записей
		InsertCount,
		//кол-во удаленных записей
		DeleteCount,
		//кол-во обновленных цен
		UpdateCostCount,
		//кол-во добавленных цен
		InsertCostCount,
		//кол-во удаленных цен, не считаются цены, которые были удалены из удаления позиции из Core
		DeleteCostCount,
		//общее кол-во SQL-команд при обновлении прайс-листа
		CommandCount,
		//Среднее время поиска в миллисекундах записи в существующем прайсе
		AvgSearchTime
	}

	[Flags]
	public enum UnrecExpStatus : int 
	{
		NOT_FORM	= 0,		// Неформализованный
		NAME_FORM	= 1,		// Формализованный по названию
		FIRM_FORM	= 2,		// Формализованный по производителю
		CURR_FORM	= 4,		// Формализованный по валюте
		FULL_FORM	= 7,		// Полностью формализован
		MARK_FORB	= 8,		// Помеченый как запрещенное
		MARK_DEL	= 16		// Помеченый как удаленный
	}

	//Класс содержит название полей из таблицы FormRules
	public sealed class FormRules
	{
		public static string colParserClassName = "ParserClassName";
		public static string colSelfPriceName = "SelfPriceName";
		public static string colFirmShortName = "FirmShortName";
		public static string colFirmCode = "FirmCode";
		public static string colFormByCode = "FormByCode";
		public static string colPriceCode = "PriceCode";
		public static string colPriceItemId = "PriceItemId";
		public static string colCostCode = "CostCode";
		public static string colParentSynonym = "ParentSynonym";
		public static string colNameMask = "NameMask";
		public static string colForbWords = "ForbWords";
		public static string colSelfAwaitPos = "SelfAwaitPos";
		public static string colSelfJunkPos = "SelfJunkPos";
		public static string colSelfVitallyImportantMask = "SelfVitallyImportantMask";
		public static string colPrevRowCount = "RowCount";
		public static string colPriceType = "PriceType";
		public static string colDelimiter = "Delimiter";
		public static string colBillingStatus = "BillingStatus";
		public static string colFirmStatus = "FirmStatus";
		public static string colCostType = "CostType";
	}

	//
	public class CoreCost : ICloneable
	{
		public System.Int64 costCode = -1;
		public bool baseCost = false;
		public string costName = String.Empty;
		public string fieldName = String.Empty;
		public int txtBegin = -1;
		public int txtEnd = -1;
		public decimal? cost = null;
		//кол-во позиций с неустановленной ценой для данной ценовой колонки
		public int undefinedCostCount = 0;

		public CoreCost(System.Int64 ACostCode, string ACostName, bool ABaseCost, string AFieldName, int ATxtBegin, int ATxtEnd)
		{
			costCode = ACostCode;
			baseCost = ABaseCost;
			costName = ACostName;
			fieldName = AFieldName;
			txtBegin = ATxtBegin;
			txtEnd = ATxtEnd;
		}

		#region ICloneable Members

		public object Clone()
		{
			CoreCost ccNew = new CoreCost(this.costCode, this.costName, this.baseCost, this.fieldName, this.txtBegin, this.txtEnd);
			ccNew.cost = this.cost;
			return ccNew;
		}

		#endregion
	}

	/// <summary>
	/// Summary description for BasePriceParser.
	/// </summary>
	public abstract class BasePriceParser
	{
		//таблица с прайсом
		protected DataTable dtPrice;

		//Соедиение с базой данных
		protected MySqlConnection MyConn;

		//Таблица со списком запрещенных названий
		protected MySqlDataAdapter daForbidden;
		protected DataTable dtForbidden;
		//Таблица со списком синонимов товаров
		protected MySqlDataAdapter daSynonym;
		protected DataTable dtSynonym;
		//Таблица со списоком синонимов производителей
		protected MySqlDataAdapter daSynonymFirmCr;
		protected DataTable dtSynonymFirmCr;

		protected MySqlDataAdapter daCore;
		protected DataTable dtCore;
		protected MySqlDataAdapter daUnrecExp;
		protected MySqlCommandBuilder cbUnrecExp;
		protected DataTable dtUnrecExp;
		protected MySqlDataAdapter daZero;
		protected MySqlCommandBuilder cbZero;
		protected DataTable dtZero;
		protected MySqlDataAdapter daForb;
		protected MySqlCommandBuilder cbForb;
		protected DataTable dtForb;
		protected MySqlDataAdapter daCoreCosts;
		protected DataTable dtCoreCosts;

		protected MySqlDataAdapter daExistsCore;
		protected DataTable dtExistsCore;

		protected MySqlDataAdapter daExistsCoreCosts;
		protected DataTable dtExistsCoreCosts;

		protected DataRelation relationExistsCoreToCosts;

		protected DataSet dsMyDB;

		protected string[] FieldNames;

		protected int CurrPos = -1;

		//Кол-во успешно формализованных
		public int formCount = 0;
		//Кол-во "нулей"
		public int zeroCount = 0;
		//Кол-во нераспознанных событий
		public int unformCount = 0;
		//Кол-во нераспознаных по всей форме
		public int unrecCount = 0;
		//Кол-во "запрещенных" позиций
		public int forbCount = 0;

		//Максимальное кол-во рестартов транзакций при применении прайс-листа в базу данных
		public int maxLockCount = 0;

		protected string priceFileName;

		//FormalizeSettings
		//имя прайса
		public string	priceName;
		//Имя клиента
		public string	firmShortName;
		//Код клиента
		public long	firmCode;
		//ключ прайса
		public long		priceCode = -1;
		//код ценовой колонки, может быть не установлен
		public long?	costCode;

		//список прайс-листов, на которых в тестовом режиме будет использоваться update
		private List<long> priceCodesUseUpdate;

		//список первичных полей, которые будут участвовать в сопоставлении позиций в прайсах
		private List<string> primaryFields;

		//Список полей из Core, по которых происходит вторичное сравнение
		private List<string> compareFields;

		private Dictionary<FormalizeStats, int> statCounters;


		//Является ли обрабатываемый прайс-лист загруженным?
		public bool downloaded = false;

		//ключ для priceitems
		public long priceItemId;
		//индекс цены с таким же кодом как у прайса в списке цен (currentCoreCosts)
		public int				priceCodeCostIndex = -1;
		//родительский синоним : прайс-родитель, нужен для выбора различных параметров
		protected long		parentSynonym;
		//Кол-во распознаных позиций в прошлый раз
		protected long		prevRowCount;
		//производить формализацию по коду
		protected bool				formByCode;

		//Маска, которая накладывается на имя позиции
		protected string nameMask;
		//Запрещенные слова, которые могут быть в имени
		protected string forbWords;
		protected string[] forbWordsList = null;
		//как в прайсе поставщика метятся ожидаемые позиции
		protected string awaitPos;
		//как в прайсе поставщика метятся "плохие" позиции
		protected string junkPos;
		//как в прайсе поставщика метятся жизненно-важные позиции
		protected string vitallyImportantMask;
		//Тип прайса : ассортиментный
		protected int    priceType;
		//Тип ценовых колонок прайса-родителя: 0 - мультиколоночный, 1 - многофайловый
		protected CostTypes costType;

		//Надо ли конвертировать полученную строку в ANSI
		protected bool convertedToANSI = false;


		protected ToughDate toughDate = null;
		protected ToughMask toughMask = null;

		protected ArrayList currentCoreCosts = null;
		protected ArrayList CoreCosts = null;

		protected readonly ILog _logger;


		/// <summary>
		/// Конструктор парсера
		/// </summary>
		/// <param name="PriceFileName"></param>
		/// <param name="conn"></param>
		/// <param name="mydr"></param>
		public BasePriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr)
		{
			_logger = LogManager.GetLogger(this.GetType());
			_logger.DebugFormat("Создали класс для обработки файла {0}", PriceFileName);
			//TODO: Все необходимые проверки вынести в конструкторы, чтобы не пытаться открыть прайс-файл

			//TODO: переделать конструктор, чтобы он не зависел от базы данных, т.е. передавать ему все, что нужно для чтения файла, чтобы парсер был самодостаточным

			priceCodesUseUpdate = new List<long>();
			foreach (string syncPriceCode in Settings.Default.SyncPriceCodes)
				priceCodesUseUpdate.Add(Convert.ToInt64(syncPriceCode));
#if TESTINGUPDATE
			//Протек-15 (Акция_1) - Воронеж 
			//priceCodesUseUpdate.Add(5);
			//Катрен (Воронеж) -  Воронеж
			//priceCodesUseUpdate.Add(3779);
			//Протек-15 (Базовый_новый) - Воронеж 
			//priceCodesUseUpdate.Add(4596);			
#endif

			primaryFields = new List<string>();
			compareFields = new List<string>();
			foreach (string field in Settings.Default.CorePrimaryFields)
				primaryFields.Add(field);
//#if TESTINGUPDATE
//#endif

			statCounters = new Dictionary<FormalizeStats, int>();
			foreach (FormalizeStats statCounter in Enum.GetValues(typeof(FormalizeStats)))
				statCounters.Add(statCounter, 0);

			priceFileName = PriceFileName;
			dtPrice = new DataTable();
			MyConn = conn;
			dsMyDB = new DataSet();
			currentCoreCosts = new ArrayList();
			CoreCosts = new ArrayList();
			FieldNames = new string[Enum.GetNames(typeof(PriceFields)).Length];
			
			priceName = mydr.Rows[0][FormRules.colSelfPriceName].ToString();
			firmShortName = mydr.Rows[0][FormRules.colFirmShortName].ToString();
			firmCode = Convert.ToInt64(mydr.Rows[0][FormRules.colFirmCode]); 
			formByCode = Convert.ToBoolean(mydr.Rows[0][FormRules.colFormByCode]);
			priceItemId = Convert.ToInt64(mydr.Rows[0][FormRules.colPriceItemId]); 
			priceCode = Convert.ToInt64(mydr.Rows[0][FormRules.colPriceCode]);
			costCode = (mydr.Rows[0][FormRules.colCostCode] is DBNull) ? null : (long?)Convert.ToInt64(mydr.Rows[0][FormRules.colCostCode]);
			parentSynonym = Convert.ToInt64(mydr.Rows[0][FormRules.colParentSynonym]); 
			costType = (CostTypes)Convert.ToInt32(mydr.Rows[0][FormRules.colCostType]);

			nameMask = mydr.Rows[0][FormRules.colNameMask] is DBNull ? String.Empty : (string)mydr.Rows[0][FormRules.colNameMask];

			//Производим попытку разобрать строку с "запрещенными выражениями"
			forbWords = mydr.Rows[0][FormRules.colForbWords] is DBNull ? String.Empty : (string)mydr.Rows[0][FormRules.colForbWords];
			forbWords = forbWords.Trim();
			if (String.Empty != forbWords)
			{
				forbWordsList = forbWords.Split( new char[] {' '} );
				int len = 0;
				foreach(string s in forbWordsList)
					if(String.Empty != s)
						len++;
				if (len > 0)
				{
					string[] newForbWordList = new string[len];
					int i = 0;
					foreach(string s in forbWordsList)
						if(String.Empty != s)
						{
							newForbWordList[i] = s;
							i++;
						}
				}
				else
					forbWordsList = null;
			}

			awaitPos = mydr.Rows[0][FormRules.colSelfAwaitPos].ToString();
			junkPos  = mydr.Rows[0][FormRules.colSelfJunkPos].ToString();
			vitallyImportantMask = mydr.Rows[0][FormRules.colSelfVitallyImportantMask].ToString();
			prevRowCount = mydr.Rows[0][FormRules.colPrevRowCount] is DBNull ? 0 : Convert.ToInt64(mydr.Rows[0][FormRules.colPrevRowCount]);
			priceType = Convert.ToInt32(mydr.Rows[0][FormRules.colPriceType]);

			toughDate = new ToughDate();
			if (String.Empty != nameMask)
				toughMask = new ToughMask(nameMask, firmCode, priceCode, firmShortName, priceName);

			string selectCostFormRulesSQL = String.Empty;
			if (costType == CostTypes.MultiColumn)
				selectCostFormRulesSQL = String.Format("select * from usersettings.PricesCosts pc, farm.CostFormRules cfr where pc.PriceCode={0} and cfr.CostCode = pc.CostCode", priceCode);
			else
				selectCostFormRulesSQL = String.Format("select * from usersettings.PricesCosts pc, farm.CostFormRules cfr where pc.PriceCode={0} and cfr.CostCode = pc.CostCode and pc.CostCode = {1}", priceCode, costCode.Value);

			MySqlDataAdapter daPricesCost = new MySqlDataAdapter( selectCostFormRulesSQL, MyConn );
			DataTable dtPricesCost = new DataTable("PricesCosts");
			daPricesCost.Fill(dtPricesCost);
			_logger.DebugFormat("Загрузили цены {0}.{1}", priceCode, costCode);

			if ((0 == dtPricesCost.Rows.Count) && (Settings.Default.ASSORT_FLG != priceType))
				throw new WarningFormalizeException(Settings.Default.CostsNotExistsError, firmCode, priceCode, firmShortName, priceName);
			foreach(DataRow r in dtPricesCost.Rows)
				currentCoreCosts.Add(
					new CoreCost(
						Convert.ToInt64(r["CostCode"]),
						(string)r["CostName"],
						("1" == r["BaseCost"].ToString()),
						(string)r["FieldName"],
						(r["TxtBegin"] is DBNull) ? -1 : Convert.ToInt32(r["TxtBegin"]),
						(r["TxtEnd"] is DBNull) ? -1 : Convert.ToInt32(r["TxtEnd"])
					)
				);

			//Если прайс является не ассортиментным прайсом-родителем с мультиколоночными ценами, то его надо проверить на базовую цену
			if ((Settings.Default.ASSORT_FLG != priceType) && (costType == CostTypes.MultiColumn))
			{
				if (1 == currentCoreCosts.Count)
				{
					if ( !(currentCoreCosts[0] as CoreCost).baseCost )
						throw new WarningFormalizeException(Settings.Default.BaseCostNotExistsError, firmCode, priceCode, firmShortName, priceName);
				}
				else
				{
					CoreCost[] bc = Array.FindAll<CoreCost>((CoreCost[])currentCoreCosts.ToArray(typeof(CoreCost)), delegate(CoreCost cc) { return cc.baseCost; });
					if (bc.Length == 0)
					{
						throw new WarningFormalizeException(Settings.Default.BaseCostNotExistsError, firmCode, priceCode, firmShortName, priceName);
					}
					else
						if (bc.Length > 1)
						{
							throw new WarningFormalizeException(
								String.Format(Settings.Default.DoubleBaseCostsError,
									(bc[0] as CoreCost).costCode,
									(bc[1] as CoreCost).costCode),
								firmCode, priceCode, firmShortName, priceName);
						}
						else
						{
							currentCoreCosts.Remove(bc[0]);
							currentCoreCosts.Insert(0, bc[0]);
						}

				}

				if (((this is TXTFixedPriceParser) && (((currentCoreCosts[0] as CoreCost).txtBegin == -1) || ((currentCoreCosts[0] as CoreCost).txtEnd == -1))) || (!(this is TXTFixedPriceParser) && (String.Empty == (currentCoreCosts[0] as CoreCost).fieldName)))
					throw new WarningFormalizeException(Settings.Default.FieldNameBaseCostsError, firmCode, priceCode, firmShortName, priceName);

				priceCodeCostIndex = Array.FindIndex<CoreCost>((CoreCost[])currentCoreCosts.ToArray(typeof(CoreCost)), delegate(CoreCost cc) { return cc.costCode == priceCode; });
				if (priceCodeCostIndex == -1)
					priceCodeCostIndex = 0;
			}
		}

		/// <summary>
		/// Производит специализированное открытие прайса в зависимости от типа
		/// </summary>
		public abstract void Open();

		/// <summary>
		/// Производится вставка данных в таблицу Core
		/// </summary>
		/// <param name="AProductId"></param>
		/// <param name="AShortCode"></param>
		/// <param name="ACodeFirmCr"></param>
		/// <param name="ASynonymCode"></param>
		/// <param name="ASynonymFirmCrCode"></param>
		/// <param name="ABaseCost"></param>
		/// <param name="AJunk"></param>
		public void InsertToCore(long? AProductId, long? ACodeFirmCr, long? ASynonymCode, long? ASynonymFirmCrCode, bool AJunk)
		{
			if (!AJunk)
				AJunk = (bool)GetFieldValueObject(PriceFields.Junk);
					 
			DataRow drCore = dsMyDB.Tables["Core"].NewRow();

			drCore["PriceCode"] = priceCode;
			drCore["ProductId"] = AProductId;
			if (ACodeFirmCr.HasValue)
				drCore["CodeFirmCr"] = ACodeFirmCr;
			drCore["SynonymCode"] = ASynonymCode;
			if (ASynonymFirmCrCode.HasValue)
				drCore["SynonymFirmCrCode"] = ASynonymFirmCrCode;
			drCore["Code"] = GetFieldValue(PriceFields.Code);
			drCore["CodeCr"] = GetFieldValue(PriceFields.CodeCr);
			drCore["Unit"] = GetFieldValue(PriceFields.Unit);
			drCore["Volume"] = GetFieldValue(PriceFields.Volume);
			drCore["Quantity"] = GetFieldValueObject(PriceFields.Quantity);
			drCore["Note"] = GetFieldValue(PriceFields.Note);
			drCore["VitallyImportant"] = Convert.ToByte( (bool)GetFieldValueObject(PriceFields.VitallyImportant) );
			drCore["RequestRatio"] = GetFieldValueObject(PriceFields.RequestRatio);
			drCore["RegistryCost"] = GetFieldValueObject(PriceFields.RegistryCost);
			object MaxBoundCost = GetFieldValueObject(PriceFields.MaxBoundCost);
			if ((MaxBoundCost is decimal) && !checkZeroCost((decimal)MaxBoundCost))
				drCore["MaxBoundCost"] = (decimal)MaxBoundCost;
			object OrderCost = GetFieldValueObject(PriceFields.OrderCost);
			if ((OrderCost is decimal) && ((decimal)OrderCost >= 0))
				drCore["OrderCost"] = (decimal)OrderCost;
			object MinOrderCount = GetFieldValueObject(PriceFields.MinOrderCount);
			if ((MinOrderCount is int) && ((int)MinOrderCount >= 0))
				drCore["MinOrderCount"] = (int)MinOrderCount;

			object dt = GetFieldValueObject(PriceFields.Period);
			string st;
			//если получилось преобразовать в дату, то сохраняем в формате даты
			if (dt is DateTime)
				st = ((DateTime)dt).ToString("dd'.'MM'.'yyyy");
			else
			{
				//Если не получилось преобразовать, то смотрим на "сырое" значение поле, если оно не пусто, то пишем в базу
				st = GetFieldRawValue(PriceFields.Period);
				if (String.IsNullOrEmpty(st))
					st = null;
			}
			drCore["Period"] = st;

			drCore["Doc"] = GetFieldValueObject(PriceFields.Doc);

			drCore["Junk"] = Convert.ToByte(AJunk);
			drCore["Await"] = Convert.ToByte( (bool)GetFieldValueObject(PriceFields.Await) );

			drCore["MinBoundCost"] = GetFieldValueObject(PriceFields.MinBoundCost);


			dsMyDB.Tables["Core"].Rows.Add(drCore);
			if (priceType != Settings.Default.ASSORT_FLG)
				CoreCosts.Add(DeepCopy(currentCoreCosts));
			formCount++;
		}

		object DeepCopy(ArrayList al)
		{
			ArrayList alNew = new ArrayList();
			for (int i = 0; i < al.Count; i++)
			{
				alNew.Add(((ICloneable)al[i]).Clone());
			}
			return alNew;
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

			zeroCount++;
		}

		/// <summary>
		/// Вставка в нераспознанные позиции
		/// </summary>
		/// <param name="AProductId"></param>
		/// <param name="AShortCode"></param>
		/// <param name="ACodeFirmCr"></param>
		/// <param name="AStatus"></param>
		/// <param name="AJunk"></param>
		public void InsertToUnrec(long? AProductId, long? ACodeFirmCr, int AStatus, bool AJunk)
		{
			DataRow drUnrecExp = dsMyDB.Tables["UnrecExp"].NewRow();
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

			if (!AJunk)
				AJunk = (bool)GetFieldValueObject(PriceFields.Junk);
			drUnrecExp["Junk"] = Convert.ToByte(AJunk);

			drUnrecExp["AddDate"] = DateTime.Now;

			drUnrecExp["Status"] = AStatus;
			drUnrecExp["Already"] = AStatus;
			if (AProductId.HasValue)
				drUnrecExp["TmpProductId"] = AProductId;
			if (ACodeFirmCr.HasValue)
				drUnrecExp["TmpCodeFirmCr"] = ACodeFirmCr;

			if (dtUnrecExp.Columns.Contains("HandMade"))
				drUnrecExp["HandMade"] = 0;

			dsMyDB.Tables["UnrecExp"].Rows.Add(drUnrecExp);
			unrecCount++;
		}

		/// <summary>
		/// Вставка в таблицу запрещенных предложений
		/// </summary>
		/// <param name="PosName"></param>
		public void InsertIntoForb(string PosName)
		{
			DataRow newRow = dsMyDB.Tables["Forb"].NewRow();
			newRow["PriceItemId"] = priceItemId;
			newRow["Forb"] = PosName;
			try
			{
				dsMyDB.Tables["Forb"].Rows.Add(newRow);
				forbCount++;
			}
			catch(ConstraintException)
			{}
		}

		/// <summary>
		/// Подготовка к разбору прайса, чтение таблиц
		/// </summary>
		public void Prepare()
		{
			_logger.Debug("начало Prepare");
			daForbidden = new MySqlDataAdapter(
				String.Format("SELECT PriceCode, LOWER(Forbidden) AS Forbidden FROM farm.Forbidden WHERE PriceCode={0}", priceCode), MyConn);
			daForbidden.Fill(dsMyDB, "Forbidden");
			dtForbidden = dsMyDB.Tables["Forbidden"];
			_logger.Debug("загрузили Forbidden");

			daSynonym = new MySqlDataAdapter(
				String.Format("SELECT SynonymCode, LOWER(Synonym) AS Synonym, ProductId, Junk FROM farm.Synonym WHERE PriceCode={0}", parentSynonym), MyConn);
			daSynonym.Fill(dsMyDB, "Synonym");
			dtSynonym = dsMyDB.Tables["Synonym"];
			_logger.Debug("загрузили Synonym");

			daSynonymFirmCr = new MySqlDataAdapter(
				String.Format("SELECT SynonymFirmCrCode, CodeFirmCr, LOWER(Synonym) AS Synonym FROM farm.SynonymFirmCr WHERE PriceCode={0}", parentSynonym), MyConn);
			daSynonymFirmCr.Fill(dsMyDB, "SynonymFirmCr");
			dtSynonymFirmCr = dsMyDB.Tables["SynonymFirmCr"];
			_logger.Debug("загрузили SynonymFirmCr");

			daCore = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Core0 WHERE PriceCode={0} LIMIT 0", priceCode), MyConn);
			daCore.Fill(dsMyDB, "Core");
			dtCore = dsMyDB.Tables["Core"];
			_logger.Debug("загрузили Core");

			daUnrecExp = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.UnrecExp WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
			cbUnrecExp = new MySqlCommandBuilder(daUnrecExp);
			daUnrecExp.InsertCommand = cbUnrecExp.GetInsertCommand();
			daUnrecExp.InsertCommand.CommandTimeout = 0;
			daUnrecExp.Fill(dsMyDB, "UnrecExp");
			dtUnrecExp = dsMyDB.Tables["UnrecExp"];
			dtUnrecExp.Columns["AddDate"].DataType = typeof(DateTime);
			_logger.Debug("загрузили UnrecExp");

			daZero = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Zero WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
			cbZero = new MySqlCommandBuilder(daZero);
			daZero.InsertCommand = cbZero.GetInsertCommand();
			daZero.InsertCommand.CommandTimeout = 0;
			daZero.Fill(dsMyDB, "Zero");
			dtZero = dsMyDB.Tables["Zero"];
			_logger.Debug("загрузили Zero");

			daForb = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Forb WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
			cbForb = new MySqlCommandBuilder(daForb);
			daForb.InsertCommand = cbForb.GetInsertCommand();
			daForb.InsertCommand.CommandTimeout = 0;
			daForb.Fill(dsMyDB, "Forb");
			dtForb = dsMyDB.Tables["Forb"];
			dtForb.Constraints.Add("ForbName", new DataColumn[] {dtForb.Columns["Forb"]}, false);
			_logger.Debug("загрузили Forb");

			daCoreCosts = new MySqlDataAdapter("SELECT * FROM farm.CoreCosts LIMIT 0", MyConn);
			daCoreCosts.Fill(dsMyDB, "CoreCosts");
			dtCoreCosts = dsMyDB.Tables["CoreCosts"];
			_logger.Debug("загрузили CoreCosts");

			if (priceCodesUseUpdate.Contains(priceCode))
			{
				Stopwatch LoadExistsWatch = Stopwatch.StartNew();

				string existsCoreSQL;
				if (costType == CostTypes.MultiColumn)
					existsCoreSQL = String.Format("SELECT * FROM farm.Core0 WHERE PriceCode={0} order by Id", priceCode);
				else
					existsCoreSQL = String.Format("SELECT Core0.* FROM farm.Core0, farm.CoreCosts WHERE Core0.PriceCode={0} and CoreCosts.Core_Id = Core0.id and CoreCosts.PC_CostCode = {1} order by Core0.Id", priceCode, costCode);

				daExistsCore = new MySqlDataAdapter(existsCoreSQL, MyConn);
				daExistsCore.Fill(dsMyDB, "ExistsCore");
				dtExistsCore = dsMyDB.Tables["ExistsCore"];
				foreach (DataColumn column in dtExistsCore.Columns)
					if (!primaryFields.Contains(column.ColumnName) && !(column.ColumnName.Equals("Id", StringComparison.OrdinalIgnoreCase)))
						compareFields.Add(column.ColumnName);
				_logger.Debug("загрузили ExistsCore");

				string existsCoreCostsSQL;
				if (costType == CostTypes.MultiColumn)
					existsCoreCostsSQL = String.Format(@"
SELECT 
  CoreCosts.* 
FROM 
  farm.Core0, 
  farm.CoreCosts,
  usersettings.pricescosts
WHERE 
    Core0.PriceCode = {0} 
and pricescosts.PriceCode = {0}
and CoreCosts.Core_Id = Core0.id
and CoreCosts.PC_CostCode = pricescosts.CostCode 
order by Core0.Id", priceCode);
				else
					existsCoreCostsSQL = String.Format("SELECT CoreCosts.* FROM farm.Core0, farm.CoreCosts WHERE Core0.PriceCode={0} and CoreCosts.Core_Id = Core0.id and CoreCosts.PC_CostCode = {1} order by Core0.Id", priceCode, costCode);
				daExistsCoreCosts = new MySqlDataAdapter(existsCoreCostsSQL, MyConn);
				dtExistsCoreCosts = dtCoreCosts.Clone();
				dtExistsCoreCosts.TableName = "ExistsCoreCosts";
				dtExistsCoreCosts.Columns["PC_CostCode"].DataType = typeof(long);
				dsMyDB.Tables.Add(dtExistsCoreCosts);
				daExistsCoreCosts.Fill(dtExistsCoreCosts);
				_logger.Debug("загрузили ExistsCoreCosts");

				Stopwatch ModifyCoreCostsWatch = Stopwatch.StartNew();
				relationExistsCoreToCosts = new DataRelation("ExistsCoreToCosts", dtExistsCore.Columns["Id"], dtExistsCoreCosts.Columns["Core_Id"]);
				dsMyDB.Relations.Add(relationExistsCoreToCosts);
				ModifyCoreCostsWatch.Stop();

				LoadExistsWatch.Stop();

				_logger.InfoFormat("Загрузка и подготовка существующего прайса : {0}", LoadExistsWatch.Elapsed);
				_logger.InfoFormat("Изменить CoreCosts : {0}", ModifyCoreCostsWatch.Elapsed);
			}

#if TESTINGUPDATE
#endif
			_logger.Debug("конец Prepare");
		}

		public string StatCommand(MySqlCommand command)
		{
			DateTime startTime = DateTime.UtcNow;
			int applyCount = command.ExecuteNonQuery();
			TimeSpan workTime = DateTime.UtcNow.Subtract(startTime);
			return String.Format("{0};{1}", applyCount, workTime);
		}

		public string TryUpdate(MySqlDataAdapter da, DataTable dt, MySqlTransaction tran)
		{
			DateTime startTime = DateTime.UtcNow;
			da.SelectCommand.Transaction = tran;
			int applyCount = da.Update(dt);
			TimeSpan workTime = DateTime.UtcNow.Subtract(startTime);
			return String.Format("{0};{1}", applyCount, workTime);
		}

		private string[] GetSQLToInsertCoreAndCoreCosts(out string SynonymUpdateCommand, out string SynonymFirmCrUpdateCommand)
		{
			SynonymUpdateCommand = null;
			SynonymFirmCrUpdateCommand = null;

			if (dtCore.Rows.Count > 0)
			{
				List<string> commandList = new List<string>();

				List<string> synonymCodes = new List<string>();
				List<string> synonymFirmCrCodes = new List<string>();

				string lastCommand = String.Empty;
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				DataRow drCore;

				for(int i = 0; i < dtCore.Rows.Count; i++)
				{
					drCore = dtCore.Rows[i];
					InsertCorePosition(drCore, sb);

					if (!synonymCodes.Contains(drCore["SynonymCode"].ToString()))
						synonymCodes.Add(drCore["SynonymCode"].ToString());

					if (!synonymFirmCrCodes.Contains(drCore["SynonymFirmCrCode"].ToString()))
						synonymFirmCrCodes.Add(drCore["SynonymFirmCrCode"].ToString());

					if (priceType != Settings.Default.ASSORT_FLG)
						InsertCoreCosts(sb, (ArrayList)CoreCosts[i]);

					if ((i+1) % Settings.Default.MaxPositionInsertToCore == 0)
					{
						lastCommand = sb.ToString();
						if (!String.IsNullOrEmpty(lastCommand))
							commandList.Add(lastCommand);
						sb = new System.Text.StringBuilder();
					}
				}

				lastCommand = sb.ToString();
				if (!String.IsNullOrEmpty(lastCommand))
					commandList.Add(lastCommand);

				SynonymUpdateCommand = "update farm.UsedSynonymLogs set LastUsed = now() where SynonymCode in (" + String.Join(", ", synonymCodes.ToArray()) + ");";

				SynonymFirmCrUpdateCommand = "update farm.UsedSynonymFirmCrLogs set LastUsed = now() where SynonymFirmCrCode in (" + String.Join(", ", synonymFirmCrCodes.ToArray()) + ");";				

				return commandList.ToArray();
			}
			else
				return new string[] {};
		}

		private string[] GetSQLToUpdateCoreAndCoreCosts(out string SynonymUpdateCommand, out string SynonymFirmCrUpdateCommand)
		{
			SynonymUpdateCommand = null;
			SynonymFirmCrUpdateCommand = null;

			List<string> commandList = new List<string>();

			//Если чего-либо формализовали, то делаем синхронизацию, иначе все позиции просто удалятся
			if (dtCore.Rows.Count > 0)
			{
				List<string> synonymCodes = new List<string>();
				List<string> synonymFirmCrCodes = new List<string>();

				string lastCommand = String.Empty;
				System.Text.StringBuilder sb = new System.Text.StringBuilder();

				//формализованная строка
				DataRow drCore;

				//найденная строка из существующего прайса
				DataRow drExistsCore;

				int AllCommandCount = 0;

				for (int i = 0; i < dtCore.Rows.Count; i++)
				{
					drCore = dtCore.Rows[i];

					if (!synonymCodes.Contains(drCore["SynonymCode"].ToString()))
						synonymCodes.Add(drCore["SynonymCode"].ToString());

					if (!synonymFirmCrCodes.Contains(drCore["SynonymFirmCrCode"].ToString()))
						synonymFirmCrCodes.Add(drCore["SynonymFirmCrCode"].ToString());

					drExistsCore = FindPositionInExistsCore(drCore);

					if (drExistsCore == null)
					{
						statCounters[FormalizeStats.InsertCount]++;
						statCounters[FormalizeStats.CommandCount]++;						
						InsertCorePosition(drCore, sb);
					}
					else
					{
						UpdateCorePosition(drExistsCore, drCore, sb);
					}

					if (priceType != Settings.Default.ASSORT_FLG)
					{
						if (drExistsCore == null)
						{
							statCounters[FormalizeStats.CommandCount]++;
							InsertCoreCosts(sb, (ArrayList)CoreCosts[i]);
						}
						else
							UpdateCoreCosts(drExistsCore, drCore, sb, (ArrayList)CoreCosts[i]);
					}

					//Если мы нашли запись в существующем Core, то удаляем ее из кэша существующих предложений, 
					//чтобы при следующем поиске она не учитывалась
					if (drExistsCore != null)
						drExistsCore.Delete();

					//Производим отсечку по кол-во сформированных команд
					if (statCounters[FormalizeStats.CommandCount] >= 200)
					{
						_logger.DebugFormat("Отсечка: {0}", statCounters[FormalizeStats.CommandCount]);
						AllCommandCount += statCounters[FormalizeStats.CommandCount];
						lastCommand = sb.ToString();
#if SQLDUMP
						_logger.DebugFormat("SQL-команда: {0}", lastCommand);
#endif
						if (!String.IsNullOrEmpty(lastCommand))
							commandList.Add(lastCommand);
						sb = new System.Text.StringBuilder();
						statCounters[FormalizeStats.CommandCount] = 0;
					}
				}

				statCounters[FormalizeStats.AvgSearchTime] = statCounters[FormalizeStats.AvgSearchTime] / dtCore.Rows.Count;

				lastCommand = sb.ToString();
				if (!String.IsNullOrEmpty(lastCommand))
				{
					_logger.DebugFormat("Отсечка: {0}", statCounters[FormalizeStats.CommandCount]);
#if SQLDUMP
					_logger.DebugFormat("SQL-команда: {0}", lastCommand);
#endif
					commandList.Add(lastCommand);
					AllCommandCount += statCounters[FormalizeStats.CommandCount];
					statCounters[FormalizeStats.CommandCount] = AllCommandCount;
				}

				//Производим поиск записей в кэше существующих предложений,
				//которые не были помечены как удаленные, что говорит о том, 
				//что эти записи не рассматривались при синхронизации, следовательно их нужно удалить
				List<string> deleteCore = new List<string>();
				foreach (DataRow deleted in dtExistsCore.Rows)
					if ((deleted.RowState != DataRowState.Deleted))
					{
						statCounters[FormalizeStats.DeleteCount]++;
						deleteCore.Add(deleted["Id"].ToString());
					}
				if (deleteCore.Count > 0)
				{
					//Если есть записи, которые нужно удалить из Core, то сначала формируем
					List<string> costsList = new List<string>();
					foreach (CoreCost c in currentCoreCosts)
						costsList.Add(c.costCode.ToString());

					string costCodeFilter = String.Join(", ", costsList.ToArray());

					//формируем команды на удаление из CoreCosts по указанному CoreId
					//для позиций из Core, которые не нашли в формализованном прайс-листе
					List<string> deleteCommandList = new List<string>();
					foreach(string coreId in deleteCore)
						deleteCommandList.Add(String.Format(@"
delete
from
  farm.CoreCosts
where
  CoreCosts.Core_Id = {0}
and CoreCosts.PC_CostCode in ({1});",
									coreId, costCodeFilter));

					//формируем команду на удаление из Core по указанному CoreId
					//для позиций, которые не нашли в формализованном прайс-листе
					deleteCommandList.Add(String.Format(@"
delete
from
  farm.Core0
where
  Core0.Id in ({0});",
						String.Join(", ", deleteCore.ToArray())));

					//Добавляем данные команды в начало списка команд, 
					//чтобы удаление из Core и CoreCosts выполнилось первым
					commandList.InsertRange(0, deleteCommandList);
				}

				List<string> statCounterValues = new List<string>();
				foreach (FormalizeStats statCounter in Enum.GetValues(typeof(FormalizeStats)))
					statCounterValues.Add(String.Format("{0} = {1}", statCounter, statCounters[statCounter]));
				_logger.InfoFormat("Статистика обновления прайс-листа: {0}", String.Join("; ", statCounterValues.ToArray()));

				SynonymUpdateCommand = "update farm.UsedSynonymLogs set LastUsed = now() where SynonymCode in (" + String.Join(", ", synonymCodes.ToArray()) + ");";

				SynonymFirmCrUpdateCommand = "update farm.UsedSynonymFirmCrLogs set LastUsed = now() where SynonymFirmCrCode in (" + String.Join(", ", synonymFirmCrCodes.ToArray()) + ");";
			}

			return commandList.ToArray();
		}

		private void UpdateCoreCosts(DataRow drExistsCore, DataRow drCore, System.Text.StringBuilder sb, ArrayList costs)
		{
			DataRow[] drExistsCosts = drExistsCore.GetChildRows(relationExistsCoreToCosts);
			DataRow drCurrent;

			foreach (CoreCost c in costs)
				//Если значение формализованной цены больше нуля, то будет ее обновлять или вставлять, иначе существующая должна быть удалена
				if (c.cost.HasValue && (c.cost > 0))
				{
					//Попытка поиска цены в списке
					drCurrent = null;
					foreach (DataRow find in drExistsCosts)
						if ((find.RowState != DataRowState.Deleted) && (long)find["PC_CostCode"] == c.costCode)
						{
							drCurrent = find;
							break;
						}

					//Если цена не найдена, то производим вставку
					if (drCurrent == null)
					{
						statCounters[FormalizeStats.InsertCostCount]++;
						statCounters[FormalizeStats.CommandCount]++;
						sb.AppendFormat("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost) values ({0}, {1}, {2});\r\n",
							drExistsCore["Id"], c.costCode, c.cost.Value.ToString(CultureInfo.InvariantCulture.NumberFormat));
					}
					else
					{
						//Если цена найдена и значение цены другое, то обновляем цену в таблице
						if (c.cost.Value.CompareTo(Convert.ToDecimal(drCurrent["Cost"])) != 0)
						{
							statCounters[FormalizeStats.UpdateCostCount]++;
							statCounters[FormalizeStats.CommandCount]++;
							sb.AppendFormat("update farm.CoreCosts set Cost = {0} where Core_Id = {1} and PC_CostCode = {2};\r\n",
								c.cost.Value.ToString(CultureInfo.InvariantCulture.NumberFormat), drExistsCore["Id"], c.costCode);
						}
						//Удаляем цену из кэша таблицы, чтобы при следующем поиске ее не рассматривать
						drCurrent.Delete();
					}
				}

			List<string> deleteCosts = new List<string>();
			foreach (DataRow deleted in drExistsCosts)
			{
				//Пробегаемся по всем неудаленным ценам и считаем их ненайденными в формализованном прайс-листе,
				//следовательно данную цену нужно удалить из CoreCosts
				if (deleted.RowState != DataRowState.Deleted)
				{
					statCounters[FormalizeStats.DeleteCostCount]++;
					deleteCosts.Add(deleted["PC_CostCode"].ToString());
				}
				deleted.Delete();
			}
			if (deleteCosts.Count > 0)
			{
				statCounters[FormalizeStats.CommandCount]++;
				sb.AppendFormat("delete from farm.CoreCosts where Core_Id = {0} and PC_CostCode in ({1});\r\n", drExistsCore["Id"], String.Join(", ", deleteCosts.ToArray()));
			}
		}

		private void UpdateCorePosition(DataRow drExistsCore, DataRow drCore, System.Text.StringBuilder sb)
		{
			List<string> updateFieldsScript = new List<string>();
			foreach (string compareField in compareFields)
			{
				object NewValue;
				if (drCore.Table.Columns[compareField].DataType == typeof(string))
				{
					if (drCore[compareField] is DBNull)
						NewValue = String.Empty;
					else
						NewValue = drCore[compareField];
				}
				else
					NewValue = drCore[compareField];
				if (!NewValue.Equals(drExistsCore[compareField]))
					if (drCore.Table.Columns[compareField].DataType == typeof(string))
					{
						if (drCore[compareField] is DBNull)
							updateFieldsScript.Add(compareField + " = ''");
						else
							updateFieldsScript.Add(String.Format("{0} = '{1}'", compareField, StringToMySqlString(drCore[compareField].ToString())));
					}
					else
						if (drCore[compareField] is DBNull)
							updateFieldsScript.Add(compareField + " = null");
						else
							if (drCore.Table.Columns[compareField].DataType == typeof(decimal))
								updateFieldsScript.Add(String.Format("{0} = {1}", compareField, Convert.ToDecimal(drCore[compareField]).ToString(CultureInfo.InvariantCulture.NumberFormat)));
							else
								updateFieldsScript.Add(String.Format("{0} = {1}", compareField, drCore[compareField]));
			}

			if (updateFieldsScript.Count > 0)
			{
				statCounters[FormalizeStats.UpdateCount]++;
				statCounters[FormalizeStats.CommandCount]++;
				sb.AppendFormat("update farm.Core0 set {0} where Id = {1};\r\n", String.Join(", ", updateFieldsScript.ToArray()), drExistsCore["Id"]);
			}
		}

		private DataRow FindPositionInExistsCore(DataRow drCore)
		{
			DateTime dtSearchTime = DateTime.UtcNow;
			List<string> filter = new List<string>();
			foreach (string primaryField in primaryFields)
			{
				if (drCore.Table.Columns[primaryField].DataType == typeof(string))
				{
					if (drCore[primaryField] is DBNull)
						filter.Add(String.Format("({0} = '')", primaryField));
					else
						filter.Add(String.Format("({0} = '{1}')", primaryField, drCore[primaryField]));
				}
				else
					if (drCore[primaryField] is DBNull)
						filter.Add(String.Format("({0} is null)", primaryField));
					else
						filter.Add(String.Format("({0} = {1})", primaryField, drCore[primaryField]));
			}
			string filterString = String.Join(" and ", filter.ToArray());
			DataRow[] drsExists = dtExistsCore.Select(filterString);
			TimeSpan tsSearchTime = DateTime.UtcNow.Subtract(dtSearchTime);
			statCounters[FormalizeStats.AvgSearchTime] += Convert.ToInt32(tsSearchTime.TotalMilliseconds);

			if (drsExists.Length == 0)
				return null;
			if (drsExists.Length == 1)
			{
				statCounters[FormalizeStats.FirstSearch]++;
				return drsExists[0];
			}

			int maxMatchesNumber = 0, currentMatchesNumber;
			DataRow maxMatchesNumberRow = null;
			foreach (DataRow drExists in drsExists)
			{
				currentMatchesNumber = 0;
				foreach (string compareField in compareFields)
					if (drCore[compareField].Equals(drExists[compareField]))
						currentMatchesNumber++;
				if (currentMatchesNumber > maxMatchesNumber)
				{
					maxMatchesNumber = currentMatchesNumber;
					maxMatchesNumberRow = drExists;
				}
			}
			statCounters[FormalizeStats.SecondSearch]++;
			return maxMatchesNumberRow;
		}

		private void InsertCoreCosts(StringBuilder sb, ArrayList coreCosts)
		{
			if ((coreCosts != null) && (coreCosts.Count > 0))
			{
				sb.AppendLine("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost) values ");
				bool FirstInsert = true;
				foreach (CoreCost c in coreCosts)
				{
					if (c.cost.HasValue && (c.cost > 0))
					{
						if (!FirstInsert)
							sb.Append(", ");
						FirstInsert = false;
						sb.AppendFormat("(@LastCoreID, {0}, {1}) ", c.costCode, (c.cost.HasValue && (c.cost > 0)) ? c.cost.Value.ToString(CultureInfo.InvariantCulture.NumberFormat) : "null");
					}
				}
				sb.AppendLine(";");
			}
		}

		private void InsertCorePosition(DataRow drCore, System.Text.StringBuilder sb)
		{
			sb.AppendLine("insert into farm.Core0 (" +
				"PriceCode, ProductId, CodeFirmCr, SynonymCode, SynonymFirmCrCode, " +
				"Period, Junk, Await, MinBoundCost, " +
				"VitallyImportant, RequestRatio, RegistryCost, " +
				"MaxBoundCost, OrderCost, MinOrderCount, " +
				"Code, CodeCr, Unit, Volume, Quantity, Note, Doc) values ");
			sb.Append("(");
			sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, ",
				drCore["PriceCode"],
				drCore["ProductId"],
				Convert.IsDBNull(drCore["CodeFirmCr"]) ? "null" : drCore["CodeFirmCr"].ToString(),
				drCore["SynonymCode"],
				Convert.IsDBNull(drCore["SynonymFirmCrCode"]) ? "null" : drCore["SynonymFirmCrCode"].ToString());
			sb.AppendFormat("'{0}', ", (drCore["Period"] is DBNull) ? String.Empty : drCore["Period"].ToString());
			sb.AppendFormat("{0}, ", drCore["Junk"]);
			sb.AppendFormat("'{0}', ", drCore["Await"]);
			sb.AppendFormat("{0}, ", (drCore["MinBoundCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["MinBoundCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", drCore["VitallyImportant"]);
			sb.AppendFormat("{0}, ", (drCore["RequestRatio"] is DBNull) ? "null" : drCore["RequestRatio"].ToString());
			sb.AppendFormat("{0}, ", (drCore["RegistryCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["RegistryCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", (drCore["MaxBoundCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["MaxBoundCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", (drCore["OrderCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["OrderCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", (drCore["MinOrderCount"] is DBNull) ? "null" : drCore["MinOrderCount"].ToString());
			AddTextParameter("Code", drCore, sb);
			sb.Append(", ");

			AddTextParameter("CodeCr", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Unit", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Volume", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Quantity", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Note", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Doc", drCore, sb);

			sb.AppendLine(");");
            sb.AppendLine("set @LastCoreID = last_insert_id();");
		}

		public void AddTextParameter(string ParamName, DataRow dr, System.Text.StringBuilder sb)
		{
			if (dr[ParamName] is DBNull)
				sb.Append("''");
			else
				sb.AppendFormat("'{0}'", StringToMySqlString(dr[ParamName].ToString()));
		}

		private static string StringToMySqlString(string s)
		{
			s = s.Replace("\\", "\\\\");
			s = s.Replace("\'", "\\\'");
			s = s.Replace("\"", "\\\"");
			s = s.Replace("`", "\\`");
			return s;
		}

		/// <summary>
		/// Окончание разбора прайса, с последующим логированием статистики
		/// </summary>
		public void FinalizePrice()
		{
			//Проверку и отправку уведомлений производим только для загруженных прайс-листов
			if (downloaded)
				ProcessUndefinedCost();

			if (Settings.Default.CheckZero && (zeroCount > (formCount + unformCount + zeroCount) * 0.95) )
				throw new RollbackFormalizeException(Settings.Default.ZeroRollbackError, firmCode, priceCode, firmShortName, priceName, formCount, zeroCount, unformCount, forbCount);

			if (formCount * 1.6 < prevRowCount)
				throw new RollbackFormalizeException(Settings.Default.PrevFormRollbackError, firmCode, priceCode, firmShortName, priceName, formCount, zeroCount, unformCount, forbCount);

			string SynonymUpdateCommand;
			string SynonymFirmCrUpdateCommand;

			string[] insertCoreAndCoreCostsCommandList;

			if (priceCodesUseUpdate.Contains(priceCode))
			{
				Stopwatch GetSQLWatch = Stopwatch.StartNew();
				insertCoreAndCoreCostsCommandList = GetSQLToUpdateCoreAndCoreCosts(out SynonymUpdateCommand, out SynonymFirmCrUpdateCommand);
				GetSQLWatch.Stop();
				_logger.InfoFormat("Общее время подготовки update SQL-команд : {0}", GetSQLWatch.Elapsed);
			}
			else
				insertCoreAndCoreCostsCommandList = GetSQLToInsertCoreAndCoreCosts(out SynonymUpdateCommand, out SynonymFirmCrUpdateCommand);

			//Производим транзакцию с применением главного прайса и таблицы цен
			bool res = false;
			int tryCount = 0;
			//Для логирования статистики
			StringBuilder sbLog;
			do
			{
				_logger.Info("FinalizePrice started.");
				sbLog = new StringBuilder();

				var finalizeTransaction = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);

				try
				{
					MySqlCommand mcClear = new MySqlCommand();
					mcClear.Connection = MyConn;
					mcClear.Transaction = finalizeTransaction;
					mcClear.CommandTimeout = 0;

					mcClear.Parameters.Clear();

					//Производим данные действия, если не надо делать update и очищаем прайс-листы, или если не формализовали прайс-лист и надо его очистить
					if (!priceCodesUseUpdate.Contains(priceCode) || (dtCore.Rows.Count == 0))
					{
						if ((costType == CostTypes.MiltiFile) && (priceType != Settings.Default.ASSORT_FLG))
						{
							mcClear.CommandText = String.Format(@"
delete
  farm.Core0
from
  farm.CoreCosts,
  farm.Core0
where
    CoreCosts.Core_Id = Core0.Id
and Core0.PriceCode = {0}
and CoreCosts.PC_CostCode = {1};", priceCode, costCode);
							sbLog.AppendFormat("DelFromCoreAndCoreCosts={0}  ", StatCommand(mcClear));
						}
						else
						{
							if (priceType != Settings.Default.ASSORT_FLG)
							{
								//Удаляем цены из CoreCosts
								var sbDelCoreCosts = new StringBuilder();
								sbDelCoreCosts.Append("delete from farm.CoreCosts where pc_costcode in (");
								bool FirstInsertCoreCosts = true;
								foreach (CoreCost c in currentCoreCosts)
								{
									if (!FirstInsertCoreCosts)
										sbDelCoreCosts.Append(", ");
									FirstInsertCoreCosts = false;
									sbDelCoreCosts.Append(c.costCode.ToString());
								}
								sbDelCoreCosts.Append(");");

								if (currentCoreCosts.Count > 0)
								{
									//Производим удаление цен
									mcClear.CommandText = sbDelCoreCosts.ToString();
									sbLog.AppendFormat("DelFromCoreCosts={0}  ", StatCommand(mcClear));
								}
							}

							//Добавляем команду на удаление данных из Core
							mcClear.CommandText = String.Format("delete from farm.Core0 where PriceCode={0};", priceCode);
							sbLog.AppendFormat("DelFromCore={0}  ", StatCommand(mcClear));
						}

					}

					//выполняем команды с обновлением данных в Core и CoreCosts
					if (insertCoreAndCoreCostsCommandList.Length > 0)
					{
						DateTime tmInsertCoreAndCoreCosts = DateTime.UtcNow;
						int applyPositionCount = 0;
						foreach (string command in insertCoreAndCoreCostsCommandList)
						{
							mcClear.CommandText = command;
							//_logger.DebugFormat("Apply Core and CoreCosts command: {0}", mcClear.CommandText);

							applyPositionCount += mcClear.ExecuteNonQuery();
#if DEBUG
							_logger.DebugFormat("Apply Core and CoreCosts Count: {0}", applyPositionCount);
#endif
						}

						TimeSpan tsInsertCoreAndCoreCosts = DateTime.UtcNow.Subtract(tmInsertCoreAndCoreCosts);

						if (priceCodesUseUpdate.Contains(priceCode))
							sbLog.AppendFormat("UpdateToCoreAndCoreCosts={0};{1}  ", applyPositionCount, tsInsertCoreAndCoreCosts);
						else
							sbLog.AppendFormat("InsertToCoreAndCoreCosts={0};{1}  ", applyPositionCount, tsInsertCoreAndCoreCosts);

						mcClear.CommandText = @"
insert into catalogs.assortment
  (ProductId, CodeFirmCr)
select
  distinct c.ProductId, c.CodeFirmCr
from
  farm.core0 c
  left join catalogs.assortment a on a.ProductId = c.ProductId and a.CodeFirmCr = c.CodeFirmCr
where
    c.PriceCode = ?PriceCode
and c.CodeFirmCr > 1
and a.ProductId is null";
						mcClear.Parameters.Clear();
						mcClear.Parameters.AddWithValue("?PriceCode", priceCode);
						sbLog.AppendFormat("UpdateAssortment={0}  ", StatCommand(mcClear));

						//mcClear.CommandText = SynonymUpdateCommand;
						//mcClear.Parameters.Clear();
						//sbLog.AppendFormat("UpdateSynonymCode={0}  ", mcClear.ExecuteNonQuery());

						//mcClear.CommandText = SynonymFirmCrUpdateCommand;
						//mcClear.Parameters.Clear();
						//sbLog.AppendFormat("UpdateSynonymFirmCrCode={0}  ", mcClear.ExecuteNonQuery());
					}
					else
					{
						if (priceCodesUseUpdate.Contains(priceCode))
							sbLog.Append("UpdateToCoreAndCoreCosts=0  ");
						else
							sbLog.Append("InsertToCoreAndCoreCosts=0  ");
					}


					mcClear.CommandText = String.Format("delete from farm.Zero where PriceItemId={0}", priceItemId);
					sbLog.AppendFormat("DelFromZero={0}  ", StatCommand(mcClear));

					mcClear.CommandText = String.Format("delete from farm.Forb where PriceItemId={0}", priceItemId);
					sbLog.AppendFormat("DelFromForb={0}  ", StatCommand(mcClear));

					MySqlDataAdapter daBlockedPrice = new MySqlDataAdapter(String.Format("SELECT * FROM farm.blockedprice where PriceItemId={0} limit 1", priceItemId), MyConn);
					daBlockedPrice.SelectCommand.Transaction = finalizeTransaction;
					DataTable dtBlockedPrice = new DataTable();
					daBlockedPrice.Fill(dtBlockedPrice);

					if ((dtBlockedPrice.Rows.Count == 0))
					{
						mcClear.CommandText = String.Format("delete from farm.UnrecExp where PriceItemId={0}", priceItemId);
						sbLog.AppendFormat("DelFromUnrecExp={0}  ", StatCommand(mcClear));
					}

					sbLog.AppendFormat("UpdateForb={0}  ", TryUpdate(daForb, dtForb.Copy(), finalizeTransaction));
					sbLog.AppendFormat("UpdateZero={0}  ", TryUpdate(daZero, dtZero.Copy(), finalizeTransaction));
					sbLog.AppendFormat("UpdateUnrecExp={0}  ", TryUpdate(daUnrecExp, dtUnrecExp.Copy(), finalizeTransaction));

					//Производим обновление PriceDate и LastFormalization в информации о формализации
					//Если прайс-лист загружен, то обновляем поле PriceDate, если нет, то обновляем данные в intersection_update_info
					mcClear.Parameters.Clear();
					if (downloaded)
					{
						mcClear.CommandText = String.Format(
							"UPDATE usersettings.PriceItems SET RowCount={0}, PriceDate=now(), LastFormalization=now(), UnformCount={1} WHERE Id={2};", formCount, unformCount, priceItemId);
					}
					else
					{
						mcClear.CommandText = String.Format(
							"UPDATE usersettings.PriceItems SET RowCount={0}, LastFormalization=now(), UnformCount={1} WHERE Id={2};", formCount, unformCount, priceItemId);
					}
					mcClear.CommandText += String.Format(@"
UPDATE usersettings.AnalitFReplicationInfo A, usersettings.PricesData P
SET
  a.ForceReplication = 1
where
    p.PriceCode = {0}
and a.FirmCode = p.FirmCode;", priceCode);

					sbLog.AppendFormat("UpdatePriceItemsAndIntersections={0}  ", StatCommand(mcClear));

					_logger.InfoFormat("Statistica: {0}", sbLog.ToString());
					_logger.InfoFormat("FinalizePrice started: {0}", "Commit");
					finalizeTransaction.Commit();
					res = true;
				}
				catch (MySqlException MyError)
				{
					if (finalizeTransaction != null)
						try
						{
							finalizeTransaction.Rollback();
						}
						catch (Exception ex)
						{
							_logger.Error("Error on rollback", ex);
						}

					if ((tryCount <= Settings.Default.MaxRepeatTranCount) && ((1213 == MyError.Number) || (1205 == MyError.Number) || (1422 == MyError.Number)))
					{
						tryCount++;
						_logger.InfoFormat("Try transaction: tryCount = {0}", tryCount);
						System.Threading.Thread.Sleep(10000 + tryCount * 1000);
					}
					else
						throw;
				}
				catch (Exception)
				{
					if (finalizeTransaction != null)
						try
						{
							finalizeTransaction.Rollback();
						}
						catch (Exception ex)
						{
							_logger.Error("Error on rollback (Exception)", ex);
						}
					throw;
				}
			}while(!res);

			if (tryCount > maxLockCount)
				maxLockCount = tryCount;
		}

		/// <summary>
		/// анализируем цены и формируем список, если ценовая колонка имеет более 5% позиций с неустановленной ценой
		/// </summary>
		private void ProcessUndefinedCost()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (CoreCost cost in currentCoreCosts)
				if (cost.undefinedCostCount > formCount * 0.05)
					stringBuilder.AppendFormat("ценовая колонка \"{0}\" имеет {1} позиций с незаполненной ценой\n", cost.costName, cost.undefinedCostCount);

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

		/// <summary>
		/// Формализование прайса
		/// </summary>
		public void Formalize()
		{
			log4net.NDC.Push(String.Format("{0}.{1}", priceCode, costCode));
			_logger.Debug("начало Formalize");
			try
			{
				DateTime tmOpen = DateTime.UtcNow;
				try
				{
					Open();
					if ((dtPrice.Rows.Count > 0) && (null != toughMask))
						toughMask.Analyze(GetFieldRawValue(PriceFields.Name1));
				}
				finally
				{
					_logger.InfoFormat("Open time: {0}", DateTime.UtcNow.Subtract(tmOpen));
				}

				try
				{
					_logger.Debug("попытка открыть соединение с базой");
					MyConn.Open();
					_logger.Debug("соединение с базой установлено");

					if (dtPrice.Rows.Count > 0)
					{
						_logger.Debug("попытка открыть транзакцию");
						MySqlTransaction _prepareTransaction = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);
						_logger.Debug("транзакция открыта");

						DateTime tmPrepare = DateTime.UtcNow;
						try
						{
							try
							{
								Prepare();
							}
							finally
							{
								_prepareTransaction.Commit();
							}
						}
						finally
						{
							_logger.InfoFormat("Prepare time: {0}", DateTime.UtcNow.Subtract(tmPrepare));
						}


						DateTime tmFormalize = DateTime.UtcNow;
						try
						{
							UnrecExpStatus st;
							string PosName = String.Empty;
							bool Junk = false;
							int costCount;
							long? ProductId = null, SynonymCode = null, CodeFirmCr = null, SynonymFirmCrCode = null;
							string strCode, strName1, strOriginalName, strFirmCr;
							do
							{
								st = UnrecExpStatus.NOT_FORM;
								PosName = GetFieldValue(PriceFields.Name1, true);

								if ((null != PosName) && (String.Empty != PosName.Trim()) && (!IsForbidden(PosName)))
								{
									if (priceType != Settings.Default.ASSORT_FLG)
									{
										costCount = ProcessCosts();
										object currentQuantity = GetFieldValueObject(PriceFields.Quantity);
										//Производим проверку для мультиколоночных прайсов
										if (costType == CostTypes.MultiColumn)
										{
											//Если кол-во ненулевых цен = 0, то тогда производим вставку в Zero
											//или если количество определенно и оно равно 0
											if ((0 == costCount) || ((currentQuantity is int) && ((int)currentQuantity == 0)))
											{
												InsertToZero();
												continue;
											}
										}
										else
										{
											//Эта проверка для всех остальных
											//Если кол-во ненулевых цен = 0
											//или если количество определенно и оно равно 0
											if ((0 == costCount) || ((currentQuantity is int) && ((int)currentQuantity == 0)))
											{
												InsertToZero();
												continue;
											}
										}
									}

									strCode = GetFieldValue(PriceFields.Code);
									strName1 = GetFieldValue(PriceFields.Name1, true);
									strOriginalName = GetFieldValue(PriceFields.OriginalName, true);

									if (GetProductId(strCode, strName1, strOriginalName, out ProductId, out  SynonymCode, out Junk))
										st = UnrecExpStatus.NAME_FORM;

									strFirmCr = GetFieldValue(PriceFields.FirmCr, true);
									if (GetCodeFirmCr(strFirmCr, out CodeFirmCr, out SynonymFirmCrCode))
										st = st | UnrecExpStatus.FIRM_FORM;

									st = st | UnrecExpStatus.CURR_FORM;

									if (((st & UnrecExpStatus.NAME_FORM) == UnrecExpStatus.NAME_FORM) && ((st & UnrecExpStatus.CURR_FORM) == UnrecExpStatus.CURR_FORM))
										InsertToCore(ProductId, CodeFirmCr, SynonymCode, SynonymFirmCrCode, Junk);
									else
										unformCount++;

									if ((st & UnrecExpStatus.FULL_FORM) != UnrecExpStatus.FULL_FORM)
										InsertToUnrec(ProductId, CodeFirmCr, (int)st, Junk);

								}
								else
									if ((null != PosName) && (String.Empty != PosName.Trim()))
										InsertIntoForb(PosName);

							}
							while (Next());
						}
						finally
						{
							_logger.InfoFormat("Formalize time: {0}", DateTime.UtcNow.Subtract(tmFormalize));
						}
					}

					DateTime tmFinalize = DateTime.UtcNow;
					try
					{
						FinalizePrice();
					}
					finally
					{
						_logger.InfoFormat("FinalizePrice time: {0}", DateTime.UtcNow.Subtract(tmFinalize));
					}
				}
				finally
				{
					MyConn.Close();
				}
			}
			finally
			{
				_logger.Debug("конец Formalize");
				log4net.NDC.Pop();
			}
		}

		/// <summary>
		/// Установить название поля, которое будет считано из набора данных
		/// </summary>
		/// <param name="PF"></param>
		/// <param name="Value"></param>
		public void SetFieldName(PriceFields PF, string Value)
		{
			FieldNames[(int)PF] = Value;
		}

		/// <summary>
		/// Получить название поля
		/// </summary>
		/// <param name="PF"></param>
		/// <returns></returns>
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
		/// Перейти на предыдущую позицию
		/// </summary>
		/// <returns>Удачно ли выполнен переход?</returns>
		public virtual bool Prior()
		{
			CurrPos--;
			if (CurrPos > -1)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Получить сырое значение текущего поля
		/// </summary>
		/// <param name="PF"></param>
		/// <returns></returns>
		public virtual string GetFieldRawValue(PriceFields PF)
		{
			try
			{
				string Value = dtPrice.Rows[CurrPos][GetFieldName(PF)].ToString();
				if (convertedToANSI)
					Value = strToANSI(Value);
				return Value;
			}
			catch
			{
				return null;
			}
		}

		public string strToANSI(string Dest)
		{
			System.Text.Encoding ansi = System.Text.Encoding.GetEncoding(1251);
			byte[] unicodeBytes = System.Text.Encoding.Unicode.GetBytes(Dest);
			byte[] ansiBytes = System.Text.Encoding.Convert(System.Text.Encoding.Unicode, ansi, unicodeBytes);
			return ansi.GetString(ansiBytes);
		}

		/// <summary>
		/// Получить значение поля в обработанном виде
		/// </summary>
		/// <param name="PF"></param>
		/// <returns></returns>
		public virtual string GetFieldValue(PriceFields PF)
		{
			string res = null;

			//Сначала пытаемся вытянуть данные из toughMask
			if (null != toughMask)
			{
				res = toughMask.GetFieldValue(PF);
				if (null != res)
				{
					//Удаляем опасные слова только из наименований
					if ((PriceFields.Name1 == PF) || (PriceFields.Name2 == PF) || (PriceFields.Name2 == PF) || (PriceFields.OriginalName == PF))
						res = RemoveForbWords(res);
					if ((PriceFields.Note != PF) && (PriceFields.Doc != PF))
						res = UnSpace(res);
				}
			}

			//Если у нас это не получилось, что пытаемся вытянуть данные из самого поля
			if ((null == res) || ("" == res.Trim()))
			{
				res = GetFieldRawValue(PF);
				if (null != res)
				{
					if ((PriceFields.Name1 == PF) || (PriceFields.Name2 == PF) || (PriceFields.Name2 == PF))
						res = RemoveForbWords(res);
					res = UnSpace(res);
				}
			}

			if ((PriceFields.Name1 == PF) || (PriceFields.Name2 == PF) || (PriceFields.Name2 == PF) || (PriceFields.OriginalName == PF) ||(PriceFields.FirmCr == PF))
			{
				if (null != res && res.Length > 255)
				{
					res = res.Remove(255, res.Length - 255);
					res = res.Trim();
				}
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
					return GetBoolValue(PriceFields.VitallyImportant, vitallyImportantMask); ;

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
					NumberFormatInfo nfi = CultureInfo.CurrentCulture.NumberFormat;
					String res = String.Empty;
					foreach (Char c in CostValue.ToCharArray())
					{
						if (Char.IsDigit(c))
							res = String.Concat(res, c);
						else
						{
							if ((!Char.IsWhiteSpace(c)) && (res != String.Empty) && (-1 == res.IndexOf(nfi.CurrencyDecimalSeparator)))
								res = String.Concat(res, nfi.CurrencyDecimalSeparator);
						}
					}
					decimal d = Decimal.Parse(res, NumberStyles.Currency);
					d = Math.Round(d, 6);
					return d;
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

		/// <summary>
		/// Содержится ли название в таблице запрещенных слов
		/// </summary>
		/// <param name="PosName"></param>
		/// <returns></returns>
		public bool IsForbidden(string PosName)
		{
			DataRow[] dr = dsMyDB.Tables["Forbidden"].Select(String.Format("Forbidden = '{0}'", PosName.Replace("'", "''")));
			return dr.Length > 0;
		}

		/// <summary>
		/// Смогли ли мы распознать позицию по коду, имени и оригинальному названию?
		/// </summary>
		/// <param name="ACode"></param>
		/// <param name="AName"></param>
		/// <param name="AOriginalName"></param>
		/// <param name="AProductId"></param>
		/// <param name="AShortCode"></param>
		/// <param name="ASynonymCode"></param>
		/// <param name="AJunk"></param>
		/// <returns></returns>
		public bool GetProductId(string ACode, string AName, string AOriginalName, out long? AProductId, out long? ASynonymCode, out bool AJunk)
		{
			DataRow[] dr = null;
			if (formByCode)
			{
				if (null != ACode)
					dr = dsMyDB.Tables["Synonym"].Select(String.Format("Code = '{0}'", ACode.Replace("'", "''")));
			}
			else
			{
				if (null != AName)
					dr = dsMyDB.Tables["Synonym"].Select(String.Format("Synonym = '{0}'", AName.Replace("'", "''")));
				if ((null == dr) || (0 == dr.Length))
					if (null != AOriginalName)
						dr = dsMyDB.Tables["Synonym"].Select(String.Format("Synonym = '{0}'", AOriginalName.Replace("'", "''")));
			}

			if ((null != dr) && (dr.Length > 0))
			{
				AProductId = Convert.ToInt64(dr[0]["ProductId"]);
				ASynonymCode = Convert.ToInt64(dr[0]["SynonymCode"]);
				AJunk = Convert.ToBoolean(dr[0]["Junk"]);
				return true;
			}

			AProductId = null;
			ASynonymCode = null;
			AJunk = false;
			return false;
		}

		/// <summary>
		/// Смогли ли мы распознать производителя по названию?
		/// </summary>
		/// <param name="FirmCr"></param>
		/// <param name="ACodeFirmCr"></param>
		/// <param name="ASynonymFirmCrCode"></param>
		/// <returns></returns>
		public bool GetCodeFirmCr(string FirmCr, out long? ACodeFirmCr, out long? ASynonymFirmCrCode)
		{
			DataRow[] dr = null;
			if (null != FirmCr)
				dr = dsMyDB.Tables["SynonymFirmCr"].Select(String.Format("Synonym = '{0}'", FirmCr.Replace("'", "''")));

			if ((null != dr) && (dr.Length > 0))
			{
				//Если значение CodeFirmCr не установлено, то устанавливаем в null, иначе берем значение кода
				ACodeFirmCr = Convert.IsDBNull(dr[0]["CodeFirmCr"]) ? null : (long?)Convert.ToInt64(dr[0]["CodeFirmCr"]);
				ASynonymFirmCrCode = Convert.ToInt64(dr[0]["SynonymFirmCrCode"]);
				return true;
			}
			else
			{
 				ACodeFirmCr = null;
 				ASynonymFirmCrCode = null;
 				//Если поле FirmCr не установлено, то считаем что позиция распознана по производителю
 				return (String.IsNullOrEmpty(FirmCr) ? true : false);
			}
		}

		protected bool GetBoolValue(PriceFields priceField, string mask)
		{
			bool value = false;

			string[] trueValues = new string[] { "истина", "true"};
			string[] falseValues = new string[] { "ложь", "false" };

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
			bool JunkValue = false;
			object t = GetFieldValueObject(PriceFields.Period);			
			if (t is DateTime)
			{
				DateTime dt = (DateTime)t;
				TimeSpan ts = DateTime.Now.Subtract(dt);
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
		/// <returns></returns>
		public int ProcessCosts()
		{
			int res = 0;
			object costValue;
			foreach(CoreCost c in currentCoreCosts)
			{
				c.cost = null;
				try
				{
					costValue = dtPrice.Rows[CurrPos][c.fieldName].ToString();
				}
				catch
				{
					costValue = null;
				}
				if (null != costValue)
				{
					costValue = ProcessCost( (string)costValue );
					if (!(costValue is DBNull))
					{
						if (!checkZeroCost((decimal)costValue))
						{
							c.cost = (decimal)costValue;
							res++;
						}
						else
							c.cost = 0;
					}
				}

				//если неустановленная цена, то увеличиваем счетчик
				if (!c.cost.HasValue)
					c.undefinedCostCount++;
			}
			return res;
		}

		public string getParserID()
		{
			return String.Format("{0}.{1}.{2}", this.GetType().Name, priceCode, costCode);
		}

		public bool checkZeroCost(decimal cost)
		{
			return (cost < 0 || Math.Abs(Decimal.Zero-cost) < 0.01m);
		}

	}
}
