using System;
using System.IO;
using System.Data;
using MySql.Data.MySqlClient;

namespace Inforoom.Formalizer
{
	/// <summary>
	/// Summary description for PricesValidator.
	/// </summary>
	public class PricesValidator
	{

		/// <summary>
		/// Производит проверку прайса на существования правил и создает соответствующий тип парсера
		/// </summary>
		/// <param name="FileName"></param>
		/// <returns></returns>
		public static BasePriceParser Validate(MySqlConnection myconn, string FileName, string TempFileName)
		{
			string ShortFileName = Path.GetFileName(FileName);
			string TestQuery = "SELECT " +
				"IF(FR.ParentFormRules,FR.ParentFormRules,FR.FirmCode) as FormID, " +
				"IF(FR.ParentSynonym,FR.ParentSynonym,FR.FirmCode) AS ParentSynonym," +
				"FR.FirmCode as SelfPriceCode, " +
				"CD.ShortName as ClientShortName, " +
				"CD.FirmCode as ClientCode, " +
				"PD.PriceName as SelfPriceName, " +
				"'" + ShortFileName + "' AS PriceFile," +
				"FR.Flag as SelfFlag," +
				"FR.JunkPos as SelfJunkPos," +
				"FR.AwaitPos as SelfAwaitPos," +
				"FR.PosNum as SelfPosNum," +
				"PFR.*, " +
				"CD.FirmStatus, " +
				"CD.BillingStatus, " +
				"CD.FirmSegment " +
				"FROM UserSettings.PricesData AS PD " +
				"INNER JOIN UserSettings.ClientsData AS CD Using(FirmCode) " +
				"INNER JOIN Farm.formrules AS FR ON FR.FirmCode=PD.PriceCode " +
				"LEFT JOIN Farm.FormRules AS PFR ON PFR.FirmCode=IF(FR.ParentFormRules,FR.ParentFormRules,FR.FirmCode) " +
				"WHERE (FR.FirmCode=" + Path.GetFileNameWithoutExtension(ShortFileName) + ")";

			DataTable dtFormRules = new DataTable("FromRules");
			myconn.Open();
			MySqlDataAdapter daFormRules = new MySqlDataAdapter(TestQuery, myconn);
			daFormRules.Fill(dtFormRules);
			if ( dtFormRules.Rows.Count > 0 )
			{	
				string currPriceFMT = dtFormRules.Rows[0][FormRules.colPriceFMT].ToString().ToUpper();

				#region Копирование файла
				//Здесь будем производить копирование файла
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
								String.Format(FormalizeSettings.FileCopyError, FileName, Path.GetDirectoryName(TempFileName), e), 
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

						if (currPriceFMT == FormalizeSettings.EXCEL_FMT)
							return new ExcelPriceParser(FileName, myconn, dtFormRules);
						else if (currPriceFMT == FormalizeSettings.DBF_FMT)
							return new DBFPriceParser(FileName, myconn, dtFormRules);
						else if (currPriceFMT == FormalizeSettings.DB_FMT)
							return new DBPriceParser(FileName, myconn, dtFormRules);
						else if (currPriceFMT == FormalizeSettings.WIN_FMT || currPriceFMT == FormalizeSettings.DOS_FMT)
						{
							if (String.Empty == dtFormRules.Rows[0][FormRules.colDelimiter].ToString())
								return new TXTFPriceParser(FileName, myconn, dtFormRules);
							else
								return new TXTDPriceParser(FileName, myconn, dtFormRules);
						}
						else
							throw new WarningFormalizeException(
								String.Format(FormalizeSettings.UnknownPriceFMTError, currPriceFMT), 
								Convert.ToInt64(dtFormRules.Rows[0][FormRules.colClientCode]), 
								Convert.ToInt64(dtFormRules.Rows[0][FormRules.colSelfPriceCode]),
								(string)dtFormRules.Rows[0][FormRules.colClientShortName],
								(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);

					}
					else
					{
						throw new WarningFormalizeException(
							FormalizeSettings.DisableByFirmStatusError, 
							Convert.ToInt64(dtFormRules.Rows[0][FormRules.colClientCode]), 
							Convert.ToInt64(dtFormRules.Rows[0][FormRules.colSelfPriceCode]),
							(string)dtFormRules.Rows[0][FormRules.colClientShortName],
							(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);
					}
				}
				else
					throw new WarningFormalizeException(
						FormalizeSettings.DisableByBillingStatusError, 
						Convert.ToInt64(dtFormRules.Rows[0][FormRules.colClientCode]), 
						Convert.ToInt64(dtFormRules.Rows[0][FormRules.colSelfPriceCode]),
						(string)dtFormRules.Rows[0][FormRules.colClientShortName],
						(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);

			}
			else
				throw new WarningFormalizeException(String.Format(FormalizeSettings.UnknownPriceError, ShortFileName));
		}
	}
}
