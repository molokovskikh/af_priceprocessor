using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Data.OleDb;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Net.Mail;
using Inforoom.PriceProcessor.Properties;
using System.ComponentModel;
using System.Diagnostics;
using log4net;

namespace Inforoom.Formalizer
{

	//����������, ������� ������������ ��������� �� ����� ������
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

	//����������, ������� ������� ������������� �����, �� �� �������� ������������
	public class WarningFormalizeException : FormalizeException
	{

		public WarningFormalizeException(string message) : base(message)
		{}

		public WarningFormalizeException(string message, System.Int64 ClientCode, System.Int64 PriceCode, string ClientName, string PriceName) : base(message, ClientCode, PriceCode, ClientName, PriceName)
		{}
	}

	//����������, ������� ��������� ��-�� ������� ������
	public class RollbackFormalizeException : WarningFormalizeException
	{
		public readonly int FormCount = -1;
		//���-�� "�����"
		public readonly int ZeroCount = -1;
		//���-�� �������������� �������
		public readonly int UnformCount = -1;
		//���-�� "�����������" �������
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

	//��� ��������� ���� ������
	public enum PriceFields : int 
	{
		[Description("���")]
		Code,
		[Description("��� �������������")]
		CodeCr,
		[Description("������������ 1")]
		Name1,
		[Description("������������ 2")]
		Name2,
		[Description("������������ 3")]
		Name3,
		[Description("�������������")]
		FirmCr,
		[Description("������� ���������")]
		Unit,
		[Description("������� ��������")]
		Volume,
		[Description("����������")]
		Quantity,
		[Description("����������")]
		Note,
		[Description("���� ��������")]
		Period,
		[Description("��������")]
		Doc,
		[Description("���� �����������")]
		MinBoundCost,
		[Description("����")]
		Junk,
		[Description("���������")]
		Await,
		[Description("������������ ������������")]
		OriginalName,
		[Description("�������� ������")]
		VitallyImportant,
		[Description("���������")]
		RequestRatio,
		[Description("���������� ����")]
		RegistryCost,
		[Description("���� ������������")]
		MaxBoundCost,
		[Description("����������� �����")]
		OrderCost,
		[Description("����������� ����������")]
		MinOrderCount
	}

	public enum CostTypes : int
	{ 
		MultiColumn = 0,
		MiltiFile = 1
	}

	//�������������� �������� ��� ������������
	public enum FormalizeStats : int
	{ 
		//������� �� ��������� �����
		FirstSearch,
		//������� �� ��������� �����
		SecondSearch,
		//���-�� ����������� �������
		UpdateCount,
		//���-�� ����������� �������
		InsertCount,
		//���-�� ��������� �������
		DeleteCount,
		//���-�� ����������� ���
		UpdateCostCount,
		//���-�� ����������� ���
		InsertCostCount,
		//���-�� ��������� ���, �� ��������� ����, ������� ���� ������� �� �������� ������� �� Core
		DeleteCostCount,
		//����� ���-�� SQL-������ ��� ���������� �����-�����
		CommandCount,
		//������� ����� ������ � ������������� ������ � ������������ ������
		AvgSearchTime
	}

	[Flags]
	public enum UnrecExpStatus : int 
	{
		NOT_FORM	= 0,		// �����������������
		NAME_FORM	= 1,		// ��������������� �� ��������
		FIRM_FORM	= 2,		// ��������������� �� �������������
		CURR_FORM	= 4,		// ��������������� �� ������
		FULL_FORM	= 7,		// ��������� ������������
		MARK_FORB	= 8,		// ��������� ��� �����������
		MARK_DEL	= 16		// ��������� ��� ���������
	}

	//����� �������� �������� ����� �� ������� FormRules
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
		//������� � �������
		protected DataTable dtPrice;

		//��������� � ����� ������
		protected MySqlConnection MyConn;
		//���������� � ���� ������
		protected MySqlTransaction myTrans;

		//������� �� ������� ����������� ��������
		protected MySqlDataAdapter daForbidden;
		protected DataTable dtForbidden;
		//������� �� ������� ��������� �������
		protected MySqlDataAdapter daSynonym;
		protected DataTable dtSynonym;
		//������� �� �������� ��������� ��������������
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

		//���-�� ������� ���������������
		public int formCount = 0;
		//���-�� "�����"
		public int zeroCount = 0;
		//���-�� �������������� �������
		public int unformCount = 0;
		//���-�� ������������� �� ���� �����
		public int unrecCount = 0;
		//���-�� "�����������" �������
		public int forbCount = 0;

		//������������ ���-�� ��������� ���������� ��� ���������� �����-����� � ���� ������
		public int maxLockCount = 0;

		protected string priceFileName;

		//FormalizeSettings
		//��� ������
		public string	priceName;
		//��� �������
		public string	firmShortName;
		//��� �������
		public long	firmCode;
		//���� ������
		public long		priceCode = -1;
		//��� ������� �������, ����� ���� �� ����������
		public long?	costCode;

		//������ �����-������, �� ������� � �������� ������ ����� �������������� update
		private List<long> priceCodesUseUpdate;

		//������ ��������� �����, ������� ����� ����������� � ������������� ������� � �������
		private List<string> primaryFields;

		//������ ����� �� Core, �� ������� ���������� ��������� ���������
		private List<string> compareFields;

		private Dictionary<FormalizeStats, int> statCounters;


		//�������� �� �������������� �����-���� �����������?
		public bool downloaded = false;

		//���� ��� priceitems
		public long priceItemId;
		//������ ���� � ����� �� ����� ��� � ������ � ������ ��� (currentCoreCosts)
		public int				priceCodeCostIndex = -1;
		//������������ ������� : �����-��������, ����� ��� ������ ��������� ����������
		protected long		parentSynonym;
		//���-�� ����������� ������� � ������� ���
		protected long		prevRowCount;
		//����������� ������������ �� ����
		protected bool				formByCode;

		//�����, ������� ������������� �� ��� �������
		protected string nameMask;
		//����������� �����, ������� ����� ���� � �����
		protected string forbWords;
		protected string[] forbWordsList = null;
		//��� � ������ ���������� ������� ��������� �������
		protected string awaitPos;
		//��� � ������ ���������� ������� "������" �������
		protected string junkPos;
		//��� � ������ ���������� ������� ��������-������ �������
		protected string vitallyImportantMask;
		//��� ������ : ��������������
		protected int    priceType;
		//��� ������� ������� ������-��������: 0 - ����������������, 1 - �������������
		protected CostTypes costType;

		//���� �� �������������� ���������� ������ � ANSI
		protected bool convertedToANSI = false;


		protected ToughDate toughDate = null;
		protected ToughMask toughMask = null;

		protected ArrayList currentCoreCosts = null;
		protected ArrayList CoreCosts = null;

		protected readonly ILog _logger;


		/// <summary>
		/// ����������� �������
		/// </summary>
		/// <param name="PriceFileName"></param>
		/// <param name="conn"></param>
		/// <param name="mydr"></param>
		public BasePriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr)
		{
			_logger = LogManager.GetLogger(this.GetType());
			_logger.DebugFormat("������� ����� ��� ��������� ����� {0}", PriceFileName);
			//TODO: ��� ����������� �������� ������� � ������������, ����� �� �������� ������� �����-����

			//TODO: ���������� �����������, ����� �� �� ������� �� ���� ������, �.�. ���������� ��� ���, ��� ����� ��� ������ �����, ����� ������ ��� ���������������

			priceCodesUseUpdate = new List<long>();
			foreach (string syncPriceCode in Settings.Default.SyncPriceCodes)
				priceCodesUseUpdate.Add(Convert.ToInt64(syncPriceCode));
#if TESTINGUPDATE
			//������-15 (�����_1) - ������� 
			//priceCodesUseUpdate.Add(5);
			//������ (�������) -  �������
			//priceCodesUseUpdate.Add(3779);
			//������-15 (�������_�����) - ������� 
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

			//���������� ������� ��������� ������ � "������������ �����������"
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
			_logger.DebugFormat("��������� ���� {0}.{1}", priceCode, costCode);

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

			//���� ����� �������� �� �������������� �������-��������� � ����������������� ������, �� ��� ���� ��������� �� ������� ����
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
		/// ���������� ������������������ �������� ������ � ����������� �� ����
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
					if (tryCount < Settings.Default.MinRepeatTranCount)
					{
						tryCount++;
						_logger.Error("Repeat Fill dtPrice on InvalidComObjectException");
						System.Threading.Thread.Sleep(500);
					}
					else
						throw;
				}
				catch(NullReferenceException)
				{
					if (tryCount < Settings.Default.MinRepeatTranCount)
					{
						tryCount++;
						_logger.Error("Repeat Fill dtPrice on NullReferenceException");
						System.Threading.Thread.Sleep(500);
					}
					else
						throw;
				}
			}while(!res);
		}

		/// <summary>
		/// ������������ ������� ������ � ������� Core
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
			//���� ���������� ������������� � ����, �� ��������� � ������� ����
			if (dt is DateTime)
				st = ((DateTime)dt).ToString("dd'.'MM'.'yyyy");
			else
			{
				//���� �� ���������� �������������, �� ������� �� "�����" �������� ����, ���� ��� �� �����, �� ����� � ����
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
		/// ������� ������ � Zero
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
		/// ������� � �������������� �������
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
		/// ������� � ������� ����������� �����������
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
		/// ���������� � ������� ������, ������ ������
		/// </summary>
		public void Prepare()
		{
			_logger.Debug("������ Prepare");
			daForbidden = new MySqlDataAdapter(
				String.Format("SELECT PriceCode, LOWER(Forbidden) AS Forbidden FROM farm.Forbidden WHERE PriceCode={0}", priceCode), MyConn);
			daForbidden.Fill(dsMyDB, "Forbidden");
			dtForbidden = dsMyDB.Tables["Forbidden"];
			_logger.Debug("��������� Forbidden");

			daSynonym = new MySqlDataAdapter(
				String.Format("SELECT SynonymCode, LOWER(Synonym) AS Synonym, ProductId, Junk FROM farm.Synonym WHERE PriceCode={0}", parentSynonym), MyConn);
			daSynonym.Fill(dsMyDB, "Synonym");
			dtSynonym = dsMyDB.Tables["Synonym"];
			_logger.Debug("��������� Synonym");

			daSynonymFirmCr = new MySqlDataAdapter(
				String.Format("SELECT SynonymFirmCrCode, CodeFirmCr, LOWER(Synonym) AS Synonym FROM farm.SynonymFirmCr WHERE PriceCode={0}", parentSynonym), MyConn);
			daSynonymFirmCr.Fill(dsMyDB, "SynonymFirmCr");
			dtSynonymFirmCr = dsMyDB.Tables["SynonymFirmCr"];
			_logger.Debug("��������� SynonymFirmCr");

			daCore = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Core0 WHERE PriceCode={0} LIMIT 0", priceCode), MyConn);
			daCore.Fill(dsMyDB, "Core");
			dtCore = dsMyDB.Tables["Core"];
			_logger.Debug("��������� Core");

			daUnrecExp = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.UnrecExp WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
			cbUnrecExp = new MySqlCommandBuilder(daUnrecExp);
			daUnrecExp.InsertCommand = cbUnrecExp.GetInsertCommand();
			daUnrecExp.InsertCommand.CommandTimeout = 0;
			daUnrecExp.Fill(dsMyDB, "UnrecExp");
			dtUnrecExp = dsMyDB.Tables["UnrecExp"];
			dtUnrecExp.Columns["AddDate"].DataType = typeof(DateTime);
			_logger.Debug("��������� UnrecExp");

			daZero = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Zero WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
			cbZero = new MySqlCommandBuilder(daZero);
			daZero.InsertCommand = cbZero.GetInsertCommand();
			daZero.InsertCommand.CommandTimeout = 0;
			daZero.Fill(dsMyDB, "Zero");
			dtZero = dsMyDB.Tables["Zero"];
			_logger.Debug("��������� Zero");

			daForb = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Forb WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
			cbForb = new MySqlCommandBuilder(daForb);
			daForb.InsertCommand = cbForb.GetInsertCommand();
			daForb.InsertCommand.CommandTimeout = 0;
			daForb.Fill(dsMyDB, "Forb");
			dtForb = dsMyDB.Tables["Forb"];
			dtForb.Constraints.Add("ForbName", new DataColumn[] {dtForb.Columns["Forb"]}, false);
			_logger.Debug("��������� Forb");

			daCoreCosts = new MySqlDataAdapter("SELECT * FROM farm.CoreCosts LIMIT 0", MyConn);
			daCoreCosts.Fill(dsMyDB, "CoreCosts");
			dtCoreCosts = dsMyDB.Tables["CoreCosts"];
			_logger.Debug("��������� CoreCosts");

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
				_logger.Debug("��������� ExistsCore");

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
				_logger.Debug("��������� ExistsCoreCosts");

				Stopwatch ModifyCoreCostsWatch = Stopwatch.StartNew();
				relationExistsCoreToCosts = new DataRelation("ExistsCoreToCosts", dtExistsCore.Columns["Id"], dtExistsCoreCosts.Columns["Core_Id"]);
				dsMyDB.Relations.Add(relationExistsCoreToCosts);
				ModifyCoreCostsWatch.Stop();

				LoadExistsWatch.Stop();

				_logger.InfoFormat("�������� � ���������� ������������� ������ : {0}", LoadExistsWatch.Elapsed);
				_logger.InfoFormat("�������� CoreCosts : {0}", ModifyCoreCostsWatch.Elapsed);
			}

#if TESTINGUPDATE
#endif
			_logger.Debug("����� Prepare");
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
						InsertCoreCosts(drCore, sb, (ArrayList)CoreCosts[i]);

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

			//���� ����-���� �������������, �� ������ �������������, ����� ��� ������� ������ ��������
			if (dtCore.Rows.Count > 0)
			{
				List<string> synonymCodes = new List<string>();
				List<string> synonymFirmCrCodes = new List<string>();

				string lastCommand = String.Empty;
				System.Text.StringBuilder sb = new System.Text.StringBuilder();

				//��������������� ������
				DataRow drCore;

				//��������� ������ �� ������������� ������
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
							InsertCoreCosts(drCore, sb, (ArrayList)CoreCosts[i]);
						}
						else
							UpdateCoreCosts(drExistsCore, drCore, sb, (ArrayList)CoreCosts[i]);
					}

					//���� �� ����� ������ � ������������ Core, �� ������� �� �� ���� ������������ �����������, 
					//����� ��� ��������� ������ ��� �� �����������
					if (drExistsCore != null)
						drExistsCore.Delete();

					//���������� ������� �� ���-�� �������������� ������
					if (statCounters[FormalizeStats.CommandCount] >= 300)
					{
						_logger.DebugFormat("�������: {0}", statCounters[FormalizeStats.CommandCount]);
						AllCommandCount += statCounters[FormalizeStats.CommandCount];
						lastCommand = sb.ToString();
#if SQLDUMP
						_logger.DebugFormat("SQL-�������: {0}", lastCommand);
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
					_logger.DebugFormat("�������: {0}", statCounters[FormalizeStats.CommandCount]);
#if SQLDUMP
					_logger.DebugFormat("SQL-�������: {0}", lastCommand);
#endif
					commandList.Add(lastCommand);
					AllCommandCount += statCounters[FormalizeStats.CommandCount];
					statCounters[FormalizeStats.CommandCount] = AllCommandCount;
				}

				//���������� ����� ������� � ���� ������������ �����������,
				//������� �� ���� �������� ��� ���������, ��� ������� � ���, 
				//��� ��� ������ �� ��������������� ��� �������������, ������������� �� ����� �������
				List<string> deleteCore = new List<string>();
				foreach (DataRow deleted in dtExistsCore.Rows)
					if ((deleted.RowState != DataRowState.Deleted))
					{
						statCounters[FormalizeStats.DeleteCount]++;
						deleteCore.Add(deleted["Id"].ToString());
					}
				if (deleteCore.Count > 0)
				{
					//���� ���� ������, ������� ����� ������� �� Core, �� ������� ���������
					List<string> costsList = new List<string>();
					foreach (CoreCost c in currentCoreCosts)
						costsList.Add(c.costCode.ToString());

					string costCodeFilter = String.Join(", ", costsList.ToArray());

					//��������� ������� �� �������� �� CoreCosts �� ���������� CoreId
					//��� ������� �� Core, ������� �� ����� � ��������������� �����-�����
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

					//��������� ������� �� �������� �� Core �� ���������� CoreId
					//��� �������, ������� �� ����� � ��������������� �����-�����
					deleteCommandList.Add(String.Format(@"
delete
from
  farm.Core0
where
  Core0.Id in ({0});",
						String.Join(", ", deleteCore.ToArray())));

					//��������� ������ ������� � ������ ������ ������, 
					//����� �������� �� Core � CoreCosts ����������� ������
					commandList.InsertRange(0, deleteCommandList);
				}

				List<string> statCounterValues = new List<string>();
				foreach (FormalizeStats statCounter in Enum.GetValues(typeof(FormalizeStats)))
					statCounterValues.Add(String.Format("{0} = {1}", statCounter, statCounters[statCounter]));
				_logger.InfoFormat("���������� ���������� �����-�����: {0}", String.Join("; ", statCounterValues.ToArray()));

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
				//���� �������� ��������������� ���� ������ ����, �� ����� �� ��������� ��� ���������, ����� ������������ ������ ���� �������
				if (c.cost > 0)
				{
					//������� ������ ���� � ������
					drCurrent = null;
					foreach (DataRow find in drExistsCosts)
						if ((find.RowState != DataRowState.Deleted) && (long)find["PC_CostCode"] == c.costCode)
						{
							drCurrent = find;
							break;
						}

					//���� ���� �� �������, �� ���������� �������
					if (drCurrent == null)
					{
						statCounters[FormalizeStats.InsertCostCount]++;
						statCounters[FormalizeStats.CommandCount]++;
						sb.AppendFormat("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost) values ({0}, {1}, {2});\r\n",
							drExistsCore["Id"], c.costCode, c.cost.ToString(CultureInfo.InvariantCulture.NumberFormat));
					}
					else
					{
						//���� ���� ������� � �������� ���� ������, �� ��������� ���� � �������
						if (c.cost.CompareTo(Convert.ToDecimal(drCurrent["Cost"])) != 0)
						{
							statCounters[FormalizeStats.UpdateCostCount]++;
							statCounters[FormalizeStats.CommandCount]++;
							sb.AppendFormat("update farm.CoreCosts set Cost = {0} where Core_Id = {1} and PC_CostCode = {2};\r\n",
								c.cost.ToString(CultureInfo.InvariantCulture.NumberFormat), drExistsCore["Id"], c.costCode);
						}
						//������� ���� �� ���� �������, ����� ��� ��������� ������ �� �� �������������
						drCurrent.Delete();
					}
				}

			List<string> deleteCosts = new List<string>();
			foreach (DataRow deleted in drExistsCosts)
			{
				//����������� �� ���� ����������� ����� � ������� �� ������������ � ��������������� �����-�����,
				//������������� ������ ���� ����� ������� �� CoreCosts
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
			else
				if (drsExists.Length == 1)
				{
					statCounters[FormalizeStats.FirstSearch]++;
					return drsExists[0];
				}
				else
				{
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

		}

		private void InsertCoreCosts(DataRow drCore, System.Text.StringBuilder sb, ArrayList coreCosts)
		{
			if ((coreCosts != null) && (coreCosts.Count > 0))
			{
				sb.AppendLine("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost) values ");
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
		/// ��������� ������� ������, � ����������� ������������ ����������
		/// </summary>
		public void FinalizePrice()
		{
			if (Settings.Default.CheckZero && (zeroCount > (formCount + unformCount + zeroCount) * 0.95) )
			{
				throw new RollbackFormalizeException(Settings.Default.ZeroRollbackError, firmCode, priceCode, firmShortName, priceName, this.formCount, this.zeroCount, this.unformCount, this.forbCount);
			}
			else
			{
				if (formCount * 1.6 < prevRowCount)
				{
					throw new RollbackFormalizeException(Settings.Default.PrevFormRollbackError, firmCode, priceCode, firmShortName, priceName, this.formCount, this.zeroCount, this.unformCount, this.forbCount);
				}
				else
				{
					string SynonymUpdateCommand = null, SynonymFirmCrUpdateCommand = null;

					string[] insertCoreAndCoreCostsCommandList;

					if (priceCodesUseUpdate.Contains(priceCode))
					{
						Stopwatch GetSQLWatch = Stopwatch.StartNew();
						insertCoreAndCoreCostsCommandList = GetSQLToUpdateCoreAndCoreCosts(out SynonymUpdateCommand, out SynonymFirmCrUpdateCommand);
						GetSQLWatch.Stop();
						_logger.InfoFormat("����� ����� ���������� update SQL-������ : {0}", GetSQLWatch.Elapsed);
					}
					else
						insertCoreAndCoreCostsCommandList = GetSQLToInsertCoreAndCoreCosts(out SynonymUpdateCommand, out SynonymFirmCrUpdateCommand);

					//���������� ���������� � ����������� �������� ������ � ������� ���
					bool res = false;
					int tryCount = 0;
					//��� ����������� ����������
					System.Text.StringBuilder sbLog;
					do
					{
						_logger.Info("FinalizePrice started.");
						sbLog = new System.Text.StringBuilder();

						myTrans = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);

						try
						{
							var mcClear = new MySqlCommand
							              	{
							              		Connection = MyConn,
							              		Transaction = myTrans,
							              		CommandTimeout = 0
							              	};

							//���������� ������ ��������, ���� �� ���� ������ update � ������� �����-�����, ��� ���� �� ������������� �����-���� � ���� ��� ��������
							if (!priceCodesUseUpdate.Contains(priceCode) || (dtCore.Rows.Count == 0))
							{
								if ((costType == CostTypes.MiltiFile) && (priceType != Settings.Default.ASSORT_FLG))
								{
									mcClear.CommandText = String.Format("delete from farm.CoreCosts where pc_costcode = {0}", costCode);
									sbLog.AppendFormat("DelFromCoreCosts={0}  ", StatCommand(mcClear));
									mcClear.CommandText = String.Format("delete from farm.Core0 where PriceCode={0};", priceCode);
									sbLog.AppendFormat("DelFromCore={0}  ", StatCommand(mcClear));
								}
								else
								{
									if (priceType != Settings.Default.ASSORT_FLG)
									{
										//������� ���� �� CoreCosts
										var sbDelCoreCosts = new System.Text.StringBuilder();
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
											//���������� �������� ���
											mcClear.CommandText = sbDelCoreCosts.ToString();
											sbLog.AppendFormat("DelFromCoreCosts={0}  ", StatCommand(mcClear));
										}
									}

									//��������� ������� �� �������� ������ �� Core
									mcClear.CommandText = String.Format("delete from farm.Core0 where PriceCode={0};", priceCode);
									sbLog.AppendFormat("DelFromCore={0}  ", StatCommand(mcClear));
								}
								
							}

							//��������� ������� � ����������� ������ � Core � CoreCosts
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
							daBlockedPrice.SelectCommand.Transaction = myTrans;
							DataTable dtBlockedPrice = new DataTable();
							daBlockedPrice.Fill(dtBlockedPrice);

							if ((dtBlockedPrice.Rows.Count == 0) )
							{
								mcClear.CommandText = String.Format("delete from farm.UnrecExp where PriceItemId={0}", priceItemId);
								sbLog.AppendFormat("DelFromUnrecExp={0}  ", StatCommand(mcClear));
							}

							sbLog.AppendFormat("UpdateForb={0}  ", TryUpdate(daForb, dtForb.Copy(), myTrans));
							sbLog.AppendFormat("UpdateZero={0}  ", TryUpdate(daZero, dtZero.Copy(), myTrans));
							sbLog.AppendFormat("UpdateUnrecExp={0}  ", TryUpdate(daUnrecExp, dtUnrecExp.Copy(), myTrans));

							//���������� ���������� PriceDate � LastFormalization � ���������� � ������������
							//���� �����-���� ��������, �� ��������� ���� PriceDate, ���� ���, �� ��������� ������ � intersection_update_info
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
								if (costCode.HasValue)
									mcClear.CommandText += String.Format(
										"update usersettings.intersection_update_info, usersettings.intersection set lastsent = default where intersection_update_info.id = intersection.id and intersection.PriceCode = {0} and intersection.CostCode = {1}", priceCode, costCode);
								else
									mcClear.CommandText += String.Format(
										"update usersettings.intersection_update_info set lastsent = default where intersection_update_info.PriceCode = {0}", priceCode);
							}
							sbLog.AppendFormat("UpdatePriceItemsAndIntersections={0}  ", StatCommand(mcClear));

							_logger.InfoFormat("Statistica: {0}", sbLog.ToString());
							_logger.InfoFormat("FinalizePrice started: {0}", "Commit");
							myTrans.Commit();
							res = true;					
						}
						catch(MySqlException MyError)
						{
							if ((tryCount <= Settings.Default.MaxRepeatTranCount) && ((1213 == MyError.Number) || (1205 == MyError.Number) || (1422 == MyError.Number)))
							{
								tryCount++;
								_logger.InfoFormat("Try transaction: tryCount = {0}", tryCount);
								try
								{ 
									myTrans.Rollback();
								}
								catch(Exception ex)
								{
									_logger.Error("Error on rollback", ex);
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
		/// �������������� ������
		/// </summary>
		public void Formalize()
		{
			log4net.NDC.Push(String.Format("{0}.{1}", priceCode, costCode));
			_logger.Debug("������ Formalize");
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
					_logger.Debug("������� ������� ���������� � �����");
					MyConn.Open();
					_logger.Debug("���������� � ����� �����������");

					try
					{

						if (dtPrice.Rows.Count > 0)
						{
							_logger.Debug("������� ������� ����������");
							myTrans = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);
							_logger.Debug("���������� �������");

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
											//���������� �������� ��� ���������������� �������
											if (costType == CostTypes.MultiColumn)
											{
												//���� ���-�� ��������� ��� = 0, �� ����� ���������� ������� � Zero
												//��� ���� ���������� ����������� � ��� ����� 0
												if ((0 == costCount) || ((currentQuantity is int) && ((int)currentQuantity == 0)))
												{
													InsertToZero();
													continue;
												}
											}
											else
											{
												//��� �������� ��� ���� ���������
												//���� ���-�� ��������� ��� = 0
												//��� ���� ���������� ����������� � ��� ����� 0
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
			finally
			{
				_logger.Debug("����� Formalize");
				log4net.NDC.Pop();
			}
		}

		/// <summary>
		/// ���������� �������� ����, ������� ����� ������� �� ������ ������
		/// </summary>
		/// <param name="PF"></param>
		/// <param name="Value"></param>
		public void SetFieldName(PriceFields PF, string Value)
		{
			FieldNames[(int)PF] = Value;
		}

		/// <summary>
		/// �������� �������� ����
		/// </summary>
		/// <param name="PF"></param>
		/// <returns></returns>
		public string GetFieldName(PriceFields PF)
		{
			return FieldNames[(int)PF];
		}

		/// <summary>
		/// ������� �� ��������� ������ ������ ������
		/// </summary>
		/// <returns>������ �� �������� �������?</returns>
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
		/// ������� �� ���������� �������
		/// </summary>
		/// <returns>������ �� �������� �������?</returns>
		public virtual bool Prior()
		{
			CurrPos--;
			if (CurrPos > -1)
				return true;
			else
				return false;
		}

		/// <summary>
		/// �������� ����� �������� �������� ����
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
		/// �������� �������� ���� � ������������ ����
		/// </summary>
		/// <param name="PF"></param>
		/// <returns></returns>
		public virtual string GetFieldValue(PriceFields PF)
		{
			string res = null;

			//������� �������� �������� ������ �� toughMask
			if (null != toughMask)
			{
				res = toughMask.GetFieldValue(PF);
				if (null != res)
				{
					//������� ������� ����� ������ �� ������������
					if ((PriceFields.Name1 == PF) || (PriceFields.Name2 == PF) || (PriceFields.Name2 == PF) || (PriceFields.OriginalName == PF))
						res = RemoveForbWords(res);
					if ((PriceFields.Note != PF) && (PriceFields.Doc != PF))
						res = UnSpace(res);
				}
			}

			//���� � ��� ��� �� ����������, ��� �������� �������� ������ �� ������ ����
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
		/// �������� �������� ���� � ������ ��������
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
		/// �������� �������� ���� ��� ������
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
		/// ���������� �������� ����
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
		/// ���������� �������� IntValue � �������� ���������� ��� ����� �����
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
		/// ���������� �������� ����� ��������
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
			else
				return DBNull.Value;
		}

		/// <summary>
		/// ������ ������ ������� � �����
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
		/// ������� ����������� �����
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
		/// ���������� �� �������� � ������� ����������� ����
		/// </summary>
		/// <param name="PosName"></param>
		/// <returns></returns>
		public bool IsForbidden(string PosName)
		{
			DataRow[] dr = dsMyDB.Tables["Forbidden"].Select(String.Format("Forbidden = '{0}'", PosName.Replace("'", "''")));
			return dr.Length > 0;
		}

		/// <summary>
		/// ������ �� �� ���������� ������� �� ����, ����� � ������������� ��������?
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
			else
			{
				AProductId = null;
				ASynonymCode = null;
				AJunk = false;
				return false;
			}

		}

		/// <summary>
		/// ������ �� �� ���������� ������������� �� ��������?
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
				//���� �������� CodeFirmCr �� �����������, �� ������������� � null, ����� ����� �������� ����
				ACodeFirmCr = Convert.IsDBNull(dr[0]["CodeFirmCr"]) ? null : (long?)Convert.ToInt64(dr[0]["CodeFirmCr"]);
				ASynonymFirmCrCode = Convert.ToInt64(dr[0]["SynonymFirmCrCode"]);
				return true;
			}
			else
			{
 				ACodeFirmCr = null;
 				ASynonymFirmCrCode = null;
 				//���� ���� FirmCr �� �����������, �� ������� ��� ������� ���������� �� �������������
 				return (String.IsNullOrEmpty(FirmCr) ? true : false);
			}
		}

		protected bool GetBoolValue(PriceFields priceField, string mask)
		{
			bool value = false;

			string[] trueValues = new string[] { "������", "true"};
			string[] falseValues = new string[] { "����", "false" };

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
		/// �������� �������� ���� Junk
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


		public bool GetAwaitValue()
		{
			return GetBoolValue(PriceFields.Await, awaitPos);
		}

		public bool GetVitallyImportantValue()
		{
			return GetBoolValue(PriceFields.VitallyImportant, vitallyImportantMask);
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
		/// ������������ ���� � ���������� ���-�� �� ������� ���
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
			return String.Format("{0}.{1}.{2}", this.GetType().Name, priceCode, costCode);
		}

		public bool checkZeroCost(decimal cost)
		{
			return (cost < 0 || Math.Abs(Decimal.Zero-cost) < 0.01m);
		}

	}
}
