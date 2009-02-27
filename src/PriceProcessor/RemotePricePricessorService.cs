using System;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;
using System.IO;

namespace Inforoom.PriceProcessor
{
	public class RemotePricePricessorService : MarshalByRefObject, RemotePricePricessor.IRemotePriceProcessor
	{
		public void ResendPrice(ulong DownLogId)
		{
			var drFocused = MySqlHelper.ExecuteDataRow(
				Literals.ConnectionString(),
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
  pricefmts.FileExtention as DFileExtention,
  pi.IsForSalve
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

			var files = Directory.GetFiles(Path.GetFullPath(Settings.Default.HistoryPath), drFocused["DRowID"].ToString() + "*");

			if (Convert.ToBoolean(drFocused["IsForSalve"]))
			{
				Marshal(DownLogId);
				return;
			}

			if (files.Length == 0)
				throw new Exception("Данный прайс-лист в архиве отсутствует!");

			var sourceFile = String.Empty;
			var TempDirectory = Path.GetTempPath();
			TempDirectory += drFocused["DArchFileName"].ToString();

			if (ArchiveHelper.IsArchive(files[0]))
			{
				if (Directory.Exists(TempDirectory))
					Directory.Delete(TempDirectory, true);
				Directory.CreateDirectory(TempDirectory);
				ArchiveHelper.Extract(files[0], drFocused["DExtrFileName"].ToString(), TempDirectory);
				var extractFiles = Directory.GetFiles(TempDirectory, drFocused["DExtrFileName"].ToString());
				if (extractFiles.Length > 0)
					sourceFile = extractFiles[0];
				else
					throw new Exception(String.Format("Невозможно найти файл {0} в распакованном архиве!", drFocused["DExtrFileName"]));
			}
			else
			{
				sourceFile = files[0];
			}

			if (String.IsNullOrEmpty(sourceFile))
				return;

			var PriceExtention = drFocused["DFileExtention"].ToString();
			var destinationFile = Common.FileHelper.NormalizeDir(Settings.Default.InboundPath) + "d" + drFocused["DPriceItemId"] + "_" + DownLogId + PriceExtention;

			if (File.Exists(destinationFile))
				throw new Exception(String.Format("Данный прайс-лист находится в очереди на формализацию в папке {0}!",
				                                  Settings.Default.InboundPath));

			File.Copy(sourceFile, destinationFile);
			var item = new PriceProcessItem(
				true,
				Convert.ToUInt64(drFocused["DPriceCode"].ToString()),
				(drFocused["DCostCode"] is DBNull) ? null : (ulong?) Convert.ToUInt64(drFocused["DCostCode"].ToString()),
				Convert.ToUInt64(drFocused["DPriceItemId"].ToString()),
				destinationFile,
				(drFocused["ParentSynonym"] is DBNull) ? null : (ulong?) Convert.ToUInt64(drFocused["ParentSynonym"].ToString()));
			PriceItemList.AddItem(item);

			if (Directory.Exists(TempDirectory))
				FileHelper.Safe(() => Directory.Delete(TempDirectory, true));
		}

		private static void Marshal(ulong id)
		{
			var slave = (RemotePricePricessor.IRemotePriceProcessor)Activator.GetObject(typeof(RemotePricePricessor.IRemotePriceProcessor), "http://fmsold.adc.analit.net:888/RemotePriceProcessor");
			slave.ResendPrice(id);
		}
	}
}
