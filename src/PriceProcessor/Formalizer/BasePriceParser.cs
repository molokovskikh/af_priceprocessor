using System;
using System.Collections;
using System.Globalization;
using System.Data;
using System.Data.OleDb;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Web.Mail;
using Inforoom.Logging;

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

		public FormalizeException(string message, System.Int64 ClientCode, System.Int64 PriceCode, string ClientName, string PriceName) : base(message)
		{
			this.clientCode = ClientCode;
			this.priceCode = PriceCode;
			this.clientName = ClientName;
			this.priceName = PriceName;
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

		public WarningFormalizeException(string message, System.Int64 ClientCode, System.Int64 PriceCode, string ClientName, string PriceName) : base(message, ClientCode, PriceCode, ClientName, PriceName)
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
		
		public RollbackFormalizeException(string message, System.Int64 ClientCode, System.Int64 PriceCode, string ClientName, string PriceName) : base(message, ClientCode, PriceCode, ClientName, PriceName)
		{}

		public RollbackFormalizeException(string message, System.Int64 ClientCode, System.Int64 PriceCode, string ClientName, string PriceName, int FormCount, int ZeroCount, int UnformCount, int ForbCount) : base(message, ClientCode, PriceCode, ClientName, PriceName)
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
		Code,
		CodeCr,
		Name1,
		Name2,
		Name3,
		FirmCr,
		CountryCr,
		Unit,
		Volume,
		Quantity,
		Note,
		Period,
		Doc,
		BaseCost,
		Currency,
		MinBoundCost,
		Junk,
		Await,
		OriginalName
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
		public static string colPriceFMT = "PriceFmt";
		public static string colSelfPriceName = "SelfPriceName";
		public static string colClientShortName = "ClientShortName";
		public static string colClientCode = "ClientCode";
		public static string colFormByCode = "FormByCode";
		public static string colFormID = "FormID";
		public static string colSelfPriceCode = "SelfPriceCode";
		public static string colParentSynonym = "ParentSynonym";
		public static string colCurrency = "Currency";
		public static string colNameMask = "NameMask";
		public static string colForbWords = "ForbWords";
		public static string colSelfAwaitPos = "SelfAwaitPos";
		public static string colSelfJunkPos = "SelfJunkPos";
		public static string colSelfPosNum = "SelfPosNum";
		public static string colSelfFlag = "SelfFlag";
		public static string colDelimiter = "Delimiter";
		public static string colBillingStatus = "BillingStatus";
		public static string colFirmStatus = "FirmStatus";
		public static string colFirmSegment = "FirmSegment";
	}

	//
	public class CoreCost
	{
		public System.Int64 costCode = -1;
		public bool baseCost = false;
		public string costName = String.Empty;
		public string fieldName = String.Empty;
		public int txtBegin = -1;
		public int txtEnd = -1;
		public decimal cost = 0m;

		public CoreCost(System.Int64 ACostCode, string ACostName, bool ABaseCost, string AFieldName, int ATxtBegin, int ATxtEnd)
		{
			costCode = ACostCode;
			baseCost = ABaseCost;
			costName = ACostName;
			fieldName = AFieldName;
			txtBegin = ATxtBegin;
			txtEnd = ATxtEnd;
		}
	}

	public class BaseCostFinder{
		
		public override bool Equals(object obj)
		{
			if (null != obj && obj is CoreCost)
			{
				return (obj as CoreCost).baseCost;
			}
			else
				return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode ();
		}
	}

	/// <summary>
	/// Summary description for BasePriceParser.
	/// </summary>
	public abstract class BasePriceParser
	{
		//таблица с прайсом
		protected DataTable dtPrice;
		//Соединение с 
		protected OleDbConnection dbcMain;

		//Соедиение с базой данных
		protected MySqlConnection MyConn;
		//Транзакция к базе данных
		protected MySqlTransaction myTrans;

		//Таблица со списком запрещенных названий
		protected MySqlDataAdapter daForbidden;
		protected DataTable dtForbidden;
		//Таблица со списком синонимов товаров
		protected MySqlDataAdapter daSynonym;
		protected DataTable dtSynonym;
		//Таблица со списоком синонимов производителей
		protected MySqlDataAdapter daSynonymFirmCr;
		protected DataTable dtSynonymFirmCr;
		//Таблица со списоком синонимов валют
		protected MySqlDataAdapter daSynonymCurrency;
		protected DataTable dtSynonymCurrency;

		protected MySqlDataAdapter daCore;
		protected MySqlCommandBuilder cbCore;
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
		protected MySqlCommandBuilder cbCoreCosts;
		protected DataTable dtCoreCosts;

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

		//Максимальное кол-во блокировок при 
		public int maxLockCount = 0;

		protected string priceFileName;

		//FormalizeSettings
		//имя прайса
		public string	priceName;
		//Имя клиента
		public string	clientShortName;
		//Код клиента
		public System.Int64	clientCode;
		//ключ правил разбора прайса
		protected System.Int64		formID;
		//ключ прайса
		public System.Int64		priceCode = -1;
		//родительский синоним : прайс-родитель, нужен для выбора различных параметров
		protected System.Int64		parentSynonym;
		//Кол-во распознаных позиций в прошлый раз
		protected System.Int64		posNum;
		//Сегмент прайса
		protected int				firmSegment;
		//производить формализацию по коду
		protected bool				formByCode;
		//валюта прайса
		protected string			priceCurrency;

		//Формат прайса
		protected string priceFmt;
		//Маска, которая накладывается на имя позиции
		protected string nameMask;
		//Запрещенные слова, которые могут быть в имени
		protected string forbWords;
		protected string[] forbWordsList = null;
		//как в прайсе поставщика метятся ожидаемые позиции
		protected string awaitPos;
		//как в прайсе поставщика метятся "плохие" позиции
		protected string junkPos;
		//Тип прайса : ассортиментный
		protected int    priceType;

		//Надо ли конвертировать полученную строку в ANSI
		protected bool convertedToANSI = false;


		protected ToughDate toughDate = null;
		protected ToughMask toughMask = null;

		protected ArrayList currentCoreCosts = null;
		protected ArrayList CoreCosts = null;
		



		/// <summary>
		/// Конструктор парсера
		/// </summary>
		/// <param name="PriceFileName"></param>
		/// <param name="conn"></param>
		/// <param name="mydr"></param>
		public BasePriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr)
		{
			//TODO: Все необходимые проверки вынести в конструкторы, чтобы не пытаться открыть прайс-файл

			//TODO: переделать конструктор, чтобы он не зависел от базы данных, т.е. передавать ему все, что нужно для чтения файла, чтобы парсер был самодостаточным

			priceFileName = PriceFileName;
			dtPrice = new DataTable();
			dbcMain = new OleDbConnection();
			MyConn = conn;
			dsMyDB = new DataSet();
			currentCoreCosts = new ArrayList();
			CoreCosts = new ArrayList();
			FieldNames = new string[Enum.GetNames(typeof(PriceFields)).Length];
			
			priceName = mydr.Rows[0][FormRules.colSelfPriceName].ToString();
			clientShortName = mydr.Rows[0][FormRules.colClientShortName].ToString();
			clientCode = Convert.ToInt64(mydr.Rows[0][FormRules.colClientCode]); 
			formByCode = Convert.ToBoolean(mydr.Rows[0][FormRules.colFormByCode]);
			formID = Convert.ToInt64(mydr.Rows[0][FormRules.colFormID]); 
			priceCode = Convert.ToInt64(mydr.Rows[0][FormRules.colSelfPriceCode]); 
			parentSynonym = Convert.ToInt64(mydr.Rows[0][FormRules.colParentSynonym]); 
			priceCurrency = mydr.Rows[0][FormRules.colCurrency].ToString();
			firmSegment = Convert.ToInt32(mydr.Rows[0][FormRules.colFirmSegment]);


			priceFmt = ((string)mydr.Rows[0][FormRules.colPriceFMT]).ToUpper();
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
			posNum = mydr.Rows[0][FormRules.colSelfPosNum] is DBNull ? 0 : Convert.ToInt64(mydr.Rows[0][FormRules.colSelfPosNum]);
			priceType = Convert.ToInt32(mydr.Rows[0][FormRules.colSelfFlag]);

			toughDate = new ToughDate();
			if (String.Empty != nameMask)
				toughMask = new ToughMask(nameMask, clientCode, priceCode, clientShortName, priceName);

			MySqlDataAdapter daPricesCost = new MySqlDataAdapter( String.Format("select * from {1} pc, {2} cfr where pc.PriceCode={0} and cfr.FR_ID = pc.PriceCode and cfr.PC_CostCode = pc.CostCode", priceCode, FormalizeSettings.tbPricesCosts, FormalizeSettings.tbCostsFormRules), MyConn );
			DataTable dtPricesCost = new DataTable("PricesCosts");
			daPricesCost.Fill(dtPricesCost);

			if (0 == dtPricesCost.Rows.Count && FormalizeSettings.ASSORT_FLG != priceType)
				throw new WarningFormalizeException(FormalizeSettings.CostsNotExistsError, clientCode, priceCode, clientShortName, priceName);
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


			//Если прайс не ассортиментный, то его надо проверить на базовую цену.
/*
			if (FormalizeSettings.ASSORT_FLG != priceType)
			{
				if (1 == currentCoreCosts.Count)
				{
					if ( !(currentCoreCosts[0] as CoreCost).baseCost )
						throw new WarningFormalizeException(FormalizeSettings.BaseCostNotExistsError, clientCode, priceCode, clientShortName, priceName);
				}
				else
				{
					int baseIndex = currentCoreCosts.IndexOf(new BaseCostFinder());
					if (-1 == baseIndex)
					{
						throw new WarningFormalizeException(FormalizeSettings.BaseCostNotExistsError, clientCode, priceCode, clientShortName, priceName);
					}
					else
					{
						int lastBaseIndex = currentCoreCosts.LastIndexOf(new BaseCostFinder());
						if (baseIndex != lastBaseIndex)
						{
							throw new WarningFormalizeException(
								String.Format(FormalizeSettings.DoubleBaseCostsError, 
									(currentCoreCosts[baseIndex] as CoreCost).costCode, 
									(currentCoreCosts[lastBaseIndex] as CoreCost).costCode), 
								clientCode, priceCode, clientShortName, priceName);
						}
						else
						{
							if (0 != baseIndex)
							{
								CoreCost tmp = (CoreCost)currentCoreCosts[baseIndex];
								currentCoreCosts.RemoveAt(baseIndex);
								currentCoreCosts.Insert(0, tmp);
							}
						}
					}
				}

				if (String.Empty == (currentCoreCosts[0] as CoreCost).fieldName)
					throw new WarningFormalizeException(FormalizeSettings.FieldNameBaseCostsError, clientCode, priceCode, clientShortName, priceName);
			}
*/			
		}

		/// <summary>
		/// Производит специализированное открытие прайса в зависимости от типа
		/// </summary>
		public abstract void Open();

		public void FillPrice(OleDbDataAdapter da)
		{
			bool res = false;
			int tryCount = 0;
			do
			{
				try
				{
					dtPrice.Clear();
					da.Fill(dtPrice);
					res = true;
				}
				catch(System.Runtime.InteropServices.InvalidComObjectException)
				{
					if (tryCount < FormalizeSettings.MinRepeatTranCount)
					{
						tryCount++;
						SimpleLog.Log( getParserID(), "Repeat Fill dtPrice on InvalidComObjectException");
						System.Threading.Thread.Sleep(500);
					}
					else
						throw;
				}
				catch(NullReferenceException)
				{
					if (tryCount < FormalizeSettings.MinRepeatTranCount)
					{
						tryCount++;
						SimpleLog.Log( getParserID(), "Repeat Fill dtPrice on NullReferenceException");
						System.Threading.Thread.Sleep(500);
					}
					else
						throw;
				}
			}while(!res);
		}

		/// <summary>
		/// Производится вставка данных в таблицу Core
		/// </summary>
		/// <param name="AFullCode"></param>
		/// <param name="AShortCode"></param>
		/// <param name="ACodeFirmCr"></param>
		/// <param name="ASynonymCode"></param>
		/// <param name="ASynonymFirmCrCode"></param>
		/// <param name="ABaseCost"></param>
		/// <param name="AJunk"></param>
		/// <param name="ACurrency"></param>
		public void InsertToCore(Int64 AFullCode, Int64 AShortCode, Int64 ACodeFirmCr, Int64 ASynonymCode, Int64 ASynonymFirmCrCode, decimal ABaseCost, string AJunk, string ACurrency)
		{
			if (String.Empty == AJunk)
				AJunk = (string)GetFieldValueObject(PriceFields.Junk);
					 
			DataRow drCore = dsMyDB.Tables["Core"].NewRow();

			drCore["FirmCode"] = priceCode;
			drCore["FullCode"] = AFullCode;
			drCore["ShortCode"] = AShortCode;
			drCore["CodeFirmCr"] = ACodeFirmCr;
			drCore["SynonymCode"] = ASynonymCode;
			drCore["SynonymFirmCrCode"] = ASynonymFirmCrCode;
			drCore["Code"] = GetFieldValue(PriceFields.Code);
			drCore["CodeCr"] = GetFieldValue(PriceFields.CodeCr);
			drCore["Unit"] = GetFieldValue(PriceFields.Unit);
			drCore["Volume"] = GetFieldValue(PriceFields.Volume);
			drCore["Quantity"] = GetFieldValue(PriceFields.Quantity);
			drCore["Note"] = GetFieldValue(PriceFields.Note);
			object dt = GetFieldValueObject(PriceFields.Period);
			string st;
			if (dt is DateTime)
				st = ((DateTime)dt).ToString("dd'.'MM'.'yyyy");
			else
				st = null;
			drCore["Period"] = st;
			drCore["Doc"] = GetFieldValueObject(PriceFields.Doc);

			drCore["Junk"] = AJunk;
			drCore["Await"] = GetFieldValueObject(PriceFields.Await);

			drCore["Currency"] = ACurrency;
			if ( !checkZeroCost(ABaseCost) )
				drCore["BaseCost"] = ABaseCost;
			drCore["MinBoundCost"] = GetFieldValueObject(PriceFields.MinBoundCost);


			dsMyDB.Tables["Core"].Rows.Add(drCore);
			if (priceType != FormalizeSettings.ASSORT_FLG)
				CoreCosts.Add( currentCoreCosts.Clone() );
			formCount++;
		}

		/// <summary>
		/// Вставка записи в Zero
		/// </summary>
		public void InsertToZero()
		{
			DataRow drZero = dtZero.NewRow();

			drZero["FirmCode"] = priceCode;
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
			drZero["Junk"] = GetFieldValueObject(PriceFields.Junk);
			drZero["Currency"] = GetFieldValueObject(PriceFields.Currency);

			dtZero.Rows.Add(drZero);

			zeroCount++;
		}

		/// <summary>
		/// Вставка в нераспознанные позиции
		/// </summary>
		/// <param name="AFullCode"></param>
		/// <param name="AShortCode"></param>
		/// <param name="ACodeFirmCr"></param>
		/// <param name="AStatus"></param>
		/// <param name="AJunk"></param>
		/// <param name="ACurrency"></param>
		public void InsertToUnrec(Int64 AFullCode, Int64 AShortCode, Int64 ACodeFirmCr, int AStatus, string AJunk, string ACurrency)
		{
			DataRow drUnrecExp = dsMyDB.Tables["UnrecExp"].NewRow();
			drUnrecExp["FirmCode"] = priceCode;
			drUnrecExp["Name1"] = GetFieldValue(PriceFields.Name1);
			drUnrecExp["FirmCr"] = GetFieldValue(PriceFields.FirmCr);
			drUnrecExp["Code"] = GetFieldValue(PriceFields.Code);
			drUnrecExp["CodeCr"] = GetFieldValue(PriceFields.CodeCr);
			drUnrecExp["Unit"] = GetFieldValue(PriceFields.Unit);
			drUnrecExp["Volume"] = GetFieldValue(PriceFields.Volume);
			drUnrecExp["Quantity"] = GetFieldValue(PriceFields.Quantity);
			drUnrecExp["Note"] = GetFieldValue(PriceFields.Note);
			drUnrecExp["Period"] = GetFieldValueObject(PriceFields.Period);
			drUnrecExp["Doc"] = GetFieldValueObject(PriceFields.Doc);


			drUnrecExp["Junk"] = AJunk;

			drUnrecExp["Currency"] = GetFieldValue(PriceFields.Currency);
			drUnrecExp["BaseCost"] = GetFieldValueObject(PriceFields.BaseCost);
			drUnrecExp["AddDate"] = DateTime.Now;

			drUnrecExp["Status"] = AStatus;
			drUnrecExp["Already"] = AStatus;
			drUnrecExp["TmpFullCode"] = AFullCode;
			drUnrecExp["TmpShortCode"] = AShortCode;
			drUnrecExp["TmpCodeFirmCr"] = ACodeFirmCr;
			drUnrecExp["TmpCurrency"] = ACurrency;

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
			newRow["FirmCode"] = priceCode;
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
			daForbidden = new MySqlDataAdapter(
				String.Format("SELECT FirmCode, LOWER(Forbidden) AS Forbidden FROM {1} WHERE FirmCode={0}", parentSynonym, FormalizeSettings.tbForbidden), MyConn);
			daForbidden.Fill(dsMyDB, "Forbidden");
			dtForbidden = dsMyDB.Tables["Forbidden"];

			daSynonym = new MySqlDataAdapter(
				String.Format("SELECT SynonymCode, LOWER(Synonym) AS Synonym, FullCode, ShortCode, Code, Junk, Date FROM {1} WHERE FirmCode={0}", parentSynonym, FormalizeSettings.tbSynonym), MyConn);
			daSynonym.Fill(dsMyDB, "Synonym");
			dtSynonym = dsMyDB.Tables["Synonym"];

			daSynonymFirmCr = new MySqlDataAdapter(
				String.Format("SELECT SynonymFirmCrCode, CodeFirmCr, LOWER(Synonym) AS Synonym FROM {1} WHERE FirmCode={0}", parentSynonym, FormalizeSettings.tbSynonymFirmCr), MyConn);
			daSynonymFirmCr.Fill(dsMyDB, "SynonymFirmCr");
			dtSynonymFirmCr = dsMyDB.Tables["SynonymFirmCr"];

			daSynonymCurrency = new MySqlDataAdapter(
				String.Format("SELECT Currency, LOWER(Synonym) AS Synonym FROM {1} WHERE FirmCode={0}", parentSynonym, FormalizeSettings.tbSynonymCurrency), MyConn);
			daSynonymCurrency.Fill(dsMyDB, "SynonymCurrency");
			dtSynonymCurrency = dsMyDB.Tables["SynonymCurrency"];

			//TODO: Сделать это одним запросом
				daCore = new MySqlDataAdapter(
				String.Format("SELECT * FROM {1}{2} WHERE FirmCode={0} LIMIT 0", priceCode, FormalizeSettings.tbCore, firmSegment), MyConn);
			cbCore = new MySqlCommandBuilder(daCore);
			daCore.InsertCommand = cbCore.GetInsertCommand();
			daCore.InsertCommand.Parameters["MinBoundCost"].MySqlDbType = MySqlDbType.Decimal;
			daCore.InsertCommand.Parameters["MinBoundCost"].DbType = DbType.Decimal;
			daCore.Fill(dsMyDB, "Core");
			dtCore = dsMyDB.Tables["Core"];

			daUnrecExp = new MySqlDataAdapter(
				String.Format("SELECT * FROM {1} WHERE FirmCode={0} LIMIT 0", priceCode, FormalizeSettings.tbUnrecExp), MyConn);
			cbUnrecExp = new MySqlCommandBuilder(daUnrecExp);
			daUnrecExp.Fill(dsMyDB, "UnrecExp");
			dtUnrecExp = dsMyDB.Tables["UnrecExp"];

			daZero = new MySqlDataAdapter(
				String.Format("SELECT * FROM {1} WHERE FirmCode={0} LIMIT 0", priceCode, FormalizeSettings.tbZero), MyConn);
			cbZero = new MySqlCommandBuilder(daZero);
			daZero.Fill(dsMyDB, "Zero");
			dtZero = dsMyDB.Tables["Zero"];

			daForb = new MySqlDataAdapter(
				String.Format("SELECT * FROM {1} WHERE FirmCode={0} LIMIT 0", priceCode, FormalizeSettings.tbForb), MyConn);
			cbForb = new MySqlCommandBuilder(daForb);
			daForb.Fill(dsMyDB, "Forb");
			dtForb = dsMyDB.Tables["Forb"];
			dtForb.Constraints.Add("ForbName", new DataColumn[] {dtForb.Columns["Forb"]}, false);

			daCoreCosts = new MySqlDataAdapter( String.Format("SELECT * FROM {0} LIMIT 0", FormalizeSettings.tbCoreCosts), MyConn);
			cbCoreCosts = new MySqlCommandBuilder(daCoreCosts);
			daCoreCosts.Fill(dsMyDB, "CoreCosts");
			dtCoreCosts = dsMyDB.Tables["CoreCosts"];
		}

		public void TryCommand(MySqlCommand cmd)
		{
			bool res = false;
			int tryCount = 0;
			int affectedrows = -1;
			do{
//				try
//				{
					affectedrows = cmd.ExecuteNonQuery();
					SimpleLog.Log(getParserID(), "AffectedRows = {0}  Command = {1}", affectedrows, cmd.CommandText);
					res = true;					
//				}
//				catch(MySqlException MyError)
//				{
//					//(1213 == MyError.Number) || (1205 == MyError.Number)
//					if ( (tryCount <= FormalizeSettings.MaxRepeatTranCount) && ( !MyError.IsFatal ) )
//					{
//						tryCount++;
//						System.Threading.Thread.Sleep(tryCount*1000);
//					}
//					else
//						throw;
//				}
			}while(!res);

			if (tryCount > maxLockCount)
				maxLockCount = tryCount;
		}

		public MySqlDataReader TryReader(MySqlCommand cmd)
		{
			bool res = false;
			MySqlDataReader reader = null;
			int tryCount = 0;
			do
			{
//				try
//				{
					reader = cmd.ExecuteReader();
					res = true;					
//				}
//				catch(MySqlException MyError)
//				{
//					//(1213 == MyError.Number) || (1205 == MyError.Number)
//					if ( (tryCount <= FormalizeSettings.MaxRepeatTranCount) && ( !MyError.IsFatal ) )
//					{
//						tryCount++;
//						System.Threading.Thread.Sleep(tryCount*1000);
//					}
//					else
//						throw;
//				}
			}while(!res);

			if (tryCount > maxLockCount)
				maxLockCount = tryCount;

			return reader;
		}

		public void onUpdating(object sender, MySqlRowUpdatingEventArgs e)
		{
			if (e.Status != UpdateStatus.Continue)
			{
				SimpleLog.Log( getParserID(), e.Errors.ToString());
			}
		}

		public void onUpdated(object sender, MySqlRowUpdatedEventArgs e)
		{
			if (e.Status != UpdateStatus.Continue)
			{
				SimpleLog.Log( getParserID(), e.Errors.ToString());
			}
		}

		public void TryUpdate(MySqlDataAdapter da, DataTable dt, MySqlTransaction tran)
		{
			bool res = false;
			int tryCount = 0;
			int affectedrows = -1;
			do
			{
//				try
//				{

					da.SelectCommand.Transaction = tran;
					affectedrows = da.Update(dt);
					SimpleLog.Log(getParserID(), "AffectedRows = {0}  Table = {1}", affectedrows, dt.TableName);
					res = true;					
//				}
//				catch(MySqlException MyError)
//				{
//					SimpleLog.Log( getParserID(), "Try update: ErrNo = {0}  IsFatal = {1}", MyError.Number, MyError.IsFatal);
//					//(1213 == MyError.Number) || (1205 == MyError.Number)
//					if ( (tryCount <= FormalizeSettings.MaxRepeatTranCount) && ( !MyError.IsFatal ) )
//					{
//						tryCount++;
//						SimpleLog.Log( getParserID(), "Try update: tryCount = {0}", tryCount);
//						System.Threading.Thread.Sleep(tryCount*1000);
//					}
//					else
//						throw;
//				}
			}while(!res);

			if (tryCount > maxLockCount)
				maxLockCount = tryCount;
		}

		/// <summary>
		/// Окончание разбора прайса, с последующим логированием статистики
		/// </summary>
		public void Finalize()
		{
			if (FormalizeSettings.CheckZero && (zeroCount > (formCount + unformCount + zeroCount) * 0.95) )
			{
				//myTrans.Rollback();
				throw new RollbackFormalizeException(FormalizeSettings.ZeroRollbackError, clientCode, priceCode, clientShortName, priceName, this.formCount, this.zeroCount, this.unformCount, this.forbCount);
			}
			else
			{
				if (formCount * 1.3 < posNum)
				{
					//myTrans.Rollback();
					throw new RollbackFormalizeException(FormalizeSettings.PrevFormRollbackError, clientCode, priceCode, clientShortName, priceName, this.formCount, this.zeroCount, this.unformCount, this.forbCount);
				}
				else
				{

					bool res = false;
					int tryCount = 0;
					do
					{
						SimpleLog.Log( getParserID(), "Finalize started.");

						myTrans = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);

						try
						{
							//TODO: Сделать одним запросом
							MySqlCommand mcClear = new MySqlCommand(String.Format("delete from {1}{2} where FirmCode={0}", priceCode, FormalizeSettings.tbCore, firmSegment), MyConn, myTrans);
							TryCommand(mcClear);

							mcClear.CommandText = String.Format("delete from {1} where FirmCode={0}", priceCode, FormalizeSettings.tbZero );
							TryCommand(mcClear);

							mcClear.CommandText = String.Format("delete from {1} where FirmCode={0}", priceCode, FormalizeSettings.tbForb);
							TryCommand(mcClear);

							//mcClear.CommandText = String.Format("delete cc from {1} cc, {2} pc where cc.pc_costcode =pc.costcode and pc.pricecode = {0}", priceCode, FormalizeSettings.tbCoreCosts, FormalizeSettings.tbPricesCosts);
							//TryCommand(mcClear);
					
							foreach(CoreCost c in currentCoreCosts)
							{
								mcClear.CommandText = String.Format("delete from {0} where pc_costcode = {1}", FormalizeSettings.tbCoreCosts, c.costCode);
								TryCommand(mcClear);
							}

							SimpleLog.Log( getParserID(), "Finalize started: delete from UnrecExp");
							MySqlDataAdapter daUnrecExpBlock = new MySqlDataAdapter(String.Format("SELECT BlockBy FROM {1} where FirmCode={0} and BlockBy <> '' limit 1", priceCode, FormalizeSettings.tbUnrecExp), MyConn);
							daUnrecExpBlock.SelectCommand.Transaction = myTrans;
							DataTable dtUnrecExpBlock = new DataTable();
							daUnrecExpBlock.Fill(dtUnrecExpBlock);

							MySqlDataAdapter daBlockedPrice = new MySqlDataAdapter(String.Format("SELECT * FROM {1} where PriceCode={0} limit 1", priceCode, FormalizeSettings.tbBlockedPrice), MyConn);
							daBlockedPrice.SelectCommand.Transaction = myTrans;
							DataTable dtBlockedPrice = new DataTable();
							daBlockedPrice.Fill(dtBlockedPrice);

							if ( (dtUnrecExpBlock.Rows.Count == 0) && (dtBlockedPrice.Rows.Count == 0) )
							{
								mcClear.CommandText = String.Format("delete from {1} where FirmCode={0}", priceCode, FormalizeSettings.tbUnrecExp);
								TryCommand(mcClear);
							}

							SimpleLog.Log( getParserID(), "Finalize started: {0}", "Core");
//							daCore.RowUpdating += new MySqlRowUpdatingEventHandler(onUpdating);
//							daCore.RowUpdated += new MySqlRowUpdatedEventHandler(onUpdated);
							TryUpdate(daCore, dtCore.Copy(), myTrans);
							SimpleLog.Log( getParserID(), "Finalize started: {0}", "Forb");
							TryUpdate(daForb, dtForb.Copy(), myTrans);
							SimpleLog.Log( getParserID(), "Finalize started: {0}", "Zero" );
							TryUpdate(daZero, dtZero.Copy(), myTrans);
							SimpleLog.Log( getParserID(), "Finalize started: {0}", "UnrecExp" );
							TryUpdate(daUnrecExp, dtUnrecExp.Copy(), myTrans);

							if (priceType != FormalizeSettings.ASSORT_FLG)
							{
								SimpleLog.Log( getParserID(), "Finalize started.prepare: {0}", "CoreCosts" );
								DataRow drCore;
								DataRow drCoreCost;
								dtCoreCosts.MinimumCapacity = dtCore.Rows.Count*currentCoreCosts.Count;
								ArrayList currCC;
								//Здесь вставить проверку того, что не надо заполнять цены два раза или обновлять их
								for(int i = 0; i<=dtCore.Rows.Count-1; i++)
								{
									drCore = dtCore.Rows[i];
									currCC = (ArrayList)CoreCosts[i];
									foreach(CoreCost c in currCC)
									{
										drCoreCost = dtCoreCosts.NewRow();
										drCoreCost["Core_ID"] = drCore["ID"];
										drCoreCost["PC_CostCode"] = c.costCode;
										if (c.cost > 0)
											drCoreCost["Cost"] = c.cost;
										else
											drCoreCost["Cost"] =  DBNull.Value;
										dtCoreCosts.Rows.Add(drCoreCost);
									}
								}

								SimpleLog.Log( getParserID(), "Finalize started: {0}", "CoreCosts" );
								//TryUpdate(daCoreCosts, dtCoreCosts.Copy());
							}

							mcClear.CommandText = String.Format("UPDATE {2} SET PosNum={0} , DateLastForm=NOW() WHERE FirmCode={1}", formCount, priceCode, FormalizeSettings.tbFormRules);
							TryCommand(mcClear);

							SimpleLog.Log( getParserID(), "Finalize started: {0}", "Commit");
							myTrans.Commit();
							res = true;					
						}
						catch(MySqlException MyError)
						{
							if ( (tryCount <= FormalizeSettings.MaxRepeatTranCount) && ( (1213 == MyError.Number) || (1205 == MyError.Number) ) )
							{
								tryCount++;
								SimpleLog.Log( getParserID(), "Try transaction: tryCount = {0}", tryCount);
								try
								{ 
									myTrans.Rollback();
								}
								catch(Exception ex)
								{
									SimpleLog.Log( getParserID(), "Error on rollback = {0}", ex);
								}
								System.Threading.Thread.Sleep(tryCount*1000);
							}
							else
								throw;
						}
					}while(!res);

					if (tryCount > maxLockCount)
						maxLockCount = tryCount;
				}
			}

		}

		/// <summary>
		/// Формализование прайса
		/// </summary>
		public void Formalize()
		{
			DateTime tmOpen = DateTime.UtcNow;
			try
			{
				Open();
				if (null != toughMask)
					toughMask.Analyze( GetFieldRawValue(PriceFields.Name1) );
			}
			finally
			{
				SimpleLog.Log( getParserID(), "Open time: {0}", DateTime.UtcNow.Subtract(tmOpen) );
			}

			try
			{
				MyConn.Open();

				try
				{
					myTrans = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);

					DateTime tmPrepare = DateTime.UtcNow;
					try
					{
						try
						{
							Prepare();					
						}
						finally
						{
							myTrans.Commit();
						}
					}
					finally
					{
						SimpleLog.Log( getParserID(), "Prepare time: {0}", DateTime.UtcNow.Subtract(tmPrepare) );
					}


					DateTime tmFormalize = DateTime.UtcNow;
					try
					{
						UnrecExpStatus st;
						decimal currBaseCost = -1m;
						string PosName, Junk = String.Empty, Currency = String.Empty;
						int costCount;
						Int64 FullCode = -1, ShortCode = -1, SynonymCode = -1, CodeFirmCr = -1, SynonymFirmCrCode = -1;
						string strCode, strName1, strOriginalName, strFirmCr, strCurrency;
						do
						{
							st = UnrecExpStatus.NOT_FORM;
							PosName = GetFieldValue(PriceFields.Name1, true);

							if ((null != PosName) && (String.Empty != PosName.Trim()) && (!IsForbidden(PosName)))
							{
								if (priceType != FormalizeSettings.ASSORT_FLG)
								{
									costCount = ProcessCosts();
									//Если кол-во ненулевых цен = 0
									if ( 0 == costCount )
									{
										InsertToZero();
										continue;
									}
									currBaseCost = (currentCoreCosts[0] as CoreCost).cost;
								}
								else
									currBaseCost = -1m;

								strCode = GetFieldValue(PriceFields.Code);
								strName1 = GetFieldValue(PriceFields.Name1, true);
								strOriginalName = GetFieldValue(PriceFields.OriginalName, true);
							
								if (GetFullCode( strCode, strName1, strOriginalName, out FullCode,out  ShortCode,out  SynonymCode, out Junk))
									st = UnrecExpStatus.NAME_FORM;

								strFirmCr = GetFieldValue(PriceFields.FirmCr, true);
								if (GetCodeFirmCr(strFirmCr, out CodeFirmCr, out SynonymFirmCrCode))
									st = st | UnrecExpStatus.FIRM_FORM;

								strCurrency = GetFieldValue(PriceFields.Currency, true);
								if (GetCurrency(strCurrency, out Currency))
									st = st | UnrecExpStatus.CURR_FORM;

								if (((st & UnrecExpStatus.NAME_FORM) == UnrecExpStatus.NAME_FORM) && ((st & UnrecExpStatus.CURR_FORM) == UnrecExpStatus.CURR_FORM))
									InsertToCore(FullCode, ShortCode, CodeFirmCr, SynonymCode, SynonymFirmCrCode, currBaseCost, Junk, Currency);
								else
									unformCount++;

								if ((st & UnrecExpStatus.FULL_FORM) != UnrecExpStatus.FULL_FORM)
									InsertToUnrec(FullCode, ShortCode, CodeFirmCr, (int)st, Junk, Currency);

							}
							else
								if ((null != PosName) && (String.Empty != PosName.Trim()))
									InsertIntoForb(PosName);

						}
						while(Next());
					}
					finally
					{
						SimpleLog.Log( getParserID(), "Formalize time: {0}", DateTime.UtcNow.Subtract(tmFormalize) );
					}

					DateTime tmFinalize = DateTime.UtcNow;
					try
					{
						Finalize();
					}
					finally
					{
						SimpleLog.Log( getParserID(), "Finalize time: {0}", DateTime.UtcNow.Subtract(tmFinalize) );
					}
				}
				catch
				{
					try
					{
						myTrans.Rollback();
					}
					catch
					{
					}
					throw;
				}
			}
			finally
			{
				MyConn.Close();
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
			else
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
			else
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
					return GetAwaitValue();

				case (int)PriceFields.Junk:
					return GetJunkValue();

				case (int)PriceFields.Code:
				case (int)PriceFields.CodeCr:
				case (int)PriceFields.CountryCr:
				case (int)PriceFields.Currency:
				case (int)PriceFields.Doc:
				case (int)PriceFields.FirmCr:
				case (int)PriceFields.Name1:
				case (int)PriceFields.Name2:
				case (int)PriceFields.Name3:
				case (int)PriceFields.Note:
				case (int)PriceFields.Quantity:
				case (int)PriceFields.Unit:
				case (int)PriceFields.Volume:
				case (int)PriceFields.OriginalName:
					return GetFieldValue(PF);

				case (int)PriceFields.BaseCost:
				case (int)PriceFields.MinBoundCost:
					return ProcessCost(GetFieldRawValue(PF));

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
			if (null != CostValue)
				try
				{
					NumberFormatInfo nfi = CultureInfo.CurrentCulture.NumberFormat;			
					String res = String.Empty;
					foreach(Char c in CostValue.ToCharArray())
					{
						if (Char.IsDigit(c))
							res = String.Concat(res, c);
						else
						{
							if ( (!Char.IsWhiteSpace(c)) && (res != String.Empty) && (-1 == res.IndexOf(nfi.CurrencyDecimalSeparator)) )
								res = String.Concat(res, nfi.CurrencyDecimalSeparator);
						}
					}
					decimal d = Decimal.Parse(res, NumberStyles.Currency);
					d = Math.Round(d, 2);
					return d;
				}
				catch
				{
					return DBNull.Value;
				}
			else
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
				if (null != pv && String.Empty != pv)
				{
					res = toughDate.Analyze( pv );
					if (!DateTime.MaxValue.Equals(res))
						return res;
				}
			}
			if (null != PeriodValue && String.Empty != PeriodValue)
			{
				res = toughDate.Analyze( PeriodValue );
				if (DateTime.MaxValue.Equals(res))
				{
					//throw new WarningFormalizeException(String.Format(FormalizeSettings.PeriodParseError, PeriodValue), clientCode, priceCode, clientShortName, priceName);
					//SendMessageToOperator("Предупреждение", String.Format(FormalizeSettings.PeriodParseError, PeriodValue), clientCode, priceCode, clientShortName, priceName);
					return DBNull.Value;
				}
				return res;
			}
			else
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
			else
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
					else
						return Value;		
				}
				else
					return Value;		

			}
			else
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
		/// <param name="AFullCode"></param>
		/// <param name="AShortCode"></param>
		/// <param name="ASynonymCode"></param>
		/// <param name="AJunk"></param>
		/// <returns></returns>
		public bool GetFullCode(string ACode, string AName, string AOriginalName, out Int64 AFullCode, out Int64 AShortCode, out Int64 ASynonymCode, out string AJunk)
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
				AFullCode = Convert.ToInt64(dr[0]["FullCode"]);
				AShortCode = Convert.ToInt64(dr[0]["ShortCode"]);
				ASynonymCode = Convert.ToInt64(dr[0]["SynonymCode"]);
				AJunk = dr[0]["Junk"].ToString();
				return true;
			}
			else
			{
				AFullCode = 0;
				AShortCode = 0;
				ASynonymCode = 0;
				AJunk = String.Empty;
				return false;
			}

		}

		/// <summary>
		/// Смогли ли мы распознать производителя по названию?
		/// </summary>
		/// <param name="FirmCr"></param>
		/// <param name="ACodeFirmCr"></param>
		/// <param name="ASynonymFirmCrCode"></param>
		/// <returns></returns>
		public bool GetCodeFirmCr(string FirmCr,out Int64 ACodeFirmCr, out Int64 ASynonymFirmCrCode)
		{
			DataRow[] dr = null;
			if (null != FirmCr)
				dr = dsMyDB.Tables["SynonymFirmCr"].Select(String.Format("Synonym = '{0}'", FirmCr.Replace("'", "''")));

			if ((null != dr) && (dr.Length > 0))
			{
				ACodeFirmCr = Convert.ToInt64(dr[0]["CodeFirmCr"]);
				ASynonymFirmCrCode = Convert.ToInt64(dr[0]["SynonymFirmCrCode"]);
				return true;
			}
			else
			{
				ACodeFirmCr = 1;
				ASynonymFirmCrCode = 0;
				return (null == FirmCr || String.Empty == FirmCr) ? true : false;
			}
		}

		/// <summary>
		/// Смогли ли мы распознать валюту позиции?
		/// </summary>
		/// <param name="ACurrency"></param>
		/// <param name="OCurrency"></param>
		/// <returns></returns>
		public bool GetCurrency(string  ACurrency, out string  OCurrency)
		{
			OCurrency = priceCurrency;
			if (FormalizeSettings.DIFF_CURR == OCurrency.ToLower())
			{
				if (null != ACurrency)
				{
					DataRow[] dr = dsMyDB.Tables["SynonymCurrency"].Select(String.Format("Synonym = '{0}'", ACurrency.Replace("'", "''")));
					if ((null != dr) && (dr.Length > 0))
						OCurrency = dr[0]["Currency"].ToString();
					else
						OCurrency = String.Empty;
				}
				else
					OCurrency = String.Empty;
			}
			return (String.Empty != OCurrency);
		}

		/// <summary>
		/// Получить значение поля Junk
		/// </summary>
		/// <returns></returns>
		public string GetJunkValue()
		{
			string JunkValue = "";
			object t = GetFieldValueObject(PriceFields.Period);			
			if (t is DateTime)
			{
				DateTime dt = (DateTime)t;
				TimeSpan ts = DateTime.Now.Subtract(dt);
				if (Math.Abs(ts.Days) < 180)
					JunkValue = FormalizeSettings.JUNK;
			}
			if (String.Empty == JunkValue)
			{
				try
				{
					Regex re = new Regex(junkPos);
					Match m = re.Match( GetFieldValue(PriceFields.Junk) );
					JunkValue = (m.Success) ? FormalizeSettings.JUNK : "";
				}
				catch
				{
				}
			}

			return JunkValue;
		}

		public string GetAwaitValue()
		{
			string AwaitValue = "";

			try
			{
				Regex re = new Regex(awaitPos);
				Match m = re.Match( GetFieldValue(PriceFields.Await) );
				AwaitValue = (m.Success) ? FormalizeSettings.AWAIT : "";
			}
			catch
			{
			}

			return AwaitValue;
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
				c.cost = -1;
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
						if ( !checkZeroCost( (decimal)costValue ) )
						{
							c.cost = (decimal)costValue;
							res++;
						}
					}
				}
			}
			return res;
		}

		public string getParserID()
		{
			return  String.Format("{0}.{1}", this.GetType().Name, priceCode);
		}

		public bool checkZeroCost(decimal cost)
		{
			return (cost < 0 || Math.Abs(Decimal.Zero-cost) < 0.01m);
		}

		public void SendMessageToOperator(string SubjectPrefix, string Message, long ClientCode, long PriceCode, string ClientName, string PriceName)
		{
			try
			{
				MailMessage m = new MailMessage();
				m.From = FormalizeSettings.FromEmail;
				m.To = FormalizeSettings.RepEmail;
				m.Subject = String.Format("{0} {1}", SubjectPrefix, PriceCode);
				m.BodyFormat = MailFormat.Text;
				m.BodyEncoding = System.Text.Encoding.GetEncoding(1251);
				m.Body = String.Format(
@"Код фирмы       : {0}
Код прайса      : {1}
Название прайса : {2}
Дата события    : {3}
Ошибка          : {4}", 
					ClientCode,
					PriceCode,
					String.Format("{0} ({1})", ClientName, PriceName),
					DateTime.Now,
					Message);
				SmtpMail.SmtpServer = "box.analit.net";
				SmtpMail.Send(m);
			}
			catch(Exception e)
			{
				SimpleLog.Log(getParserID(), "Error on SendMessageToOperator : {0}", e);
			}
		}
	}
}
