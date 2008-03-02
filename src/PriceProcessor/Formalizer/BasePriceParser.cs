using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Data.OleDb;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Net.Mail;
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
		OriginalName,
		VitallyImportant,
		RequestRatio,
		RegistryCost,
		MaxBoundCost,
		OrderCost,
		MinOrderCount
	}

	public enum CostTypes : int
	{ 
		MultiColumn = 0,
		MiltiFile = 1
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
		public static string colSelfVitallyImportantMask = "SelfVitallyImportantMask";
		public static string colSelfPosNum = "SelfPosNum";
		public static string colSelfFlag = "SelfFlag";
		public static string colDelimiter = "Delimiter";
		public static string colBillingStatus = "BillingStatus";
		public static string colFirmStatus = "FirmStatus";
		public static string colFirmSegment = "FirmSegment";
		public static string colHasParentPrice = "HasParentPrice";
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

		#region ICloneable Members

		public object Clone()
		{
			CoreCost ccNew = new CoreCost(this.costCode, this.costName, this.baseCost, this.fieldName, this.txtBegin, this.txtEnd);
			ccNew.cost = this.cost;
			return ccNew;
		}

		#endregion
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
		//индекс цены с таким же кодом как у прайса в списке цен (currentCoreCosts)
		public int				priceCodeCostIndex = -1;
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
		//как в прайсе поставщика метятся жизненно-важные позиции
		protected string vitallyImportantMask;
		//Тип прайса : ассортиментный
		protected int    priceType;
		//Является ли текущий прайс ценовой колонкой прайса-родителя
		protected bool hasParentPrice;
		//Тип ценовых колонок прайса-родителя: 1 - мультиколоночный, 2 - многофайловый
		protected int costType;

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
			hasParentPrice = Convert.ToBoolean(mydr.Rows[0][FormRules.colHasParentPrice]);
			costType = Convert.ToInt32(mydr.Rows[0][FormRules.colCostType]);


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
			vitallyImportantMask = mydr.Rows[0][FormRules.colSelfVitallyImportantMask].ToString();
			posNum = mydr.Rows[0][FormRules.colSelfPosNum] is DBNull ? 0 : Convert.ToInt64(mydr.Rows[0][FormRules.colSelfPosNum]);
			priceType = Convert.ToInt32(mydr.Rows[0][FormRules.colSelfFlag]);

			toughDate = new ToughDate();
			if (String.Empty != nameMask)
				toughMask = new ToughMask(nameMask, clientCode, priceCode, clientShortName, priceName);

			string selectCostFormRulesSQL = String.Empty;
			if (!hasParentPrice && (costType == (int)CostTypes.MultiColumn))
				selectCostFormRulesSQL = String.Format("select * from {1} pc, {2} cfr where pc.ShowPriceCode={0} and cfr.FR_ID = pc.PriceCode and cfr.PC_CostCode = pc.CostCode", priceCode, FormalizeSettings.tbPricesCosts, FormalizeSettings.tbCostsFormRules);
			else
				selectCostFormRulesSQL = String.Format("select * from {1} pc, {2} cfr where pc.PriceCode={0} and cfr.FR_ID = pc.PriceCode and cfr.PC_CostCode = pc.CostCode", priceCode, FormalizeSettings.tbPricesCosts, FormalizeSettings.tbCostsFormRules);

			MySqlDataAdapter daPricesCost = new MySqlDataAdapter( selectCostFormRulesSQL, MyConn );
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

			//Если прайс является не ассортиментным прайсом-родителем с мультиколоночными ценами, то его надо проверить на базовую цену
			if ((FormalizeSettings.ASSORT_FLG != priceType) && !hasParentPrice && (costType == (int)CostTypes.MultiColumn))
			{
				if (1 == currentCoreCosts.Count)
				{
					if ( !(currentCoreCosts[0] as CoreCost).baseCost )
						throw new WarningFormalizeException(FormalizeSettings.BaseCostNotExistsError, clientCode, priceCode, clientShortName, priceName);
				}
				else
				{
					CoreCost[] bc = Array.FindAll<CoreCost>((CoreCost[])currentCoreCosts.ToArray(typeof(CoreCost)), delegate(CoreCost cc) { return cc.baseCost; });
					if (bc.Length == 0)
					{
						throw new WarningFormalizeException(FormalizeSettings.BaseCostNotExistsError, clientCode, priceCode, clientShortName, priceName);
					}
					else
						if (bc.Length > 1)
						{
							throw new WarningFormalizeException(
								String.Format(FormalizeSettings.DoubleBaseCostsError,
									(bc[0] as CoreCost).costCode,
									(bc[1] as CoreCost).costCode),
								clientCode, priceCode, clientShortName, priceName);
						}
						else
						{
							currentCoreCosts.Remove(bc[0]);
							currentCoreCosts.Insert(0, bc[0]);
						}

				}

				if (((this is TXTFPriceParser) && (((currentCoreCosts[0] as CoreCost).txtBegin == -1) || ((currentCoreCosts[0] as CoreCost).txtEnd == -1))) || (!(this is TXTFPriceParser) && (String.Empty == (currentCoreCosts[0] as CoreCost).fieldName)))
					throw new WarningFormalizeException(FormalizeSettings.FieldNameBaseCostsError, clientCode, priceCode, clientShortName, priceName);

				priceCodeCostIndex = Array.FindIndex<CoreCost>((CoreCost[])currentCoreCosts.ToArray(typeof(CoreCost)), delegate(CoreCost cc) { return cc.costCode == priceCode; });
				if (priceCodeCostIndex == -1)
					priceCodeCostIndex = 0;
			}
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
		/// <param name="AProductId"></param>
		/// <param name="AShortCode"></param>
		/// <param name="ACodeFirmCr"></param>
		/// <param name="ASynonymCode"></param>
		/// <param name="ASynonymFirmCrCode"></param>
		/// <param name="ABaseCost"></param>
		/// <param name="AJunk"></param>
		/// <param name="ACurrency"></param>
		public void InsertToCore(Int64 AProductId, Int64 ACodeFirmCr, Int64 ASynonymCode, Int64 ASynonymFirmCrCode, decimal ABaseCost, bool AJunk, string ACurrency)
		{
			if (!AJunk)
				AJunk = (bool)GetFieldValueObject(PriceFields.Junk);
					 
			DataRow drCore = dsMyDB.Tables["Core"].NewRow();

			drCore["PriceCode"] = priceCode;
			drCore["ProductId"] = AProductId;
			drCore["CodeFirmCr"] = ACodeFirmCr;
			drCore["SynonymCode"] = ASynonymCode;
			drCore["SynonymFirmCrCode"] = ASynonymFirmCrCode;
			drCore["Code"] = GetFieldValue(PriceFields.Code);
			drCore["CodeCr"] = GetFieldValue(PriceFields.CodeCr);
			drCore["Unit"] = GetFieldValue(PriceFields.Unit);
			drCore["Volume"] = GetFieldValue(PriceFields.Volume);
			drCore["Quantity"] = GetFieldValueObject(PriceFields.Quantity);
			drCore["Note"] = GetFieldValue(PriceFields.Note);
			drCore["VitallyImportant"] = GetFieldValueObject(PriceFields.VitallyImportant);
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
			if (dt is DateTime)
				st = ((DateTime)dt).ToString("dd'.'MM'.'yyyy");
			else
				st = null;
			drCore["Period"] = st;
			drCore["Doc"] = GetFieldValueObject(PriceFields.Doc);

			drCore["Junk"] = GetJunkValueAsString( AJunk );
			drCore["Await"] = GetAwaitValueAsString( (bool)GetFieldValueObject(PriceFields.Await) );

			drCore["Currency"] = ACurrency;
			if ( !checkZeroCost(ABaseCost) )
				drCore["BaseCost"] = ABaseCost;
			drCore["MinBoundCost"] = GetFieldValueObject(PriceFields.MinBoundCost);


			dsMyDB.Tables["Core"].Rows.Add(drCore);
			if (priceType != FormalizeSettings.ASSORT_FLG)
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

			drZero["PriceCode"] = priceCode;
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
			drZero["Currency"] = GetFieldValueObject(PriceFields.Currency);

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
		/// <param name="ACurrency"></param>
		public void InsertToUnrec(Int64 AProductId, Int64 ACodeFirmCr, int AStatus, bool AJunk, string ACurrency)
		{
			DataRow drUnrecExp = dsMyDB.Tables["UnrecExp"].NewRow();
			drUnrecExp["PriceCode"] = priceCode;
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

			drUnrecExp["Currency"] = GetFieldValue(PriceFields.Currency);
			drUnrecExp["BaseCost"] = GetFieldValueObject(PriceFields.BaseCost);
			drUnrecExp["AddDate"] = DateTime.Now;

			drUnrecExp["Status"] = AStatus;
			drUnrecExp["Already"] = AStatus;
			drUnrecExp["TmpProductId"] = AProductId;
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
			newRow["PriceCode"] = priceCode;
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
			SimpleLog.Log(getParserID(), "get Forbidden");
			daForbidden = new MySqlDataAdapter(
				String.Format("SELECT PriceCode, LOWER(Forbidden) AS Forbidden FROM {1} WHERE PriceCode={0}", parentSynonym, FormalizeSettings.tbForbidden), MyConn);
			daForbidden.Fill(dsMyDB, "Forbidden");
			dtForbidden = dsMyDB.Tables["Forbidden"];

			SimpleLog.Log(getParserID(), "get Synonym");
			daSynonym = new MySqlDataAdapter(
				String.Format("SELECT SynonymCode, LOWER(Synonym) AS Synonym, ProductId, Junk FROM {1} WHERE PriceCode={0}", parentSynonym, FormalizeSettings.tbSynonym), MyConn);
			daSynonym.Fill(dsMyDB, "Synonym");
			dtSynonym = dsMyDB.Tables["Synonym"];

			SimpleLog.Log(getParserID(), "get SynonymFirmCr");
			daSynonymFirmCr = new MySqlDataAdapter(
				String.Format("SELECT SynonymFirmCrCode, CodeFirmCr, LOWER(Synonym) AS Synonym FROM {1} WHERE PriceCode={0}", parentSynonym, FormalizeSettings.tbSynonymFirmCr), MyConn);
			daSynonymFirmCr.Fill(dsMyDB, "SynonymFirmCr");
			dtSynonymFirmCr = dsMyDB.Tables["SynonymFirmCr"];

			SimpleLog.Log(getParserID(), "get Core");
			daCore = new MySqlDataAdapter(
				String.Format("SELECT * FROM {1} WHERE PriceCode={0} LIMIT 0", priceCode, FormalizeSettings.tbCore), MyConn);
			daCore.Fill(dsMyDB, "Core");
			dtCore = dsMyDB.Tables["Core"];

			SimpleLog.Log(getParserID(), "get UnrecExp");
			daUnrecExp = new MySqlDataAdapter(
				String.Format("SELECT * FROM {1} WHERE PriceCode={0} LIMIT 0", priceCode, FormalizeSettings.tbUnrecExp), MyConn);
			cbUnrecExp = new MySqlCommandBuilder(daUnrecExp);
			daUnrecExp.InsertCommand = cbUnrecExp.GetInsertCommand();
			daUnrecExp.InsertCommand.CommandTimeout = 0;
			daUnrecExp.Fill(dsMyDB, "UnrecExp");
			dtUnrecExp = dsMyDB.Tables["UnrecExp"];
			dtUnrecExp.Columns["AddDate"].DataType = typeof(DateTime);

			SimpleLog.Log(getParserID(), "get Zero");
			daZero = new MySqlDataAdapter(
				String.Format("SELECT * FROM {1} WHERE PriceCode={0} LIMIT 0", priceCode, FormalizeSettings.tbZero), MyConn);
			cbZero = new MySqlCommandBuilder(daZero);
			daZero.InsertCommand = cbZero.GetInsertCommand();
			daZero.InsertCommand.CommandTimeout = 0;
			daZero.Fill(dsMyDB, "Zero");
			dtZero = dsMyDB.Tables["Zero"];

			SimpleLog.Log(getParserID(), "get Forb");
			daForb = new MySqlDataAdapter(
				String.Format("SELECT * FROM {1} WHERE PriceCode={0} LIMIT 0", priceCode, FormalizeSettings.tbForb), MyConn);
			cbForb = new MySqlCommandBuilder(daForb);
			daForb.InsertCommand = cbForb.GetInsertCommand();
			daForb.InsertCommand.CommandTimeout = 0;
			daForb.Fill(dsMyDB, "Forb");
			dtForb = dsMyDB.Tables["Forb"];
			dtForb.Constraints.Add("ForbName", new DataColumn[] {dtForb.Columns["Forb"]}, false);

			SimpleLog.Log(getParserID(), "get CoreCosts");
			daCoreCosts = new MySqlDataAdapter(String.Format("SELECT * FROM {0} LIMIT 0", FormalizeSettings.tbCoreCosts), MyConn);
			daCoreCosts.Fill(dsMyDB, "CoreCosts");
			dtCoreCosts = dsMyDB.Tables["CoreCosts"];

			SimpleLog.Log(getParserID(), "stop Core");
		}

		public int TryUpdate(MySqlDataAdapter da, DataTable dt, MySqlTransaction tran)
		{
			da.SelectCommand.Transaction = tran;
			return (da.Update(dt));
		}

		private string[] GetSQLToInsertCoreAndCoreCosts()
		{
			if (dtCore.Rows.Count > 0)
			{
				List<string> commandList = new List<string>();
				string lastCommand = String.Empty;
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				DataRow drCore;

				for(int i = 0; i < dtCore.Rows.Count; i++)
				{
					drCore = dtCore.Rows[i];
					InsertCorePosition(drCore, sb);
					if (priceType != FormalizeSettings.ASSORT_FLG)
						InsertCoreCosts(drCore, sb, (ArrayList)CoreCosts[i]);

					if ((i+1) % FormalizeSettings.MaxPositionInsertToCore == 0)
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

				return commandList.ToArray();
			}
			else
				return new string[] {};
		}

		private void InsertCoreCosts(DataRow drCore, System.Text.StringBuilder sb, ArrayList coreCosts)
		{
			if ((coreCosts != null) && (coreCosts.Count > 0))
			{
				sb.AppendLine(String.Format("insert into {0} (Core_ID, PC_CostCode, Cost) values ", FormalizeSettings.tbCoreCosts));
				bool FirstInsert = true;
				foreach (CoreCost c in coreCosts)
				{
					if (c.cost > 0)
					{
						if (!FirstInsert)
							sb.Append(", ");
						FirstInsert = false;
						sb.AppendFormat("(@LastCoreID, {0}, {1}) ", c.costCode, (c.cost > 0) ? c.cost.ToString(CultureInfo.InvariantCulture.NumberFormat) : "null");
					}
				}
				sb.AppendLine(";");
			}
		}

		private void InsertCorePosition(DataRow drCore, System.Text.StringBuilder sb)
		{
			sb.AppendLine(String.Format("insert into {0} (" +
				"PriceCode, ProductId, CodeFirmCr, SynonymCode, SynonymFirmCrCode, " +
				"Period, Junk, Await, BaseCost, MinBoundCost, " +
				"VitallyImportant, RequestRatio, RegistryCost, " +
				"MaxBoundCost, OrderCost, MinOrderCount, " +
				"Code, CodeCr, Unit, Volume, Quantity, Note, Doc, Currency) values ", FormalizeSettings.tbCore));
			sb.Append("(");
			sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, ", drCore["PriceCode"], drCore["ProductId"], drCore["CodeFirmCr"], drCore["SynonymCode"], drCore["SynonymFirmCrCode"]);
			sb.AppendFormat("'{0}', ", (drCore["Period"] is DBNull) ? String.Empty : drCore["Period"].ToString());
			sb.AppendFormat("'{0}', ", (drCore["Junk"] is DBNull) ? String.Empty : drCore["Junk"].ToString());
			sb.AppendFormat("'{0}', ", (drCore["Await"] is DBNull) ? String.Empty : drCore["Await"].ToString());
			sb.AppendFormat("{0}, ", (drCore["BaseCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["BaseCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", (drCore["MinBoundCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["MinBoundCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", drCore["VitallyImportant"].ToString());
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
			sb.Append(", ");

			AddTextParameter("Currency", drCore, sb);

			sb.AppendLine(");");
            sb.AppendLine("set @LastCoreID = last_insert_id();");
		}

		public void AddTextParameter(string ParamName, DataRow dr, System.Text.StringBuilder sb)
		{
			if (dr[ParamName] is DBNull)
				sb.Append("''");
			else
			{
				string s = dr[ParamName].ToString();
				s = s.Replace("\\", "\\\\");
				s = s.Replace("\'", "\\\'");
				s = s.Replace("\"", "\\\"");
				s = s.Replace("`", "\\`");
				sb.AppendFormat("'{0}'", s);
			}
		}

		/// <summary>
		/// Окончание разбора прайса, с последующим логированием статистики
		/// </summary>
		public void FinalizePrice()
		{
			if (FormalizeSettings.CheckZero && (zeroCount > (formCount + unformCount + zeroCount) * 0.95) )
			{
				throw new RollbackFormalizeException(FormalizeSettings.ZeroRollbackError, clientCode, priceCode, clientShortName, priceName, this.formCount, this.zeroCount, this.unformCount, this.forbCount);
			}
			else
			{
				if (formCount * 1.6 < posNum)
				{
					throw new RollbackFormalizeException(FormalizeSettings.PrevFormRollbackError, clientCode, priceCode, clientShortName, priceName, this.formCount, this.zeroCount, this.unformCount, this.forbCount);
				}
				else
				{
					string[] insertCoreAndCoreCostsCommandList = GetSQLToInsertCoreAndCoreCosts();
					//Производим транзакцию с применением главного прайса и таблицы цен
					bool res = false;
					int tryCount = 0;
					//Для логирования статистики
					System.Text.StringBuilder sbLog;
					do
					{
						SimpleLog.Log( getParserID(), "FinalizePrice started.");
						sbLog = new System.Text.StringBuilder();

						myTrans = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);

						try
						{
							MySqlCommand mcClear = new MySqlCommand();
							mcClear.Connection = MyConn;
							mcClear.Transaction = myTrans;
							mcClear.CommandTimeout = 0;

							mcClear.Parameters.Clear();

							if (priceType != FormalizeSettings.ASSORT_FLG)
							{
								//Удаляем цены из CoreCosts
								System.Text.StringBuilder sbDelCoreCosts = new System.Text.StringBuilder();
								sbDelCoreCosts.Append(String.Format("delete from {0} where pc_costcode in (", FormalizeSettings.tbCoreCosts));
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
                                    sbLog.AppendFormat("DelFromCoreCosts={0}  ", mcClear.ExecuteNonQuery());
                                }
                            }							

							//Добавляем команду на удаление данных из Core
                            mcClear.CommandText = String.Format("delete from {1} where PriceCode={0};", priceCode, FormalizeSettings.tbCore); 
							sbLog.AppendFormat("DelFromCoreAndCosts={0}  ", mcClear.ExecuteNonQuery());

							if (insertCoreAndCoreCostsCommandList.Length > 0)
							{
								//SimpleLog.Log(getParserID(), "INSERT Core and CoreCosts command: {0}", insertCoreAndCoreCostsSQL);
								int insertCoreCount = 0;
								foreach (string command in insertCoreAndCoreCostsCommandList)
								{
									mcClear.CommandText = command;
									//SimpleLog.Log(getParserID(), "INSERT Core and CoreCosts command: {0}", mcClear.CommandText);
									insertCoreCount += mcClear.ExecuteNonQuery();
									//SimpleLog.Log(getParserID(), "INSERT Core and CoreCosts Count: {0}", insertCoreCount);
								}
								sbLog.AppendFormat("InsToCoreAndCoreCosts={0}  ", insertCoreCount);

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
								sbLog.AppendFormat("UpdateAssortment={0}  ", mcClear.ExecuteNonQuery());
							}
							else
								sbLog.Append("InsToCoreAndCoreCosts=0  ");


							mcClear.CommandText = String.Format("delete from {1} where PriceCode={0}", priceCode, FormalizeSettings.tbZero);
							sbLog.AppendFormat("DelFromZero={0}  ", mcClear.ExecuteNonQuery());

							mcClear.CommandText = String.Format("delete from {1} where PriceCode={0}", priceCode, FormalizeSettings.tbForb);
							sbLog.AppendFormat("DelFromForb={0}  ", mcClear.ExecuteNonQuery());

							MySqlDataAdapter daBlockedPrice = new MySqlDataAdapter(String.Format("SELECT * FROM {1} where PriceCode={0} limit 1", priceCode, FormalizeSettings.tbBlockedPrice), MyConn);
							daBlockedPrice.SelectCommand.Transaction = myTrans;
							DataTable dtBlockedPrice = new DataTable();
							daBlockedPrice.Fill(dtBlockedPrice);

							if ((dtBlockedPrice.Rows.Count == 0) )
							{
								mcClear.CommandText = String.Format("delete from {1} where PriceCode={0}", priceCode, FormalizeSettings.tbUnrecExp);
								sbLog.AppendFormat("DelFromUnrecExp={0}  ", mcClear.ExecuteNonQuery());
							}

							sbLog.AppendFormat("UpdateForb={0}  ", TryUpdate(daForb, dtForb.Copy(), myTrans));
							sbLog.AppendFormat("UpdateZero={0}  ", TryUpdate(daZero, dtZero.Copy(), myTrans));
							sbLog.AppendFormat("UpdateUnrecExp={0}  ", TryUpdate(daUnrecExp, dtUnrecExp.Copy(), myTrans));

							//Производим обновление DateLastForm в информации о формализации
							mcClear.CommandText = String.Format(
								"UPDATE usersettings.price_update_info SET RowCount={0}, DateLastForm=now(), UnformCount={1} WHERE PriceCode={2};", formCount, unformCount, priceCode);
							mcClear.Parameters.Clear();
							mcClear.ExecuteNonQuery();

							SimpleLog.Log(getParserID(), "Statistica: {0}", sbLog.ToString());
							SimpleLog.Log(getParserID(), "FinalizePrice started: {0}", "Commit");
							myTrans.Commit();
							res = true;					
						}
						catch(MySqlException MyError)
						{
							if ((tryCount <= FormalizeSettings.MaxRepeatTranCount) && ((1213 == MyError.Number) || (1205 == MyError.Number) || (1422 == MyError.Number)))
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
								System.Threading.Thread.Sleep(10000 + tryCount*1000);
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
                SimpleLog.Log(getParserID(), "Open prepare connection");
                MyConn.Open();

				try
				{
                    SimpleLog.Log(getParserID(), "Start prepare transaction");
                    myTrans = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);

					DateTime tmPrepare = DateTime.UtcNow;
					try
					{
						try
						{
                            SimpleLog.Log(getParserID(), "Start prepare method");
                            Prepare();
                            SimpleLog.Log(getParserID(), "Stop prepare method");
                        }
						finally
						{
                            SimpleLog.Log(getParserID(), "Commiting prepare");
                            myTrans.Commit();
                            SimpleLog.Log(getParserID(), "Commited prepare");
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
						string PosName, Currency = String.Empty;
						bool Junk = false;
						int costCount;
						Int64 ProductId = -1, SynonymCode = -1, CodeFirmCr = -1, SynonymFirmCrCode = -1;
						string strCode, strName1, strOriginalName, strFirmCr;
						do
						{
							st = UnrecExpStatus.NOT_FORM;
							PosName = GetFieldValue(PriceFields.Name1, true);

							if ((null != PosName) && (String.Empty != PosName.Trim()) && (!IsForbidden(PosName)))
							{
								if (priceType != FormalizeSettings.ASSORT_FLG)
								{
									costCount = ProcessCosts();
									object currentQuantity = GetFieldValueObject(PriceFields.Quantity);
									//Производим проверку для мультиколоночных прайсов
									if (!hasParentPrice && (costType == (int)CostTypes.MultiColumn))
									{
										//Если кол-во ненулевых цен = 0, то тогда производим вставку в Zero
										//или если количество определенно и оно равно 0
										if ((0 == costCount) || ((currentQuantity is int) && ((int)currentQuantity == 0)))
										{
											InsertToZero();
											continue;
										}
										else
											currBaseCost = (currentCoreCosts[priceCodeCostIndex] as CoreCost).cost;
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
										currBaseCost = (currentCoreCosts[0] as CoreCost).cost;
									}
								}
								else
									currBaseCost = -1m;

								strCode = GetFieldValue(PriceFields.Code);
								strName1 = GetFieldValue(PriceFields.Name1, true);
								strOriginalName = GetFieldValue(PriceFields.OriginalName, true);
							
								if (GetProductId( strCode, strName1, strOriginalName, out ProductId, out  SynonymCode, out Junk))
									st = UnrecExpStatus.NAME_FORM;

								strFirmCr = GetFieldValue(PriceFields.FirmCr, true);
								if (GetCodeFirmCr(strFirmCr, out CodeFirmCr, out SynonymFirmCrCode))
									st = st | UnrecExpStatus.FIRM_FORM;

								Currency = priceCurrency;
								st = st | UnrecExpStatus.CURR_FORM;									

								if (((st & UnrecExpStatus.NAME_FORM) == UnrecExpStatus.NAME_FORM) && ((st & UnrecExpStatus.CURR_FORM) == UnrecExpStatus.CURR_FORM))
									InsertToCore(ProductId, CodeFirmCr, SynonymCode, SynonymFirmCrCode, currBaseCost, Junk, Currency);
								else
									unformCount++;

								if ((st & UnrecExpStatus.FULL_FORM) != UnrecExpStatus.FULL_FORM)
									InsertToUnrec(ProductId, CodeFirmCr, (int)st, Junk, Currency);

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
						FinalizePrice();
					}
					finally
					{
						SimpleLog.Log( getParserID(), "FinalizePrice time: {0}", DateTime.UtcNow.Subtract(tmFinalize) );
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

				case (int)PriceFields.VitallyImportant:
					return GetVitallyImportantValue();

				case (int)PriceFields.RequestRatio:
					return GetRequestRatioValue();

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
				case (int)PriceFields.Unit:
				case (int)PriceFields.Volume:
				case (int)PriceFields.OriginalName:
					return GetFieldValue(PF);

				case (int)PriceFields.BaseCost:
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
					d = Math.Round(d, 6);
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
		/// Обработать значение IntValue и получить результать как целое число
		/// </summary>
		/// <param name="IntValue"></param>
		/// <returns></returns>
		public virtual object ProcessInt(string IntValue)
		{
			if (!String.IsNullOrEmpty(IntValue))
				try
				{
					object cost = ProcessCost(IntValue);
					if (cost is decimal)
						return Convert.ToInt32( decimal.Truncate((decimal)cost) );
					else
						return cost;
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
		/// <param name="AProductId"></param>
		/// <param name="AShortCode"></param>
		/// <param name="ASynonymCode"></param>
		/// <param name="AJunk"></param>
		/// <returns></returns>
		public bool GetProductId(string ACode, string AName, string AOriginalName, out Int64 AProductId, out Int64 ASynonymCode, out bool AJunk)
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
			else
			{
				AProductId = 0;
				ASynonymCode = 0;
				AJunk = false;
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
				try
				{
					Regex re = new Regex(junkPos);
					Match m = re.Match( GetFieldValue(PriceFields.Junk) );
					JunkValue = m.Success;
				}
				catch
				{
				}
			}

			return JunkValue;
		}

		public string GetJunkValueAsString(bool Junk)
		{
			return (Junk) ? FormalizeSettings.JUNK : String.Empty;
		}

		public bool GetAwaitValue()
		{
			bool AwaitValue = false;

			try
			{
				Regex re = new Regex(awaitPos);
				Match m = re.Match( GetFieldValue(PriceFields.Await) );
				AwaitValue = (m.Success);
			}
			catch
			{
			}

			return AwaitValue;
		}

		public string GetAwaitValueAsString(bool Await)
		{
			return (Await) ? FormalizeSettings.AWAIT : String.Empty;
		}

		public byte GetVitallyImportantValue()
		{ 
			byte VitallyImportantValue = 0;

			try
			{
				Regex re = new Regex(vitallyImportantMask);
				Match m = re.Match(GetFieldValue(PriceFields.VitallyImportant));
				VitallyImportantValue = (m.Success) ? (byte)1 : (byte)0;
			}
			catch
			{ 
			}

			return VitallyImportantValue;
		}

		public object GetRequestRatioValue()
		{
			try
			{
				string rr = GetFieldRawValue(PriceFields.RequestRatio);
				if (rr != null)
				{
					int rrValue;
					if (int.TryParse(rr, out rrValue))
						return rrValue;
				}
			}
			catch
			{
			}
			return DBNull.Value;
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
				MailMessage mailMessage = new MailMessage(FormalizeSettings.FromEmail, FormalizeSettings.RepEmail,
					String.Format("{0} {1}", SubjectPrefix, PriceCode),
					null);
				mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
				mailMessage.Body = String.Format(
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
				SmtpClient Client = new SmtpClient(FormalizeSettings.SMTPHost);
				Client.Send(mailMessage);
			}
			catch(Exception e)
			{
				SimpleLog.Log(getParserID(), "Error on SendMessageToOperator : {0}", e);
			}
		}
	}
}
