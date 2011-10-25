using System;
using System.IO;
using System.Data;
using Common.MySql;
using Inforoom.PriceProcessor.Formalizer.New;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor;
using System.Reflection;

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
		/// Производит проверку прайса на существования правил и создает соответствующий тип парсера
		/// </summary>
		public static IPriceFormalizer Validate(string fileName, string tempFileName, uint priceItemId)
		{
			var shortFileName = Path.GetFileName(fileName);
			var dtFormRules = LoadFormRules(priceItemId);
			if (dtFormRules.Rows.Count == 0)
				throw new WarningFormalizeException(String.Format(Settings.Default.UnknownPriceError, shortFileName));

			var currentParserClassName = dtFormRules.Rows[0][FormRules.colParserClassName].ToString();

			//Здесь будем производить копирование файла
			int CopyErrorCount = 0;
			bool CopySucces = false;
			do
			{
				try
				{
					File.Copy(fileName, tempFileName, true);
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
							String.Format(Settings.Default.FileCopyError, fileName, Path.GetDirectoryName(tempFileName), e), 
							Convert.ToInt64(dtFormRules.Rows[0][FormRules.colFirmCode]), 
							Convert.ToInt64(dtFormRules.Rows[0][FormRules.colPriceCode]),
							(string)dtFormRules.Rows[0][FormRules.colFirmShortName],
							(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);
				}
			}
			while(!CopySucces);

			fileName = tempFileName;

			if (dtFormRules.Rows[0][FormRules.colFirmStatus].ToString() == "0")
				throw new WarningFormalizeException(
					Settings.Default.DisableByFirmStatusError,
					Convert.ToInt64(dtFormRules.Rows[0][FormRules.colFirmCode]),
					Convert.ToInt64(dtFormRules.Rows[0][FormRules.colPriceCode]),
					(string)dtFormRules.Rows[0][FormRules.colFirmShortName],
					(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);

			//Проверяем типы ценовых колонок прайса: установлена ли она или нет, известный ли тип ценовых колонок
			if (dtFormRules.Rows[0].IsNull(FormRules.colCostType) || !Enum.IsDefined(typeof(CostTypes), Convert.ToInt32(dtFormRules.Rows[0][FormRules.colCostType])))
				throw new WarningFormalizeException(
					String.Format(Settings.Default.UnknowCostTypeError, dtFormRules.Rows[0][FormRules.colCostType]),
					Convert.ToInt64(dtFormRules.Rows[0][FormRules.colFirmCode]),
					Convert.ToInt64(dtFormRules.Rows[0][FormRules.colPriceCode]),
					(string)dtFormRules.Rows[0][FormRules.colFirmShortName],
					(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);

			var parserClass = GetParserClassName(currentParserClassName);
			if (parserClass == null)
				throw new WarningFormalizeException(
					String.Format(Settings.Default.UnknownPriceFMTError, currentParserClassName),
					Convert.ToInt64(dtFormRules.Rows[0][FormRules.colFirmCode]),
					Convert.ToInt64(dtFormRules.Rows[0][FormRules.colPriceCode]),
					(string)dtFormRules.Rows[0][FormRules.colFirmShortName],
					(string)dtFormRules.Rows[0][FormRules.colSelfPriceName]);

			return With.Connection(c => (IPriceFormalizer) Activator.CreateInstance(parserClass, new object[] {fileName, c, dtFormRules}));
		}

		public static DataTable LoadFormRules(uint priceItemId)
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
  pricefmts.ParserClassName,
  pd.BuyingMatrix,
  pricefmts.Id											as PriceFormat
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
			With.Connection(c => {
				var daFormRules = new MySqlDataAdapter(query, c);
				daFormRules.Fill(dtFormRules);
			});
			return dtFormRules;
		}
	}
}
