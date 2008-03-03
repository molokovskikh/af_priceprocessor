using System;
using System.IO;
using System.Data;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using System.Reflection;

namespace Inforoom.Formalizer
{
	/// <summary>
	/// Summary description for PricesValidator.
	/// </summary>
	public class PricesValidator
	{

		public static Type GetParserClassName(string ParserClassName)
		{
			Type[] types = Assembly.GetExecutingAssembly().GetModules()[0].FindTypes(Module.FilterTypeNameIgnoreCase, ParserClassName);
			if (types.Length == 1)
				return types[0];
			else
				return null;
		}

		/// <summary>
		/// ѕроизводит проверку прайса на существовани€ правил и создает соответствующий тип парсера
		/// </summary>
		/// <param name="FileName"></param>
		/// <returns></returns>
		public static BasePriceParser Validate(MySqlConnection myconn, string FileName, string TempFileName)
		{
			string ShortFileName = Path.GetFileName(FileName);
			string TestQuery = String.Format(
@"SELECT
        IF(FR.ParentFormRules,FR.ParentFormRules,FR.FirmCode) as FormID,
        IF(FR.ParentSynonym,FR.ParentSynonym,FR.FirmCode)     AS ParentSynonym,
        FR.FirmCode                                           as SelfPriceCode,
        CD.ShortName                                          as ClientShortName,
        CD.FirmCode                                           as ClientCode,
        PD.PriceName                                          as SelfPriceName,
        PD.PriceType                                          as SelfFlag,
        FR.JunkPos                                            as SelfJunkPos,
        FR.AwaitPos                                           as SelfAwaitPos,
        pui.RowCount                                          as SelfPosNum,
        FR.VitallyImportantMask                               as SelfVitallyImportantMask,           
        PFR.*,
        CD.FirmStatus,
        CD.BillingStatus,
        CD.FirmSegment,
        if(pc.PriceCode <> pc.ShowPriceCode, 1, 0)  as HasParentPrice,
        ppd.CostType
FROM    UserSettings.PricesData     AS PD
INNER JOIN UserSettings.ClientsData AS CD Using(FirmCode)
INNER JOIN Farm.formrules           AS FR
        ON FR.FirmCode= PD.PriceCode
inner join UserSettings.pricescosts pc on pc.PriceCode = pd.PriceCode
inner join UserSettings.PricesData ppd on ppd.PriceCode = pc.ShowPriceCode
inner join UserSettings.price_update_info pui on pui.PriceCode = pd.PriceCode
LEFT JOIN Farm.FormRules AS PFR
        ON PFR.FirmCode= IF(FR.ParentFormRules,FR.ParentFormRules,FR.FirmCode)
WHERE   (FR.FirmCode = {0})",
							  Path.GetFileNameWithoutExtension(ShortFileName));

			DataTable dtFormRules = new DataTable("FromRules");
			myconn.Open();
			MySqlDataAdapter daFormRules = new MySqlDataAdapter(TestQuery, myconn);
			daFormRules.Fill(dtFormRules);
			if ( dtFormRules.Rows.Count > 0 )
			{	
				string currPriceFMT = dtFormRules.Rows[0][FormRules.colPriceFMT].ToString().ToUpper();

				string CurrentParserClassName = "test";

				#region  опирование файла
				//«десь будем производить копирование файла
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
								Convert.ToInt64(dtFormRules.Rows[0][FormRules.colClientCode]), 
								Convert.ToInt64(dtFormRules.Rows[0][FormRules.colSelfPriceCode]),
								(string)dtFormRules.Rows[0][FormRules.colClientShortName],
								(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);
					}
				}
				while(!CopySucces);

				FileName = TempFileName;
				#endregion

				if ("1" == dtFormRules.Rows[0][FormRules.colBillingStatus].ToString())
				{
					if ("1" == dtFormRules.Rows[0][FormRules.colFirmStatus].ToString())
					{
						//ѕровер€ем типы ценовых колонок прайса: установлена ли она или нет, известный ли тип ценовых колонок
						if (!dtFormRules.Rows[0].IsNull(FormRules.colCostType) && Enum.IsDefined(typeof(CostTypes), Convert.ToInt32(dtFormRules.Rows[0][FormRules.colCostType])))
						{
							//ѕровер€ем, чтобы не было попыток формализовать колонку мультиколоночного прайс-листа
							if (Convert.ToBoolean(dtFormRules.Rows[0][FormRules.colHasParentPrice]) && (Convert.ToInt32(dtFormRules.Rows[0][FormRules.colCostType]) == (int)CostTypes.MultiColumn))
								throw new WarningFormalizeException(
									Settings.Default.MulticolumnAsPriceError,
									Convert.ToInt64(dtFormRules.Rows[0][FormRules.colClientCode]),
									Convert.ToInt64(dtFormRules.Rows[0][FormRules.colSelfPriceCode]),
									(string)dtFormRules.Rows[0][FormRules.colClientShortName],
									(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);

							Type parserClass = GetParserClassName(CurrentParserClassName);

							if (parserClass != null)
							{
								return (BasePriceParser)Activator.CreateInstance(parserClass, new object[] { FileName, myconn, dtFormRules });
							}
							else
								throw new WarningFormalizeException(
									String.Format(Settings.Default.UnknownPriceFMTError, currPriceFMT),
									Convert.ToInt64(dtFormRules.Rows[0][FormRules.colClientCode]),
									Convert.ToInt64(dtFormRules.Rows[0][FormRules.colSelfPriceCode]),
									(string)dtFormRules.Rows[0][FormRules.colClientShortName],
									(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);

						}
						else
							throw new WarningFormalizeException(
								String.Format(Settings.Default.UnknowCostTypeError, dtFormRules.Rows[0][FormRules.colCostType]),
								Convert.ToInt64(dtFormRules.Rows[0][FormRules.colClientCode]),
								Convert.ToInt64(dtFormRules.Rows[0][FormRules.colSelfPriceCode]),
								(string)dtFormRules.Rows[0][FormRules.colClientShortName],
								(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);
					}
					else
					{
						throw new WarningFormalizeException(
							Settings.Default.DisableByFirmStatusError, 
							Convert.ToInt64(dtFormRules.Rows[0][FormRules.colClientCode]), 
							Convert.ToInt64(dtFormRules.Rows[0][FormRules.colSelfPriceCode]),
							(string)dtFormRules.Rows[0][FormRules.colClientShortName],
							(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);
					}
				}
				else
					throw new WarningFormalizeException(
						Settings.Default.DisableByBillingStatusError, 
						Convert.ToInt64(dtFormRules.Rows[0][FormRules.colClientCode]), 
						Convert.ToInt64(dtFormRules.Rows[0][FormRules.colSelfPriceCode]),
						(string)dtFormRules.Rows[0][FormRules.colClientShortName],
						(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);

			}
			else
				throw new WarningFormalizeException(String.Format(Settings.Default.UnknownPriceError, ShortFileName));
		}
	}
}
