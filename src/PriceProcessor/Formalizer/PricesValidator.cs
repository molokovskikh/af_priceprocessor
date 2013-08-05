using System;
using System.IO;
using System.Data;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor;
using System.Reflection;
using NHibernate;

namespace Inforoom.Formalizer
{
	public class PricesValidator
	{
		public static Type GetParserClassName(string className)
		{
			var types = Assembly.GetExecutingAssembly().GetModules()[0].FindTypes(Module.FilterTypeNameIgnoreCase, className);
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

			var dataRow = dtFormRules.Rows[0];
			var currentParserClassName = dataRow[FormRules.colParserClassName].ToString();

			//Здесь будем производить копирование файла
			var copyErrorCount = 0;
			var copySucces = false;
			do {
				try {
					File.Copy(fileName, tempFileName, true);
					copySucces = true;
				}
				catch (Exception e) {
					if (copyErrorCount < 10) {
						copyErrorCount++;
						System.Threading.Thread.Sleep(500);
					}
					else
						throw new FormalizeException(
							String.Format(Settings.Default.FileCopyError, fileName, Path.GetDirectoryName(tempFileName), e),
							Convert.ToInt64(dataRow[FormRules.colFirmCode]),
							Convert.ToInt64(dataRow[FormRules.colPriceCode]),
							(string)dataRow[FormRules.colFirmShortName],
							(string)dataRow[FormRules.colSelfPriceName]);
				}
			} while (!copySucces);

			fileName = tempFileName;

			if (dataRow[FormRules.colFirmStatus].ToString() == "0")
				throw new WarningFormalizeException(
					Settings.Default.DisableByFirmStatusError,
					Convert.ToInt64(dataRow[FormRules.colFirmCode]),
					Convert.ToInt64(dataRow[FormRules.colPriceCode]),
					(string)dataRow[FormRules.colFirmShortName],
					(string)dataRow[FormRules.colSelfPriceName]);

			//Проверяем типы ценовых колонок прайса: установлена ли она или нет, известный ли тип ценовых колонок
			if (dataRow.IsNull(FormRules.colCostType) || !Enum.IsDefined(typeof(CostTypes), Convert.ToInt32(dataRow[FormRules.colCostType])))
				throw new WarningFormalizeException(
					String.Format(Settings.Default.UnknowCostTypeError, dataRow[FormRules.colCostType]),
					Convert.ToInt64(dataRow[FormRules.colFirmCode]),
					Convert.ToInt64(dataRow[FormRules.colPriceCode]),
					(string)dataRow[FormRules.colFirmShortName],
					(string)dataRow[FormRules.colSelfPriceName]);

			var parserClass = GetParserClassName(currentParserClassName);
			if (parserClass == null)
				throw new WarningFormalizeException(
					String.Format(Settings.Default.UnknownPriceFMTError, currentParserClassName),
					Convert.ToInt64(dataRow[FormRules.colFirmCode]),
					Convert.ToInt64(dataRow[FormRules.colPriceCode]),
					(string)dataRow[FormRules.colFirmShortName],
					(string)dataRow[FormRules.colSelfPriceName]);

			return CreateFormalizer(fileName, dataRow, parserClass);
		}

		public static IPriceFormalizer CreateFormalizer(string fileName, DataRow dataRow, Type parserClass)
		{
			PriceFormalizationInfo priceInfo;
			using (new SessionScope()) {
				var price = Price.Find(Convert.ToUInt32(dataRow[FormRules.colPriceCode]));
				NHibernateUtil.Initialize(price);
				priceInfo = new PriceFormalizationInfo(dataRow, price);
			}
			return With.Connection(c => (IPriceFormalizer)Activator.CreateInstance(parserClass, new object[] { fileName, priceInfo }));
		}

		public static DataTable LoadFormRules(uint priceItemId)
		{
			var query = String.Format(@"
select distinct
  pi.Id as PriceItemId,
  pi.RowCount,
  pd.PriceCode,
  PD.PriceName as SelfPriceName,
  PD.PriceType,
  pd.CostType,
  if(pd.CostType = 1, pc.CostCode, null) CostCode,
  pc.CostName,
  s.Id as FirmCode,
  s.Name as FirmShortName,
  r.Region,
  not s.Disabled as FirmStatus,
  FR.JunkPos as SelfJunkPos,
  FR.AwaitPos as SelfAwaitPos,
  FR.VitallyImportantMask as SelfVitallyImportantMask,
  ifnull(pd.ParentSynonym, pd.PriceCode) as ParentSynonym,
  PFR.*,
  pricefmts.FileExtention,
  pricefmts.ParserClassName,
  pd.BuyingMatrix,
  pricefmts.Id as PriceFormat,
  FR.PriceEncode
from
  (usersettings.PriceItems pi,
  usersettings.pricescosts pc,
  UserSettings.PricesData pd,
  Customers.Suppliers s,
  Farm.formrules FR,
  Farm.FormRules PFR,
  farm.pricefmts)
  join Farm.Regions r on r.RegionCode = s.HomeRegion
where
	pi.Id = {0}
and pc.PriceItemId = pi.Id
and pd.PriceCode = pc.PriceCode
and ((pd.CostType = 1) or (exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pd.PriceCode and prd.BaseCost=pc.CostCode)))
and s.Id = pd.FirmCode
and FR.Id = pi.FormRuleId
and PFR.Id= if(FR.ParentFormRules, FR.ParentFormRules, FR.Id)
and pricefmts.ID = PFR.PriceFormatId",
				priceItemId);

			var dtFormRules = new DataTable("FromRules");
			With.Connection(c => {
				var daFormRules = new MySqlDataAdapter(query, c);
				daFormRules.Fill(dtFormRules);
			});
			return dtFormRules;
		}
	}
}