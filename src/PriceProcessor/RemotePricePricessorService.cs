using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;
using System.IO;
using System.Configuration;

namespace Inforoom.PriceProcessor
{
	public class RemotePricePricessorService : MarshalByRefObject, RemotePricePricessor.IRemotePriceProcessor
	{
		public void ResendPrice(ulong DownLogId)
		{
			DataRow drFocused = MySqlHelper.ExecuteDataRow(
				ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
				@"
SELECT
  logs.RowID as DRowID,
  logs.LogTime as DLogTime,
  logs.Addition as DAddition,
  logs.ArchFileName as DArchFileName,
  logs.ExtrFileName as DExtrFileName,
  cd.ShortName as DFirmName,
  r.Region as DRegion,
  cd.FirmSegment as DFirmSegment,
  if(pd.CostType = 1, concat('[Колонка] ', pc.CostName), pd.PriceName) as DPriceName,
  pim.Id as DPriceItemId,
  pd.PriceCode as DPriceCode,
  pd.ParentSynonym,
  if(pd.CostType = 1, pc.CostCode, null) DCostCode,
  st.Type as DSourceType,
  s.PricePath as DPricePath,
  s.EmailTo as DEmailTo,
  s.EmailFrom as DEmailFrom,
  pricefmts.FileExtention as DFileExtention
FROM
  logs.downlogs as logs,
  usersettings.clientsdata cd,
  usersettings.pricesdata pd,
  usersettings.pricescosts pc,
  usersettings.PriceItems pim,
  farm.regions r,
  farm.sources s,
  farm.Sourcetypes st,
  farm.formrules fr,
  farm.pricefmts 
WHERE
    pim.Id = logs.PriceItemId
and pc.PriceItemId = pim.Id
and pc.PriceCode = pd.PriceCode
and ((pd.CostType = 1) OR (pc.BaseCost = 1))
and cd.firmcode=pd.firmcode
and r.regioncode=cd.regioncode
and s.Id = pim.SourceId
and st.Id = s.SourceTypeId
and logs.ResultCode in (2, 3)
and fr.Id = pim.FormRuleId
and pricefmts.id = fr.PriceFormatId
and logs.Rowid = ?DownLogId",
						 new MySqlParameter("?DownLogId", DownLogId));

			string[] files = Directory.GetFiles(Path.GetFullPath(Settings.Default.HistoryPath), drFocused["DRowID"].ToString() + "*");

			if (files.Length > 0)
			{
				string sourceFile = String.Empty;
				string TempDirectory = Path.GetTempPath();
				TempDirectory += drFocused["DArchFileName"].ToString();

				if (ArchiveHelper.IsArchive(files[0]))
				{
					if (Directory.Exists(TempDirectory))
						Directory.Delete(TempDirectory, true);
					Directory.CreateDirectory(TempDirectory);
					ArchiveHelper.Extract(files[0], drFocused["DExtrFileName"].ToString(), TempDirectory);
					string[] extractFiles = Directory.GetFiles(TempDirectory, drFocused["DExtrFileName"].ToString());
					if (extractFiles.Length > 0)
						sourceFile = extractFiles[0];
					else
						throw new Exception(String.Format("Невозможно найти файл {0} в распакованном архиве!", drFocused["DExtrFileName"]));
				}
				else
				{
					sourceFile = files[0];
				}

				if (!String.IsNullOrEmpty(sourceFile))
				{
					string PriceExtention = drFocused["DFileExtention"].ToString();
					string destinationFile = FileHelper.NormalizeDir(Settings.Default.InboundPath) + "d" + drFocused["DPriceItemId"].ToString() + "_" + DownLogId + PriceExtention;

					if (!File.Exists(destinationFile))
					{
						File.Copy(sourceFile, destinationFile);
						PriceProcessItem item = new PriceProcessItem(
							true, 
							Convert.ToUInt64(drFocused["DPriceCode"].ToString()), 
							(drFocused["DCostCode"] is DBNull) ? null : (ulong?)Convert.ToUInt64(drFocused["DCostCode"].ToString()),
							Convert.ToUInt64(drFocused["DPriceItemId"].ToString()), 
							destinationFile,
							(drFocused["ParentSynonym"] is DBNull) ? null : (ulong?)Convert.ToUInt64(drFocused["ParentSynonym"].ToString()));
						item.FileTime = DateTime.Now;
						PriceItemList.AddItem(item);

						if (Directory.Exists(TempDirectory))
							try
							{
								Directory.Delete(TempDirectory, true);
							}
							catch { }
					}
					else
						throw new Exception(String.Format("Данный прайс-лист находится в очереди на формализацию в папке {0}!", Settings.Default.InboundPath));
				}
			}
			else
				throw new Exception("Данный прайс-лист в архиве отсутствует!");

		}		
	}
}
