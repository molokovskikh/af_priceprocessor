using System;
using System.IO;
using System.Data;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using System.Reflection;
using System.Configuration;
using Inforoom.PriceProcessor;

namespace Inforoom.Formalizer
{
	public class PricesValidator
	{
		public static Type GetParserClassName(string ParserClassName)
		{
			var types = Assembly.GetExecutingAssembly().GetModules()[0].FindTypes(Module.FilterTypeNameIgnoreCase, ParserClassName);
			if (types.Length == 1)
				return types[0];
			return null;
		}

		/// <summary>
		/// ���������� �������� ������ �� ������������� ������ � ������� ��������������� ��� �������
		/// </summary>
		public static BasePriceParser Validate(MySqlConnection myconn, string FileName, string TempFileName, PriceProcessItem item)
		{
			var shortFileName = Path.GetFileName(FileName);
			var dtFormRules = LoadFormRules((uint) item.PriceItemId, myconn);
			if ( dtFormRules.Rows.Count > 0 )
			{	
				var CurrentParserClassName = dtFormRules.Rows[0][FormRules.colParserClassName].ToString();

				#region ����������� �����
				//����� ����� ����������� ����������� �����
				int CopyErrorCount = 0;
				bool CopySucces = false;
				do
				{
					try
					{
						File.Copy(FileName, TempFileName, true);
						CopySucces = true;
					}
					catch(Exception e)
					{
						if (CopyErrorCount < 10 )
						{
							CopyErrorCount++;
							System.Threading.Thread.Sleep(500);
						}
						else
							throw new FormalizeException(
								String.Format(Settings.Default.FileCopyError, FileName, Path.GetDirectoryName(TempFileName), e), 
								Convert.ToInt64(dtFormRules.Rows[0][FormRules.colFirmCode]), 
								Convert.ToInt64(dtFormRules.Rows[0][FormRules.colPriceCode]),
								(string)dtFormRules.Rows[0][FormRules.colFirmShortName],
								(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);
					}
				}
				while(!CopySucces);

				FileName = TempFileName;
				#endregion

				if ("1" == dtFormRules.Rows[0][FormRules.colFirmStatus].ToString())
				{
					//��������� ���� ������� ������� ������: ����������� �� ��� ��� ���, ��������� �� ��� ������� �������
					if (!dtFormRules.Rows[0].IsNull(FormRules.colCostType) && Enum.IsDefined(typeof(CostTypes), Convert.ToInt32(dtFormRules.Rows[0][FormRules.colCostType])))
					{
						var parserClass = GetParserClassName(CurrentParserClassName);

						if (parserClass != null)
						{
							return (BasePriceParser)Activator.CreateInstance(parserClass, new object[] { FileName, myconn, dtFormRules });
						}
						throw new WarningFormalizeException(
							String.Format(Settings.Default.UnknownPriceFMTError, CurrentParserClassName),
							Convert.ToInt64(dtFormRules.Rows[0][FormRules.colFirmCode]),
							Convert.ToInt64(dtFormRules.Rows[0][FormRules.colPriceCode]),
							(string)dtFormRules.Rows[0][FormRules.colFirmShortName],
							(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);
					}
					throw new WarningFormalizeException(
						String.Format(Settings.Default.UnknowCostTypeError, dtFormRules.Rows[0][FormRules.colCostType]),
						Convert.ToInt64(dtFormRules.Rows[0][FormRules.colFirmCode]),
						Convert.ToInt64(dtFormRules.Rows[0][FormRules.colPriceCode]),
						(string)dtFormRules.Rows[0][FormRules.colFirmShortName],
						(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);
				}
				throw new WarningFormalizeException(
					Settings.Default.DisableByFirmStatusError,
					Convert.ToInt64(dtFormRules.Rows[0][FormRules.colFirmCode]),
					Convert.ToInt64(dtFormRules.Rows[0][FormRules.colPriceCode]),
					(string)dtFormRules.Rows[0][FormRules.colFirmShortName],
					(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);
			}
			throw new WarningFormalizeException(String.Format(Settings.Default.UnknownPriceError, shortFileName));
		}

		public static DataTable LoadFormRules(uint priceItemId, MySqlConnection myconn)
		{
			var query = String.Format(@"
select
  pi.Id as PriceItemId,
  pi.RowCount,
  pd.PriceCode,
  PD.PriceName as SelfPriceName,
  PD.PriceType,
  pd.CostType,
  if(pd.CostType = 1, pc.CostCode, null) CostCode,
  CD.FirmCode,
  CD.ShortName as FirmShortName,
  CD.FirmStatus,
  CD.FirmSegment,
  FR.JunkPos                                            as SelfJunkPos,
  FR.AwaitPos                                           as SelfAwaitPos,
  FR.VitallyImportantMask                               as SelfVitallyImportantMask,
  ifnull(pd.ParentSynonym, pd.PriceCode)                as ParentSynonym,
  PFR.*,
  pricefmts.FileExtention,
  pricefmts.ParserClassName
from
  usersettings.PriceItems pi,
  usersettings.pricescosts pc,
  UserSettings.PricesData pd,
  UserSettings.ClientsData cd,
  Farm.formrules FR,
  Farm.FormRules PFR,
  farm.pricefmts 
where
    pi.Id = {0}
and pc.PriceItemId = pi.Id
and pd.PriceCode = pc.PriceCode
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and cd.FirmCode = pd.FirmCode
and FR.Id = pi.FormRuleId
and PFR.Id= if(FR.ParentFormRules, FR.ParentFormRules, FR.Id)
and pricefmts.ID = PFR.PriceFormatId", priceItemId);

			var dtFormRules = new DataTable("FromRules");
			myconn.Open();
			var daFormRules = new MySqlDataAdapter(query, myconn);
			daFormRules.Fill(dtFormRules);
			return dtFormRules;
		}
	}
}
