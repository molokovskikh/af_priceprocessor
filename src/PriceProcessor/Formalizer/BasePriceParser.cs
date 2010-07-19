using System;
using System.Linq;
using Common.Tools;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Text;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Helpers;
using Inforoom.PriceProcessor.Formalizer.New;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using Inforoom.PriceProcessor.Properties;
using System.ComponentModel;
using System.Diagnostics;
using log4net;
using System.Configuration;

namespace Inforoom.Formalizer
{
	//��� ��������� ���� ������
	public enum PriceFields
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
		[Description("���� �������������")]
		ProducerCost,
		[Description("������ ���")]
		Nds,
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

	public enum CostTypes
	{ 
		MultiColumn = 0,
		MiltiFile = 1
	}

	//�������������� �������� ��� ������������
	public class FormalizeStats
	{
		//������� �� ��������� �����
		public int FirstSearch;
		//������� �� ��������� �����
		public int SecondSearch;
		//���-�� ����������� �������
		public int UpdateCount;
		//���-�� ����������� �������
		public int InsertCount;
		//���-�� ��������� �������
		public int DeleteCount;
		//���-�� ����������� ���
		public int UpdateCostCount;
		//���-�� ����������� ���
		public int InsertCostCount;
		//���-�� ��������� ���, �� ��������� ����, ������� ���� ������� �� �������� ������� �� Core
		public int DeleteCostCount;
		//����� ���-�� SQL-������ ��� ���������� �����-�����
		public int CommandCount;
		//������� ����� ������ � ������������� ������ � ������������ ������
		public int AvgSearchTime;

		public int ProducerSynonymCreatedCount;

		public int ProducerSynonymUsedExistCount;

		public bool CanCreateProducerSynonyms()
		{
			return ProducerSynonymCreatedCount == 0 || ProducerSynonymCreatedCount < ProducerSynonymUsedExistCount || (ProducerSynonymUsedExistCount / ProducerSynonymCreatedCount * 100 > 20);
		}

		//�������� ��������, ������� ������������ � ���������� ���������� SQL-������ � update'��
		public void ResetCountersForUpdate()
		{ 
			FirstSearch = 0;
			SecondSearch = 0;
			UpdateCount = 0;
			InsertCount = 0;
			DeleteCount = 0;
			UpdateCostCount = 0;
			InsertCostCount = 0;
			DeleteCostCount = 0;
			CommandCount = 0;
			AvgSearchTime = 0;
		}

		//
		public string GetStatUpdateMessage()
		{
			var statCounterValues = new List<string>();
			foreach (var field in typeof(FormalizeStats).GetFields())
				statCounterValues.Add(String.Format("{0} = {1}", field.Name, field.GetValue(this)));
			return String.Format("���������� ���������� �����-�����: {0}", String.Join("; ", statCounterValues.ToArray()));
		}
	}

	[Flags]
	public enum UnrecExpStatus : byte
	{
		NotForm	       = 0, // �����������������
		NameForm	   = 1, // ��������������� �� ��������
		FirmForm	   = 2, // ��������������� �� �������������
		AssortmentForm = 4, // ��������������� �� ������������
		FullForm	   = 7, // ��������� ������������ �� ������������, ������������� � ������������
		MarkForb	   = 8, // ��������� ��� �����������
		MarkExclude	   = 16,// ��������� ��� ����������
		ExcludeForm    = 19 // ������������ �� ������������, ������������� � ��� ����������
	}

	// ���� ��
	[Flags]
	public enum PricePurpose
	{
		Normal = 0, // �������
		Assortment = 1, // ��������������
		Helper = 2      // ����������
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
		public static string colFirmSegment = "FirmSegment";
	}

	public class CoreCost : ICloneable
	{
		public Int64 costCode = -1;
		public bool baseCost;
		public string costName = String.Empty;
		public string fieldName = String.Empty;
		public int txtBegin = -1;
		public int txtEnd = -1;
		public decimal? cost;
		//���-�� ������� � ��������������� ����� ��� ������ ������� �������
		public int undefinedCostCount;
		//���-�� ������� � ������� ����� ��� ������ ������� �������
		public int zeroCostCount;

		public CoreCost(Int64 ACostCode, string ACostName, bool ABaseCost, 
			string AFieldName, int ATxtBegin, int ATxtEnd)
		{
			costCode = ACostCode;
			baseCost = ABaseCost;
			costName = ACostName;
			fieldName = AFieldName;
			txtBegin = ATxtBegin;
			txtEnd = ATxtEnd;
		}

		public object Clone()
		{
			var ccNew = new CoreCost(costCode, costName, baseCost, fieldName, txtBegin, txtEnd);
			ccNew.cost = cost;
			return ccNew;
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

		//������� �� ������� ����������� ��������
		protected MySqlDataAdapter daForbidden;
		protected DataTable dtForbidden;
		//������� �� ������� ��������� �������
		protected MySqlDataAdapter daSynonym;
		protected DataTable dtSynonym;
		//������� �� �������� ��������� ��������������
		protected MySqlDataAdapter daSynonymFirmCr;
		protected DataTable dtSynonymFirmCr;
		protected DataTable dtNewSynonymFirmCr;
		//������� � �������������
		protected MySqlDataAdapter daAssortment;
		protected DataTable dtAssortment;
		//������� � ������������
		protected MySqlDataAdapter daExcludes;
		protected DataTable dtExcludes;
		protected MySqlCommandBuilder cbExcludes;

		Stopwatch assortmentSearchWatch;
		int assortmentSearchCount;
		Stopwatch excludesSearchWatch;
		int excludesSearchCount;

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
		public int formCount;
		//���-�� "�����"
		public int zeroCount;
		//���-�� �������������� �������
		public int unformCount;
		//���-�� ������������� �� ���� �����
		public int unrecCount;
		//���-�� "�����������" �������
		public int forbCount;

		//������������ ���-�� ��������� ���������� ��� ���������� �����-����� � ���� ������
		public int maxLockCount;

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

		//������ �����-������, �� ������� ����� �������������� update
		private List<long> priceCodesUseUpdate;

		//������ ��������� �����, ������� ����� ����������� � ������������� ������� � �������
		private List<string> primaryFields;

		//������ ����� �� Core, �� ������� ���������� ��������� ���������
		private List<string> compareFields;

		private FormalizeStats _stats = new FormalizeStats();

		//�������� �� �������������� �����-���� �����������?
		public bool downloaded;

		//���� ��� priceitems
		public long priceItemId;
		//������ ���� � ����� �� ����� ��� � ������ � ������ ��� (currentCoreCosts)
		public int priceCodeCostIndex = -1;
		//������������ ������� : �����-��������, ����� ��� ������ ��������� ����������
		protected long parentSynonym;
		//���-�� ����������� ������� � ������� ���
		protected long prevRowCount;
		//����������� ������������ �� ����
		protected bool formByCode;

		//�����, ������� ������������� �� ��� �������
		protected string nameMask;
		//����������� �����, ������� ����� ���� � �����
		protected string forbWords;
		protected string[] forbWordsList;
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
		//������������� ������: �������, ��������������, ����������
		protected PricePurpose pricePurpose;

		//���� �� �������������� ���������� ������ � ANSI
		protected bool convertedToANSI;


		protected ToughDate toughDate;
		protected ToughMask toughMask;

		protected List<CoreCost> currentCoreCosts;
		protected List<List<CoreCost>> CoreCosts;

		protected readonly ILog _logger;

		public string InputFileName { get; set; }

		/// <summary>
		/// ����������� �������
		/// </summary>
		public BasePriceParser(string priceFileName, MySqlConnection connection, DataTable data)
		{
			_logger = LogManager.GetLogger(GetType());
			_logger.DebugFormat("������� ����� ��� ��������� ����� {0}", priceFileName);
			//TODO: ��� ����������� �������� ������� � ������������, ����� �� �������� ������� �����-����

			//TODO: ���������� �����������, ����� �� �� ������� �� ���� ������, �.�. ���������� ��� ���, ��� ����� ��� ������ �����, ����� ������ ��� ���������������

			priceCodesUseUpdate = new List<long>();
			foreach (string syncPriceCode in Settings.Default.SyncPriceCodes)
				priceCodesUseUpdate.Add(Convert.ToInt64(syncPriceCode));

			primaryFields = new List<string>();
			compareFields = new List<string>();
			foreach (string field in Settings.Default.CorePrimaryFields)
				primaryFields.Add(field);

			this.priceFileName = priceFileName;
			dtPrice = new DataTable();
			MyConn = connection;
			dsMyDB = new DataSet();
			currentCoreCosts = new List<CoreCost>();
			CoreCosts = new List<List<CoreCost>>();
			FieldNames = new string[Enum.GetNames(typeof(PriceFields)).Length];
			
			priceName = data.Rows[0][FormRules.colSelfPriceName].ToString();
			firmShortName = data.Rows[0][FormRules.colFirmShortName].ToString();
			firmCode = Convert.ToInt64(data.Rows[0][FormRules.colFirmCode]); 
			formByCode = Convert.ToBoolean(data.Rows[0][FormRules.colFormByCode]);
			priceItemId = Convert.ToInt64(data.Rows[0][FormRules.colPriceItemId]); 
			priceCode = Convert.ToInt64(data.Rows[0][FormRules.colPriceCode]);
			costCode = (data.Rows[0][FormRules.colCostCode] is DBNull) ? null : (long?)Convert.ToInt64(data.Rows[0][FormRules.colCostCode]);
			parentSynonym = Convert.ToInt64(data.Rows[0][FormRules.colParentSynonym]); 
			costType = (CostTypes)Convert.ToInt32(data.Rows[0][FormRules.colCostType]);
			
			nameMask = data.Rows[0][FormRules.colNameMask] is DBNull ? String.Empty : (string)data.Rows[0][FormRules.colNameMask];

			//���������� ������� ��������� ������ � "������������ �����������"
			forbWords = data.Rows[0][FormRules.colForbWords] is DBNull ? String.Empty : (string)data.Rows[0][FormRules.colForbWords];
			forbWords = forbWords.Trim();
			if (String.Empty != forbWords)
			{
				forbWordsList = forbWords.Split( new[] {' '} );
				int len = 0;
				foreach(string s in forbWordsList)
					if(String.Empty != s)
						len++;
				if (len > 0)
				{
					var newForbWordList = new string[len];
					var i = 0;
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

			awaitPos = data.Rows[0][FormRules.colSelfAwaitPos].ToString();
			junkPos  = data.Rows[0][FormRules.colSelfJunkPos].ToString();
			vitallyImportantMask = data.Rows[0][FormRules.colSelfVitallyImportantMask].ToString();
			prevRowCount = data.Rows[0][FormRules.colPrevRowCount] is DBNull ? 0 : Convert.ToInt64(data.Rows[0][FormRules.colPrevRowCount]);
			priceType = Convert.ToInt32(data.Rows[0][FormRules.colPriceType]);
			var firmSegment = Convert.ToInt16(data.Rows[0][FormRules.colFirmSegment]);

			pricePurpose = PricePurpose.Normal;
			if (priceType == Settings.Default.ASSORT_FLG)
				pricePurpose |= PricePurpose.Assortment;
			if (firmSegment == 1)
				pricePurpose |= PricePurpose.Helper;

			toughDate = new ToughDate();
			if (String.Empty != nameMask)
				toughMask = new ToughMask(nameMask, firmCode, priceCode, firmShortName, priceName);

			var selectCostFormRulesSQL = String.Empty;
			if (costType == CostTypes.MultiColumn)
				selectCostFormRulesSQL = String.Format("select * from usersettings.PricesCosts pc, farm.CostFormRules cfr where pc.PriceCode={0} and cfr.CostCode = pc.CostCode", priceCode);
			else
				selectCostFormRulesSQL = String.Format("select * from usersettings.PricesCosts pc, farm.CostFormRules cfr where pc.PriceCode={0} and cfr.CostCode = pc.CostCode and pc.CostCode = {1}", priceCode, costCode.Value);

			var daPricesCost = new MySqlDataAdapter( selectCostFormRulesSQL, MyConn );
			var dtPricesCost = new DataTable("PricesCosts");
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
					if (!currentCoreCosts[0].baseCost)
						throw new WarningFormalizeException(Settings.Default.BaseCostNotExistsError, firmCode, priceCode, firmShortName, priceName);
				}
				else
				{
					var bc = currentCoreCosts.Where(c => c.baseCost).ToArray();
					if (bc.Length == 0)
						throw new WarningFormalizeException(Settings.Default.BaseCostNotExistsError, firmCode, priceCode, firmShortName, priceName);

					if (bc.Length > 1)
					{
						throw new WarningFormalizeException(
							String.Format(Settings.Default.DoubleBaseCostsError,
							              bc[0].costCode,
							              bc[1].costCode),
							firmCode, priceCode, firmShortName, priceName);
					}
					currentCoreCosts.Remove(bc[0]);
					currentCoreCosts.Insert(0, bc[0]);
				}

				if ((this is FixedNativeTextParser1251 || this is FixedNativeTextParser866) && (currentCoreCosts[0].txtBegin == -1 || currentCoreCosts[0].txtEnd == -1))
					throw new WarningFormalizeException(Settings.Default.FieldNameBaseCostsError, firmCode, priceCode, firmShortName, priceName);

				if (!(this is FixedNativeTextParser1251 || this is FixedNativeTextParser866) && (String.Empty == currentCoreCosts[0].fieldName))
					throw new WarningFormalizeException(Settings.Default.FieldNameBaseCostsError, firmCode, priceCode, firmShortName, priceName);

				priceCodeCostIndex = currentCoreCosts.IndexOf(c => c.costCode == priceCode);
				if (priceCodeCostIndex == -1)
					priceCodeCostIndex = 0;
			}
		}

		/// <summary>
		/// ���������� ������������������ �������� ������ � ����������� �� ����
		/// </summary>
		public abstract void Open();

		/// <summary>
		/// ������������ ������� ������ � ������� Core
		/// </summary>
		public void InsertToCore(FormalizationPosition position)
		{
			if (!position.Junk)
				position.Junk = (bool)GetFieldValueObject(PriceFields.Junk);

			DataRow drCore = dtCore.NewRow();

			drCore["PriceCode"] = priceCode;
			drCore["ProductId"] = position.ProductId;
			drCore["CatalogId"] = position.CatalogId;
			
			if (position.CodeFirmCr.HasValue)
				drCore["CodeFirmCr"] = position.CodeFirmCr;
			drCore["SynonymCode"] = position.SynonymCode;
			if (position.SynonymFirmCrCode.HasValue)
				drCore["SynonymFirmCrCode"] = position.SynonymFirmCrCode;
			if (position.InternalProducerSynonymId.HasValue)
				drCore["InternalProducerSynonymId"] = position.InternalProducerSynonymId;
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
			var producerCost = GetFieldValueObject(PriceFields.ProducerCost);
			if ((producerCost is decimal) && ((decimal)producerCost >= 0))
				drCore["ProducerCost"] = (decimal) producerCost;
			var nds = GetFieldValueObject(PriceFields.Nds);
			if ((nds is int) && (Convert.ToUInt32(nds) > 0))
				drCore["Nds"] = Convert.ToUInt32(nds);

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

			drCore["Junk"] = Convert.ToByte(position.Junk);
			drCore["Await"] = Convert.ToByte( (bool)GetFieldValueObject(PriceFields.Await) );

			drCore["MinBoundCost"] = GetFieldValueObject(PriceFields.MinBoundCost);


			dtCore.Rows.Add(drCore);
			if (priceType != Settings.Default.ASSORT_FLG)
				CoreCosts.Add(currentCoreCosts.Select(c => (CoreCost)c.Clone()).ToList());
			formCount++;
		}

		/// <summary>
		/// ������� � ������� ����������� �����������
		/// </summary>
		/// <param name="PosName"></param>
		public void InsertIntoForb(string PosName)
		{
			DataRow newRow = dtForb.NewRow();
			newRow["PriceItemId"] = priceItemId;
			newRow["Forb"] = PosName;
			try
			{
				dtForb.Rows.Add(newRow);
				forbCount++;
			}
			catch(ConstraintException)
			{}
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
			unrecCount++;
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
				parentSynonym), MyConn);
			daSynonym.Fill(dsMyDB, "Synonym");
			dtSynonym = dsMyDB.Tables["Synonym"];
			_logger.Debug("��������� Synonym");

			daAssortment = new MySqlDataAdapter("SELECT Id, CatalogId, ProducerId, Checked FROM catalogs.Assortment ", MyConn);
			var excludesBuilder  = new MySqlCommandBuilder(daAssortment);
			daAssortment.InsertCommand = excludesBuilder.GetInsertCommand();
			daAssortment.InsertCommand.CommandTimeout = 0;
			daAssortment.Fill(dsMyDB, "Assortment");
			dtAssortment = dsMyDB.Tables["Assortment"];
			_logger.Debug("��������� Assortment");
			dtAssortment.PrimaryKey = new[] { dtAssortment.Columns["CatalogId"], dtAssortment.Columns["ProducerId"] };
			_logger.Debug("��������� ������ �� Assortment");

			daExcludes = new MySqlDataAdapter(
				String.Format("SELECT Id, CatalogId, ProducerSynonymId, PriceCode, OriginalSynonymId FROM farm.Excludes where PriceCode = {0}", parentSynonym), MyConn);
			cbExcludes = new MySqlCommandBuilder(daExcludes);
			daExcludes.InsertCommand = cbExcludes.GetInsertCommand();
			daExcludes.InsertCommand.CommandTimeout = 0;
			daExcludes.Fill(dsMyDB, "Excludes");
			dtExcludes = dsMyDB.Tables["Excludes"];
			_logger.Debug("��������� Excludes");
			dtExcludes.Constraints.Add("ProducerSynonymKey", new[] { dtExcludes.Columns["CatalogId"], dtExcludes.Columns["ProducerSynonymId"] }, false);
			_logger.Debug("��������� ������ �� Excludes");

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
				parentSynonym), MyConn);
			daSynonymFirmCr.Fill(dsMyDB, "SynonymFirmCr");
			daSynonymFirmCr.InsertCommand = new MySqlCommand(@"
insert into farm.SynonymFirmCr (PriceCode, CodeFirmCr, Synonym) values (?PriceCode, null, ?OriginalSynonym);
set @LastSynonymFirmCrCode = last_insert_id();
insert farm.UsedSynonymFirmCrLogs (SynonymFirmCrCode) values (@LastSynonymFirmCrCode);
insert into farm.AutomaticProducerSynonyms (ProducerSynonymId) values (@LastSynonymFirmCrCode);
select @LastSynonymFirmCrCode;");
			daSynonymFirmCr.InsertCommand.Parameters.Add("?PriceCode", MySqlDbType.Int64);
			daSynonymFirmCr.InsertCommand.Parameters.Add("?OriginalSynonym", MySqlDbType.String);
			daSynonymFirmCr.InsertCommand.Connection = MyConn;
			dtSynonymFirmCr = dsMyDB.Tables["SynonymFirmCr"];
			dtSynonymFirmCr.Columns.Add("OriginalSynonym", typeof(string));
			dtSynonymFirmCr.Columns.Add("InternalProducerSynonymId", typeof(long));
			dtSynonymFirmCr.Columns["InternalProducerSynonymId"].AutoIncrement = true;
			_logger.Debug("��������� SynonymFirmCr");

			daCore = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Core0 WHERE PriceCode={0} LIMIT 0", priceCode), MyConn);
			daCore.Fill(dsMyDB, "Core");
			dtCore = dsMyDB.Tables["Core"];
			dtCore.Columns.Add("InternalProducerSynonymId", typeof(long));
			dtCore.Columns.Add("CatalogId", typeof(long));
			_logger.Debug("��������� Core");

			daUnrecExp = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.UnrecExp WHERE PriceItemId={0} LIMIT 0", priceItemId), MyConn);
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

		private string[] GetSQLToInsertCoreAndCoreCosts(out string updateUsedSynonymLogs)
		{
			updateUsedSynonymLogs = null;

			if (dtCore.Rows.Count == 0)
				return new string[] {};

			var commandList = new List<string>();

			var synonymCodes = new List<string>();
			var synonymFirmCrCodes = new List<string>();

			string lastCommand;
			var sb = new StringBuilder();

			for(var i = 0; i < dtCore.Rows.Count; i++)
			{
				var drCore = dtCore.Rows[i];

				DataRow drNewProducerSynonym = null;

				//��������� � ������ ������������ ���������
				if (!synonymCodes.Contains(drCore["SynonymCode"].ToString()))
					synonymCodes.Add(drCore["SynonymCode"].ToString());

				if (!Convert.IsDBNull(drCore["InternalProducerSynonymId"]))
					drNewProducerSynonym = CheckPositionByProducerSynonym(drCore);
				//���� ������� �� ����� ���������, �� ��������� � ������ ������������ ��������� ��������������
				else if (!Convert.IsDBNull(drCore["SynonymFirmCrCode"]) && !synonymFirmCrCodes.Contains(drCore["SynonymFirmCrCode"].ToString()))
					synonymFirmCrCodes.Add(drCore["SynonymFirmCrCode"].ToString());

				SqlBuilder.InsertCorePosition(drCore, sb, drNewProducerSynonym);

				if (priceType != Settings.Default.ASSORT_FLG)
					InsertCoreCosts(sb, CoreCosts[i]);

				if ((i+1) % Settings.Default.MaxPositionInsertToCore == 0)
				{
					lastCommand = sb.ToString();
					if (!String.IsNullOrEmpty(lastCommand))
						commandList.Add(lastCommand);
					sb = new StringBuilder();
				}
			}

			lastCommand = sb.ToString();
			if (!String.IsNullOrEmpty(lastCommand))
				commandList.Add(lastCommand);

			updateUsedSynonymLogs = 
				"update farm.UsedSynonymLogs set LastUsed = now() where SynonymCode in (" + String.Join(", ", synonymCodes.ToArray()) + ");";
			if (synonymFirmCrCodes.Count > 0)
				updateUsedSynonymLogs += 
					"update farm.UsedSynonymFirmCrLogs set LastUsed = now() where SynonymFirmCrCode in (" + String.Join(", ", synonymFirmCrCodes.ToArray()) + ");";

			return commandList.ToArray();
		}

		private DataRow CheckPositionByProducerSynonym(DataRow drCore)
		{
			var drNewSynonym = dtNewSynonymFirmCr.Select("InternalProducerSynonymId = " + drCore["InternalProducerSynonymId"])[0];

			//���� ��� ����� ��������� �������, �� ���������� ������ �� ����, ����� �������� Core
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
					Convert.ToInt64(drCore["SynonymCode"]), pricePurpose == PricePurpose.Normal);
				if (status == UnrecExpStatus.AssortmentForm)
					drCore["CodeFirmCr"] = drNewSynonym["CodeFirmCr"];
			}
			return null;
		}

		private string[] GetSQLToUpdateCoreAndCoreCosts(out string updateUsedSynonymLogs)
		{
			updateUsedSynonymLogs = null;

			var commandList = new List<string>();

			var synonymCodes = new List<string>();
			var synonymFirmCrCodes = new List<string>();

			//���� ����-���� �������������, �� ������ �������������, ����� ��� ������� ������ ��������
			if (dtCore.Rows.Count == 0)
				return new string[0];

			string lastCommand;
			var sb = new StringBuilder();

			//��������������� ������

			//��������� ������ �� ������������� ������
			DataRow drExistsCore;

			int AllCommandCount = 0;

			for (var i = 0; i < dtCore.Rows.Count; i++)
			{
				var drCore = dtCore.Rows[i];

				DataRow drNewProducerSynonym = null;

				//��������� � ������ ������������ ���������
				if (!synonymCodes.Contains(drCore["SynonymCode"].ToString()))
					synonymCodes.Add(drCore["SynonymCode"].ToString());

				if (!Convert.IsDBNull(drCore["InternalProducerSynonymId"]))
					drNewProducerSynonym = CheckPositionByProducerSynonym(drCore);
				else
					//���� ������� �� ����� ���������, �� ��������� � ������ ������������ ��������� ��������������
                    if (!Convert.IsDBNull(drCore["SynonymFirmCrCode"]) 
						&& !synonymFirmCrCodes.Contains(drCore["SynonymFirmCrCode"].ToString()))
						synonymFirmCrCodes.Add(drCore["SynonymFirmCrCode"].ToString());
                if (drNewProducerSynonym != null)
                    drExistsCore = null;
				else
                    drExistsCore = FindPositionInExistsCore(drCore);
				if (drExistsCore == null)
				{
					_stats.InsertCount++;
					_stats.CommandCount++;
					SqlBuilder.InsertCorePosition(drCore, sb, drNewProducerSynonym);
				}
				else
				{
					UpdateCorePosition(drExistsCore, drCore, sb);
				}

				if (priceType != Settings.Default.ASSORT_FLG)
				{
					if (drExistsCore == null)
					{
						_stats.CommandCount++;
						InsertCoreCosts(sb, CoreCosts[i]);
					}
					else
						UpdateCoreCosts(drExistsCore, sb, CoreCosts[i]);
				}

				//���� �� ����� ������ � ������������ Core, �� ������� �� �� ���� ������������ �����������, 
				//����� ��� ��������� ������ ��� �� �����������
				if (drExistsCore != null)
					drExistsCore.Delete();

				//���������� ������� �� ���-�� �������������� ������
				if (_stats.CommandCount >= 200)
				{
					_logger.DebugFormat("�������: {0}", _stats.CommandCount);
					AllCommandCount += _stats.CommandCount;
					lastCommand = sb.ToString();
#if SQLDUMP
						_logger.DebugFormat("SQL-�������: {0}", lastCommand);
#endif
					if (!String.IsNullOrEmpty(lastCommand))
						commandList.Add(lastCommand);
					sb = new StringBuilder();
					_stats.CommandCount = 0;
				}
			}

			_stats.AvgSearchTime = _stats.AvgSearchTime / dtCore.Rows.Count;

			lastCommand = sb.ToString();
			if (!String.IsNullOrEmpty(lastCommand))
			{
				_logger.DebugFormat("�������: {0}", _stats.CommandCount);
#if SQLDUMP
					_logger.DebugFormat("SQL-�������: {0}", lastCommand);
#endif
				commandList.Add(lastCommand);
				AllCommandCount += _stats.CommandCount;
				_stats.CommandCount = AllCommandCount;
			}

			//���������� ����� ������� � ���� ������������ �����������,
			//������� �� ���� �������� ��� ���������, ��� ������� � ���, 
			//��� ��� ������ �� ��������������� ��� �������������, ������������� �� ����� �������
			var deleteCore = new List<string>();
			foreach (DataRow deleted in dtExistsCore.Rows)
				if ((deleted.RowState != DataRowState.Deleted))
				{
					_stats.DeleteCount++;
					deleteCore.Add(deleted["Id"].ToString());
				}
			if (deleteCore.Count > 0)
			{
				//���� ���� ������, ������� ����� ������� �� Core, �� ������� ���������
				var costsList = new List<string>();
				foreach (var c in currentCoreCosts)
					costsList.Add(c.costCode.ToString());

				var costCodeFilter = String.Join(", ", costsList.ToArray());

				//��������� ������� �� �������� �� CoreCosts �� ���������� CoreId
				//��� ������� �� Core, ������� �� ����� � ��������������� �����-�����
				var deleteCommandList = new List<string>();
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

			_logger.InfoFormat(_stats.GetStatUpdateMessage());

			updateUsedSynonymLogs =
				"update farm.UsedSynonymLogs set LastUsed = now() where SynonymCode in (" + String.Join(", ", synonymCodes.ToArray()) + ");";
			if (synonymFirmCrCodes.Count > 0)
				updateUsedSynonymLogs += "update farm.UsedSynonymFirmCrLogs set LastUsed = now() where SynonymFirmCrCode in (" + String.Join(", ", synonymFirmCrCodes.ToArray()) + ");";

			return commandList.ToArray();
		}

		private void UpdateCoreCosts(DataRow drExistsCore, StringBuilder sb, List<CoreCost> costs)
		{
			var drExistsCosts = drExistsCore.GetChildRows(relationExistsCoreToCosts);
			DataRow drCurrent;

			foreach (CoreCost c in costs)
				//���� �������� ��������������� ���� ������ ����, �� ����� �� ��������� ��� ���������, ����� ������������ ������ ���� �������
				if (c.cost.HasValue && (c.cost > 0))
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
						_stats.InsertCostCount++;
						_stats.CommandCount++;
						sb.AppendFormat("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost) values ({0}, {1}, {2});\r\n",
							drExistsCore["Id"], c.costCode, c.cost.Value.ToString(CultureInfo.InvariantCulture.NumberFormat));
					}
					else
					{
						//���� ���� ������� � �������� ���� ������, �� ��������� ���� � �������
						if (c.cost.Value.CompareTo(Convert.ToDecimal(drCurrent["Cost"])) != 0)
						{
							_stats.UpdateCostCount++;
							_stats.CommandCount++;
							sb.AppendFormat("update farm.CoreCosts set Cost = {0} where Core_Id = {1} and PC_CostCode = {2};\r\n",
								c.cost.Value.ToString(CultureInfo.InvariantCulture.NumberFormat), drExistsCore["Id"], c.costCode);
						}
						//������� ���� �� ���� �������, ����� ��� ��������� ������ �� �� �������������
						drCurrent.Delete();
					}
				}

			var deleteCosts = new List<string>();
			foreach (var deleted in drExistsCosts)
			{
				//����������� �� ���� ����������� ����� � ������� �� ������������ � ��������������� �����-�����,
				//������������� ������ ���� ����� ������� �� CoreCosts
				if (deleted.RowState != DataRowState.Deleted)
				{
					_stats.DeleteCostCount++;
					deleteCosts.Add(deleted["PC_CostCode"].ToString());
				}
				deleted.Delete();
			}
			if (deleteCosts.Count > 0)
			{
				_stats.CommandCount++;
				sb.AppendFormat("delete from farm.CoreCosts where Core_Id = {0} and PC_CostCode in ({1});\r\n", drExistsCore["Id"], String.Join(", ", deleteCosts.ToArray()));
			}
		}

		private void UpdateCorePosition(DataRow drExistsCore, DataRow drCore, StringBuilder sb)
		{
			var updateFieldsScript = new List<string>();
			foreach (var compareField in compareFields)
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
				{
					if (compareField.ToLower() == "updatetime" || compareField.ToLower() == "quantityupdate")
						continue;

					if (drCore.Table.Columns[compareField].DataType == typeof (string))
					{
						if (drCore[compareField] is DBNull)
							updateFieldsScript.Add(compareField + " = ''");
						else
							updateFieldsScript.Add(String.Format("{0} = '{1}'", compareField,
							                                     SqlBuilder.StringToMySqlString(drCore[compareField].ToString())));
					}
					else if (drCore[compareField] is DBNull)
						updateFieldsScript.Add(compareField + " = null");
					else if (drCore.Table.Columns[compareField].DataType == typeof (decimal))
						updateFieldsScript.Add(String.Format("{0} = {1}", compareField, Convert.ToDecimal(drCore[compareField]).ToString(CultureInfo.InvariantCulture.NumberFormat)));
					else
						updateFieldsScript.Add(String.Format("{0} = {1}", compareField, drCore[compareField]));

					if (compareField.ToLower() == "quantity")
						updateFieldsScript.Add("QuantityUpdate = now()");
					else
						updateFieldsScript.Add("UpdateTime = now()");
				}
			}

			if (updateFieldsScript.Count > 0)
			{
				_stats.UpdateCount++;
				_stats.CommandCount++;
				sb.AppendFormat("update farm.Core0 set {0} where Id = {1};\r\n", String.Join(", ", updateFieldsScript.ToArray()), drExistsCore["Id"]);
			}
		}

		private DataRow FindPositionInExistsCore(DataRow drCore)
		{
			var dtSearchTime = DateTime.UtcNow;
			var filter = new List<string>();
			foreach (string primaryField in primaryFields)
			{
				if (drCore.Table.Columns[primaryField].DataType == typeof(string))
				{
					if (drCore[primaryField] is DBNull)
						filter.Add(String.Format("({0} = '')", primaryField));
					else
						filter.Add(String.Format("({0} = '{1}')", primaryField, drCore[primaryField]));
				}
				else if (drCore.Table.Columns[primaryField].DataType == typeof(decimal))
				{
					if (drCore[primaryField] is DBNull)
						filter.Add(String.Format("({0} is null)", primaryField));
					else
						filter.Add(String.Format("({0} = {1})", primaryField, Convert.ToString(drCore[primaryField], CultureInfo.InvariantCulture)));					
				}
				else
					if (drCore[primaryField] is DBNull)
						filter.Add(String.Format("({0} is null)", primaryField));
					else
						filter.Add(String.Format("({0} = {1})", primaryField, drCore[primaryField]));
			}
			var filterString = String.Join(" and ", filter.ToArray());
			var drsExists = dtExistsCore.Select(filterString);
			var tsSearchTime = DateTime.UtcNow.Subtract(dtSearchTime);
			_stats.AvgSearchTime += Convert.ToInt32(tsSearchTime.TotalMilliseconds);

			if (drsExists.Length == 0)
				return null;
			if (drsExists.Length == 1)
			{
				_stats.FirstSearch++;
				return drsExists[0];
			}

			int maxMatchesNumber = 0;
			DataRow maxMatchesNumberRow = null;
			foreach (var drExists in drsExists)
			{
				var currentMatchesNumber = 0;
				foreach (var compareField in compareFields)
					if (drCore[compareField].Equals(drExists[compareField]))
						currentMatchesNumber++;
				if (currentMatchesNumber > maxMatchesNumber)
				{
					maxMatchesNumber = currentMatchesNumber;
					maxMatchesNumberRow = drExists;
				}
			}
			_stats.SecondSearch++;
			return maxMatchesNumberRow;
		}

		private static void InsertCoreCosts(StringBuilder sb, List<CoreCost> coreCosts)
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

		/// <summary>
		/// ��������� ������� ������, � ����������� ������������ ����������
		/// </summary>
		public void FinalizePrice()
		{
			//�������� � �������� ����������� ���������� ������ ��� ����������� �����-������
			if (downloaded)
			{
				ProcessUndefinedCost();
				ProcessZeroCost();
			}

			if (Settings.Default.CheckZero && (zeroCount > (formCount + unformCount + zeroCount) * 0.95) )
				throw new RollbackFormalizeException(Settings.Default.ZeroRollbackError, firmCode, priceCode, firmShortName, priceName, formCount, zeroCount, unformCount, forbCount);

			if (formCount * 1.6 < prevRowCount)
				throw new RollbackFormalizeException(Settings.Default.PrevFormRollbackError, firmCode, priceCode, firmShortName, priceName, formCount, zeroCount, unformCount, forbCount);

			string[] insertCoreAndCoreCostsCommandList;

			//���������� ���������� � ����������� �������� ������ � ������� ���
			bool res = false;
			int tryCount = 0;
			//��� ����������� ����������
			StringBuilder sbLog;
			do
			{
				_logger.Info("FinalizePrice started.");
				sbLog = new StringBuilder();

				var finalizeTransaction = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);

				InsertNewProducerSynonyms(finalizeTransaction);

				string updateUsedSynonymLogs;

				if (priceCodesUseUpdate.Contains(priceCode))
				{
					Stopwatch GetSQLWatch = Stopwatch.StartNew();
					insertCoreAndCoreCostsCommandList = GetSQLToUpdateCoreAndCoreCosts(out updateUsedSynonymLogs);
					GetSQLWatch.Stop();
					_logger.InfoFormat("����� ����� ���������� update SQL-������ : {0}", GetSQLWatch.Elapsed);
				}
				else
					insertCoreAndCoreCostsCommandList = GetSQLToInsertCoreAndCoreCosts(out updateUsedSynonymLogs);

				try
				{
					MySqlCommand mcClear = new MySqlCommand();
					mcClear.Connection = MyConn;
					mcClear.Transaction = finalizeTransaction;
					mcClear.CommandTimeout = 0;

					mcClear.Parameters.Clear();

					//���������� ������ ��������, ���� �� ���� ������ update � ������� �����-�����, ��� ���� �� ������������� �����-���� � ���� ��� ��������
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
								//������� ���� �� CoreCosts
								var sbDelCoreCosts = new StringBuilder();
								sbDelCoreCosts.Append("delete from farm.CoreCosts where pc_costcode in (");
								bool FirstInsertCoreCosts = true;
								foreach (var c in currentCoreCosts)
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

						mcClear.CommandText = updateUsedSynonymLogs;
						mcClear.Parameters.Clear();
						sbLog.AppendFormat("UpdateSynonymLogs={0}  ", mcClear.ExecuteNonQuery());
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
					sbLog.AppendFormat("UpdateUnrecExp={0}  ", UnrecExpUpdate(finalizeTransaction));
					//���������� ��������� ����� ��������������, �.�. ��� ����� ����������
					sbLog.AppendFormat("UpdateExcludes={0}  ", TryUpdate(daExcludes, dtExcludes.Copy(), finalizeTransaction));
					sbLog.AppendFormat("UpdateAssortment={0}", TryUpdate(daAssortment, dtAssortment.Copy(), finalizeTransaction));

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
					_logger.DebugFormat(
						"Statistica search: assortment = {0} excludes = {1}  during assortment = {2} during excludes = {3}",
						(assortmentSearchCount > 0) ? assortmentSearchWatch.ElapsedMilliseconds / assortmentSearchCount : 0,
						(excludesSearchCount > 0) ? excludesSearchWatch.ElapsedMilliseconds / excludesSearchCount : 0,
						assortmentSearchWatch.ElapsedMilliseconds,
						excludesSearchWatch.ElapsedMilliseconds);

					//���������� ��������, ��� � Core ���������� ����� �� ���-�� �������, ��� � � ���������� formCount
					int existsCoreCount;
					if (costType == CostTypes.MiltiFile)
					{
						mcClear.CommandText = String.Format(@"
select
  count(*)
from
  farm.CoreCosts,
  farm.Core0
where
    CoreCosts.Core_Id = Core0.Id
and Core0.PriceCode = {0}
and CoreCosts.PC_CostCode = {1};", priceCode, costCode);
						existsCoreCount = Convert.ToInt32(mcClear.ExecuteScalar());
					}
					else
					{
						mcClear.CommandText = String.Format(@"
select
  count(*)
from
  farm.Core0
where
  Core0.PriceCode = {0};", priceCode);
						existsCoreCount = Convert.ToInt32(mcClear.ExecuteScalar());
					}
					if (existsCoreCount != formCount)
						throw new FormalizeException(
							String.Format(
								"��� ���������� �����-����� � ���� ������ �������� ���������� ������� � Core " +
								"�� ������������� ���������� ��������������� �������. ���-�� � Core: {0}  " + 
								"���-�� ��������������� �������: {1}",
								existsCoreCount,
								formCount),
							firmCode, priceCode, firmShortName, priceName);

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
						_logger.InfoFormat("Try transaction: tryCount = {0}  ErrorNumber = {1}  ErrorMessage = {2}", tryCount, MyError.Number, MyError.Message);
						//���������� ����� � ��������� � � Core, CoreCosts, ����� ��� ����������� ���������� ���� � ��� ����������������
						if (priceCodesUseUpdate.Contains(priceCode))
						{
								_stats.ResetCountersForUpdate();
								dtExistsCore.RejectChanges();
								dtExistsCoreCosts.RejectChanges();
						}
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
						throw new Exception(String.Format("�� ����� ����� ��������� ���� ������ ����������: {0}  {1}", drUnrecExp["FirmCr"], drUnrecExp));
					else
						if (drsProducerSynonyms.Length == 1)
						{
							drUnrecExp["ProducerSynonymId"] = drsProducerSynonyms[0]["SynonymFirmCrCode"];
							//���� ��������� ������� ����� � ��� �������� ��� ���������� �����-����� � ����
						    //� ���� �� ���������� ������ �� ����� �������
							if ((drsProducerSynonyms[0].RowState == DataRowState.Unchanged) && !Convert.IsDBNull(drUnrecExp["InternalProducerSynonymId"]))
							{
								drUnrecExp["InternalProducerSynonymId"] = DBNull.Value;
								//���� ������� �� ��������������, �� ����� ���������� CodeFirmCr
								if (!Convert.ToBoolean(drsProducerSynonyms[0]["IsAutomatic"]))
								{
									//���� CodeFirmCr �� ����������, �� ������� ������������� ����������� � "������������� �� ��������"
									if (Convert.IsDBNull(drsProducerSynonyms[0]["CodeFirmCr"]))
									{
										if (!Convert.IsDBNull(drUnrecExp["PriorProductId"]))
										{
											//���� ������������ �� ������������, �� ��� ��������� ������������ � ������� �� ��������������
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
												MyConn,
												"select CatalogId from catalogs.products p where Id = ?Productid",
												new MySqlParameter("?Productid", drUnrecExp["PriorProductId"])));
											long? synonymId = null;
											if (!Convert.IsDBNull(drUnrecExp["ProductSynonymId"]))
												synonymId = Convert.ToInt64(drUnrecExp["ProductSynonymId"]);
											var status = GetAssortmentStatus(
												CatalogId, 
												Convert.ToInt64(drUnrecExp["PriorProducerId"]), 
												Convert.ToInt64(drUnrecExp["ProducerSynonymId"]), 
												synonymId,
												pricePurpose == PricePurpose.Normal);
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
								throw new Exception(String.Format("�������� ����� ��������� ������ 1: {0}  {1}", drUnrecExp["FirmCr"], drUnrecExp));

					//���� �� ����������, ��� ������� ��-�� ����� ��������� ��������� ���� ��������� ����������, �� ��������� �� � ����
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
			daSynonymFirmCr.InsertCommand.Connection = MyConn;
			daSynonymFirmCr.InsertCommand.Transaction = finalizeTransaction;

			dtNewSynonymFirmCr = null;
			dtSynonymFirmCr.DefaultView.RowFilter = "InternalProducerSynonymId is not null";
			dtNewSynonymFirmCr = dtSynonymFirmCr.DefaultView.ToTable();

			if (!_stats.CanCreateProducerSynonyms())
				return;

			foreach (DataRow drNewProducerSynonym in dtNewSynonymFirmCr.Rows)
			{
				if (!Convert.IsDBNull(drNewProducerSynonym["SynonymFirmCrCode"]))
					//���� ��� �������� ������������� ����������, �� �� ��� ������ �� PriceProcessor � 
					//������� �� ���� ��� ���������� ������
					drNewProducerSynonym.AcceptChanges();
				else
				{
					var dsExistsProducerSynonym = MySqlHelper.ExecuteDataset(MyConn, @"
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
						//���� ��� ������� ����������, �� �������� ��� � ����
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
		/// ����������� ���� � ��������� ������, ���� ������� ������� ����� ����� 5% ������� � ��������������� �����
		/// </summary>
		private void ProcessUndefinedCost()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var cost in currentCoreCosts)
				if (cost.undefinedCostCount > formCount * 0.05)
					stringBuilder.AppendFormat("������� ������� \"{0}\" ����� {1} ������� � ������������� �����\n", cost.costName, cost.undefinedCostCount);

			if (stringBuilder.Length > 0)
				SendAlertToUserFail(
					stringBuilder,
					"PriceProcessor: � �����-����� {0} ���������� {1} ������� ������� � �������������� ������",
					@"
������������!
  � �����-����� {0} ���������� {1} ������� ������� � �������������� ������.
  ������ ������� �������:
{2}

� ���������,
  PriceProcessor.");

		}

		/// <summary>
		/// ����������� ���� � ��������� ���������, ���� ������� ������� ����� ��� ������� �������������� � 0
		/// </summary>
		private void ProcessZeroCost()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var cost in currentCoreCosts)
				if ((cost.zeroCostCount > 0) && ((formCount == 0) || (cost.zeroCostCount == formCount)))
					stringBuilder.AppendFormat("������� ������� \"{0}\" ��������� ��������� '0'\n", cost.costName);

			if (stringBuilder.Length > 0)
				SendAlertToUserFail(
					stringBuilder,
					"PriceProcessor: � �����-����� {0} ���������� {1} ������� ������� �������, ��������� ����������� ����� \"0\"",
					@"
������������!
  � �����-����� {0} ���������� {1} ������� ������� �������, ��������� ����������� ����� '0'.
  ������ ������� �������:
{2}

� ���������,
  PriceProcessor.");

		}

		protected void SendAlertToUserFail(StringBuilder stringBuilder, string subject, string body)
		{
			var drProvider = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(), @"
select
  if(pd.CostType = 1, concat('[�������] ', pc.CostName), pd.PriceName) PriceName,
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

			_logger.DebugFormat("������������ �������������� � ���������� ������������ �����-�����: {0}", body);
			Mailer.SendUserFail(subject, body);
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

					if (dtPrice.Rows.Count > 0)
					{
						_logger.Debug("������� ������� ����������");
						MySqlTransaction _prepareTransaction = MyConn.BeginTransaction(IsolationLevel.ReadCommitted);
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
							InternalFormalize();
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
				_logger.Debug("����� Formalize");
				log4net.NDC.Pop();
			}
		}

		private void InternalFormalize()
		{
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

				if (priceType != Settings.Default.ASSORT_FLG)
				{
					var costCount = ProcessCosts();
					var currentQuantity = GetFieldValueObject(PriceFields.Quantity);

					//���� ���-�� ��������� ��� = 0, �� ����� ���������� ������� � Zero
					//��� ���� ���������� ����������� � ��� ����� 0
					if (0 == costCount || (currentQuantity is int && (int)currentQuantity == 0))
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

				//���� �� �������� CodeFirmCr, �� �������, 
				//��� ������� ������������� �� ������������, �.�. ������������� �� ������� � ��������� �� ������������ ������
				/*
				�������� ��������:
				  UnrecExpStatus.NameForm UnrecExpStatus.FirmForm UnrecExpStatus.AssortmentForm
				  UnrecExpStatus.NameForm                         UnrecExpStatus.AssortmentForm
				*/
				//��������� �����������
				if (position.ProductId.HasValue && position.CodeFirmCr.HasValue)
					GetAssortmentStatus(position);

				//����������, ��� ���� ������������� �� ������������, �� ��� ������� ����� ���������� �������
				if (position.IsSet(UnrecExpStatus.NameForm))
				{
					if (!position.IsHealth())
						throw new Exception(String.Format("�� ������ ��������� ������������� ������� {0}, ����������� �������� ������", position.PositionName));

					InsertToCore(position);
				}
				else
					unformCount++;

				if (position.IsNotSet(UnrecExpStatus.FullForm) && position.IsNotSet(UnrecExpStatus.ExcludeForm))
					InsertToUnrec(position);

			}
			while (Next());
		}

		/// <summary>
		/// ���������� �������� ����, ������� ����� ������� �� ������ ������
		/// </summary>
		public void SetFieldName(PriceFields PF, string Value)
		{
			FieldNames[(int)PF] = Value;
		}

		/// <summary>
		/// �������� �������� ����
		/// </summary>
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

			return false;
		}

		/// <summary>
		/// �������� ����� �������� �������� ����
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public virtual string GetFieldRawValue(PriceFields field)
		{
			try
			{
				//���� ��� ������� ��� ���� �� ����������, �� ���������� null
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
		/// ������ � ���� ������ �������� � 1251, ���� �� ������� � ���� ������ �������� ��� � 1251 
		/// �� ����� �� ������� �� ���� �� ������ � ��� �� "�������" �� ����� ������ ����� ������� �� 
		/// ����� ��������
		/// </summary>
		public string CleanupCharsThatNotFitIn1251(string value)
		{
			var ansi = Encoding.GetEncoding(1251);
			var unicodeBytes = Encoding.Unicode.GetBytes(value);
			var ansiBytes = Encoding.Convert(Encoding.Unicode, ansi, unicodeBytes);
			return ansi.GetString(ansiBytes);
		}

		/// <summary>
		/// �������� �������� ���� � ������������ ����
		/// </summary>
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
				case (int)PriceFields.ProducerCost:
					return ProcessCost(GetFieldRawValue(PF));

				case (int)PriceFields.Quantity:
				case (int)PriceFields.MinOrderCount:
				case (int)PriceFields.Nds:
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

					//���� �������������� ������ �����, �� ���������� DBNull
					if (String.IsNullOrEmpty(res))
						return DBNull.Value;

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
		/// ���������� �������� IntValue � �������� ���������� ��� ����� �����
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
					return Value;
				}
				return Value;
			}
			return null;
		}

		//���������� �� �������� � ������� ����������� ����
		public bool IsForbidden(string PosName)
		{
			DataRow[] dr = dtForbidden.Select(String.Format("Forbidden = '{0}'", PosName.Replace("'", "''")));
			return dr.Length > 0;
		}

		//������ �� �� ���������� ������� �� ����, ����� � ������������� ��������?
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
			var assortmentStatus = 
				GetAssortmentStatus(
					position.CatalogId, 
					position.CodeFirmCr, 
					position.SynonymFirmCrCode,
					position.SynonymCode,
					pricePurpose == PricePurpose.Normal);

			//���� �������� ����������, �� ���������� CodeFirmCr
			if (assortmentStatus == UnrecExpStatus.MarkExclude)
				position.CodeFirmCr = null;
			position.AddStatus(assortmentStatus);
		}

		public UnrecExpStatus GetAssortmentStatus(long? CatalogId, long? ProducerId, long? ProducerSynonymId, long? synonymId, bool insertIfNotFound)
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

			//���� �� ������ �� �����, �� ��������� � ����������
			if (dr == null || dr.Length == 0 && insertIfNotFound)
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
			var count = Convert.ToInt32(MySqlHelper.ExecuteScalar(MyConn, query));
			return (count == 0);
		}

		//������ �� �� ���������� ������������� �� ��������?
		public void GetCodeFirmCr(FormalizationPosition position)
		{
			if (String.IsNullOrEmpty(position.FirmCr))
			{
				//���� � ������������� ������ �� ��������, �� ������������� ��� � null, � �������, ��� ���������� �� �������������
				position.AddStatus(UnrecExpStatus.FirmForm);
			}
			else
			{
				var dr = dtSynonymFirmCr.Select(String.Format("Synonym = '{0}'", position.FirmCr.Replace("'", "''")));
				if ((null != dr) && (dr.Length > 0))
				{
					//���� �������� CodeFirmCr �� �����������, �� ������������� � null, ����� ����� �������� ����
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
					//���� ������� ���������� �� ������������, �� ��������� �������������� �������, ���� ��� ���
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

			var trueValues = new[] { "������", "true"};
			var falseValues = new[] { "����", "false" };

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

				//���� � ������� �������� �����, �� ���������� �������� �� ���������
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
		/// �������� �������� ���� Junk
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
		/// ������������ ���� � ���������� ���-�� �� ������� ���
		/// </summary>
		/// <returns></returns>
		public int ProcessCosts()
		{
			int res = 0;
			object costValue;
			foreach(var c in currentCoreCosts)
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

				//���� ��������������� ����, �� ����������� �������
				if (!c.cost.HasValue)
					c.undefinedCostCount++;
				//���� ������� ����, �� ����������� �������
				if (c.cost == 0)
					c.zeroCostCount++;
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
