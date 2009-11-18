﻿using System;
using Inforoom.PriceProcessor.Properties;
using LumiSoft.Net.SMTP.Client;
using MySql.Data.MySqlClient;
using Inforoom.Common;
using System.IO;
using RemotePriceProcessor;
using Common.Tools;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace Inforoom.PriceProcessor
{
	public class RemotePriceProcessorService : MarshalByRefObject, IRemotePriceProcessor
	{
		private const string MessagePriceInQueue = "Данный прайс-лист находится в очереди на формализацию";

		private const string MessagePriceNotFoundInArchive = "Данный прайс-лист в архиве отсутствует";

		private const string MessagePriceNotFound = "Данный прайс-лист отсутствует";

		public void ResendPrice(ulong downlogId)
		{
			var drFocused = MySqlHelper.ExecuteDataRow(
				Literals.ConnectionString(),@"
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
and logs.Rowid = ?DownLogId", new MySqlParameter("?DownLogId", downlogId));

			var filename = GetFileFromArhive(downlogId);

			if (drFocused["DSourceType"].ToString().Equals("EMAIL", StringComparison.OrdinalIgnoreCase))
			{
				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
					SmtpClientEx.QuickSendSmartHost(Settings.Default.SMTPHost, 25, Environment.MachineName, "prices@analit.net", new[] { "prices@analit.net" }, fs);
				return;
			}

			var sourceFile = String.Empty;
			var TempDirectory = Path.GetTempPath();
			TempDirectory += drFocused["DArchFileName"].ToString();

			if (ArchiveHelper.IsArchive(filename))
			{
				if (Directory.Exists(TempDirectory))
					Directory.Delete(TempDirectory, true);
				Directory.CreateDirectory(TempDirectory);
				ArchiveHelper.Extract(filename, drFocused["DExtrFileName"].ToString(), TempDirectory);
				var extractFiles = Directory.GetFiles(TempDirectory, drFocused["DExtrFileName"].ToString());
				if (extractFiles.Length > 0)
					sourceFile = extractFiles[0];
				else
					throw new Exception(String.Format("Невозможно найти файл {0} в распакованном архиве!", drFocused["DExtrFileName"]));
			}
			else
			{
				sourceFile = filename;
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
			RetransPrice(priceItemId, Settings.Default.BasePath);
		}

		private void RetransPrice(uint priceItemId, string sourceDir)
		{
			var row = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(),
@"
select p.FileExtention
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
where pim.Id = ?PriceItemId", new MySqlParameter("?PriceItemId", priceItemId));
			var extention = row["FileExtention"];

			var sourceFile = Path.Combine(Path.GetFullPath(sourceDir), priceItemId.ToString() + extention);
			var destinationFile = Path.Combine(Path.GetFullPath(Settings.Default.InboundPath), priceItemId.ToString() + extention);

			if (File.Exists(sourceFile))
			{
				if (!File.Exists(destinationFile))
				{
					File.Move(sourceFile, destinationFile);
					return;
				}
				throw new FaultException<string>(MessagePriceInQueue, new FaultReason(MessagePriceInQueue));
			}
			throw new FaultException<string>(MessagePriceNotFound, new FaultReason(MessagePriceNotFound));
		}

		public void RetransErrorPrice(uint priceItemId)
		{
			RetransPrice(priceItemId, Settings.Default.ErrorFilesPath);
		}

		public string[] ErrorFiles()
		{
			return Directory.GetFiles(Settings.Default.ErrorFilesPath);
		}

		public string[] InboundFiles()
		{
			return Directory.GetFiles(Settings.Default.InboundPath);
		}

		public Stream BaseFile(uint priceItemId)
		{
			var row = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(),@"
select p.FileExtention
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
where pim.Id = ?PriceItemId", new MySqlParameter("?PriceItemId", priceItemId));
			var extention = row["FileExtention"];

			var baseFile = Path.Combine(Path.GetFullPath(Settings.Default.BasePath), priceItemId.ToString() + extention);
			var inboundFile = Path.Combine(Path.GetFullPath(Settings.Default.InboundPath), priceItemId.ToString() + extention);

			if (!File.Exists(baseFile) && !File.Exists(inboundFile))
				throw new PriceProcessorException("Данный прайс-лист отсутствует!");
			if (!File.Exists(baseFile) && File.Exists(inboundFile))
				throw new PriceProcessorException("Данный прайс-лист находится в очереди на формализацию!");

			return File.OpenRead(baseFile);
		}

		public HistoryFile GetFileFormHistory(WcfCallParameter downlog)
		{
			ulong downlogId = Convert.ToUInt64(downlog.Value);
			var filename = GetFileFromArhive(downlogId);
			return new HistoryFile
				       	{
							Filename = Path.GetFileName(filename),
							FileStream = File.OpenRead(filename),
				       	};
		}

		private static string GetFileFromArhive(ulong downlogId)
		{
			var files = Directory.GetFiles(Settings.Default.HistoryPath, downlogId + "*");
			if (files.Length == 0)
				throw new PriceProcessorException("Данный прайс-лист в архиве отсутствует!");
			return files[0];
		}

        public void PutFileToBase(FilePriceInfo filePriceInfo)
		{
            uint priceItemId = filePriceInfo.PriceItemId;
            Stream file = filePriceInfo.Stream;

			var row = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(), @"
select p.FileExtention
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
where pim.Id = ?PriceItemId", new MySqlParameter("?PriceItemId", priceItemId));
			var extention = row["FileExtention"];

			var newBaseFile = Path.Combine(Path.GetFullPath(Settings.Default.BasePath), priceItemId.ToString() + extention);

			if (File.Exists(newBaseFile))
				try
				{
					File.Delete(newBaseFile);
				}
				catch (Exception exception)
				{					
					throw new PriceProcessorException(
						String.Format("Невозможно удалить из Base старый файл {0}!", Path.GetFileName(newBaseFile)), 
						exception);
				}

			//file.Position = 0;
			using (var fileStream = File.Create(newBaseFile))
			{
				file.Copy(fileStream);
			}
		}
	}
}
