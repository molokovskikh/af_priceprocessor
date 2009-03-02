using System;
using System.Linq;
using LumiSoft.Net.SMTP.Client;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;
using System.IO;
using RemotePricePricessor;

namespace Inforoom.PriceProcessor
{
	public class RemotePricePricessorService : MarshalByRefObject, IRemotePriceProcessor
	{
		public void ResendPrice(ulong downlogId)
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
  pim.IsForSlave
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
						 new MySqlParameter("?DownLogId", downlogId));

			var files = Directory.GetFiles(Path.GetFullPath(Settings.Default.HistoryPath), drFocused["DRowID"] + "*");

#if !SLAVE
			if (Convert.ToBoolean(drFocused["IsForSlave"]))
			{
				GetSlave().ResendPrice(downlogId);
				return;
			}
#endif

			if (files.Length == 0)
				throw new PriceProcessorException("Данный прайс-лист в архиве отсутствует!");

			if (drFocused["DSourceType"].ToString().Equals("EMAIL", StringComparison.OrdinalIgnoreCase))
			{
				using (var fs = new FileStream(files[0], FileMode.Open, FileAccess.Read, FileShare.Read))
					SmtpClientEx.QuickSendSmartHost(Settings.Default.SMTPHost, 25, Environment.MachineName, "prices@analit.net", new[] { "prices@analit.net" }, fs);
				return;
			}

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
			var destinationFile = Common.FileHelper.NormalizeDir(Settings.Default.InboundPath) + "d" + drFocused["DPriceItemId"] + "_" + downlogId + PriceExtention;

			if (File.Exists(destinationFile))
				throw new PriceProcessorException(String.Format("Данный прайс-лист находится в очереди на формализацию в папке {0}!",
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

		public void RetransPrice(uint priceItemId)
		{
			var row = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(),
			                                     @"
select p.FileExtention,
	   pim.IsForSlave
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
where pim.Id = ?PriceItemId",
			                                     new MySqlParameter("?PriceItemId", priceItemId));
			var extention = row["FileExtention"];
			var isForSlave = Convert.ToBoolean(row["IsForSlave"]);

#if !SLAVE
			if (isForSlave)
			{
				GetSlave().RetransPrice(priceItemId);
				return;
			}
#endif

			var sourceFile = Path.Combine(Path.GetFullPath(Settings.Default.BasePath), priceItemId.ToString() + extention);
			var destinationFile = Path.Combine(Path.GetFullPath(Settings.Default.InboundPath), priceItemId.ToString() + extention);

			if (File.Exists(sourceFile))
			{
				if (!File.Exists(destinationFile))
				{
					File.Move(sourceFile, destinationFile);
					return;
				}
				throw new PriceProcessorException("Данный прайс-лист отсутствует!");
			}
			throw new PriceProcessorException("Данный прайс-лист находится в очереди на формализацию!");
		}

		public string[] ErrorFiles()
		{
#if SLAVE
			return Directory.GetFiles(Settings.Default.ErrorFilesPath);
#else
			return GetSlave().ErrorFiles().Union(Directory.GetFiles(Settings.Default.ErrorFilesPath)).ToArray();
#endif
		}

		public string[] InboundFiles()
		{
#if SLAVE
			return Directory.GetFiles(Settings.Default.InboundPath);
#else
			return GetSlave().InboundFiles().Union(Directory.GetFiles(Settings.Default.InboundPath)).ToArray();
#endif
		}

		public byte[] BaseFile(uint priceItemId)
		{
			var row = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(),@"
select p.FileExtention,
	   pim.IsForSlave
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
where pim.Id = ?PriceItemId", new MySqlParameter("?PriceItemId", priceItemId));
			var extention = row["FileExtention"];
			var isForSlave = Convert.ToBoolean(row["IsForSlave"]);

#if !SLAVE
			if (isForSlave)
				return GetSlave().BaseFile(priceItemId);
#endif

			var file = Path.Combine(Path.GetFullPath(Settings.Default.BasePath), priceItemId.ToString() + extention);
			if (!File.Exists(file))
				throw new PriceProcessorException("Данный прайс-лист отсутствует!");

			return File.ReadAllBytes(file);
		}

		private static IRemotePriceProcessor GetSlave()
		{
			return (IRemotePriceProcessor)Activator.GetObject(typeof(IRemotePriceProcessor), "http://fmsold.adc.analit.net:888/RemotePriceProcessor");
		}
	}
}
