using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Inforoom.PriceProcessor.Properties;
using MySql.Data.MySqlClient;
using Inforoom.Common;
using System.IO;
using RemotePriceProcessor;
using Common.Tools;
using System.ServiceModel;
using LumiSoft.Net.Mime;
using log4net;
using Inforoom.Downloader;

namespace Inforoom.PriceProcessor
{
	public class WCFPriceProcessorService : IRemotePriceProcessor
	{
		private const string MessagePriceInQueue = "Данный прайс-лист находится в очереди на формализацию";

		private const string MessagePriceNotFoundInArchive = "Данный прайс-лист в архиве отсутствует";

		private const string MessagePriceNotFound = "Данный прайс-лист отсутствует";

		public void ResendPrice(WcfCallParameter paramDownlogId)
		{
			var downlogId = Convert.ToUInt64(paramDownlogId.Value);
			var drFocused = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(),
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
  s.ArchivePassword,
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
			var sourceArchiveFileName = filename;
			var archFileName = drFocused["DArchFileName"].ToString();
			var externalFileName = drFocused["DExtrFileName"].ToString();
			if (drFocused["DSourceType"].ToString().Equals("EMAIL", StringComparison.OrdinalIgnoreCase))
			{
				// Если файл пришел по e-mail, то это должен быть файл *.eml, открываем его на чтение
				filename = ExtractFileFromAttachment(filename, archFileName, externalFileName);
			}
			var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(archFileName));
			if (ArchiveHelper.IsArchive(filename))
			{
				if (Directory.Exists(tempDirectory))
					Directory.Delete(tempDirectory, true);
				Directory.CreateDirectory(tempDirectory);
				ArchiveHelper.Extract(filename, externalFileName, tempDirectory, drFocused["ArchivePassword"].ToString());
				filename = FileHelper.FindFromArhive(tempDirectory, externalFileName);
				if (String.IsNullOrEmpty(filename))
				{
					var errorMessage = String.Format(
						"Невозможно найти файл {0} в распакованном архиве!", externalFileName);
					throw new FaultException<string>(errorMessage, new FaultReason(errorMessage));
				}
			}

			if (String.IsNullOrEmpty(filename))
				return;

			var priceExtention = drFocused["DFileExtention"].ToString();
			var destinationFile = Common.FileHelper.NormalizeDir(Settings.Default.InboundPath) + 
				"d" + drFocused["DPriceItemId"] + "_" + downlogId + priceExtention;

			if (File.Exists(destinationFile))
			{
				throw new FaultException<string>(MessagePriceInQueue, 
					new FaultReason(MessagePriceInQueue));
			}

			File.Copy(filename, destinationFile);

			var item = new PriceProcessItem(true, 
				Convert.ToUInt64(drFocused["DPriceCode"].ToString()),
				(drFocused["DCostCode"] is DBNull) ? null : 
					(ulong?)Convert.ToUInt64(drFocused["DCostCode"].ToString()),
				Convert.ToUInt64(drFocused["DPriceItemId"].ToString()),
				destinationFile,
				(drFocused["ParentSynonym"] is DBNull) ? null : 
					(ulong?)Convert.ToUInt64(drFocused["ParentSynonym"].ToString()));
			PriceItemList.AddItem(item);

			var priceItemId = Convert.ToUInt64(drFocused["DPriceItemId"]);
			downlogId = LogResendPriceAsDownload(priceItemId, archFileName, externalFileName, paramDownlogId.LogInformation);
			if (downlogId > 0)
			{
				destinationFile = Common.FileHelper.NormalizeDir(Settings.Default.HistoryPath) + downlogId +
					Path.GetExtension(sourceArchiveFileName);
				File.Copy(sourceArchiveFileName, destinationFile);
			}
			if (Directory.Exists(tempDirectory))
				FileHelper.Safe(() => Directory.Delete(tempDirectory, true));
		}

		private string ExtractFileFromAttachment(string filename, string archFileName, string externalFileName)
		{
			using (var fs = new FileStream(filename, FileMode.Open, 
				FileAccess.Read, FileShare.Read))
			{
				var logger = LogManager.GetLogger(GetType());
				try
				{
					var message = Mime.Parse(fs);
					message = UueHelper.ExtractFromUue(message, Path.GetTempPath());

					var attachments = message.GetValidAttachements();
					foreach (var entity in attachments)
					{
						var attachmentFilename = entity.GetFilename();

						if (!FileHelper.CheckMask(attachmentFilename, archFileName) &&
							!FileHelper.CheckMask(attachmentFilename, externalFileName))
							continue;
						entity.DataToFile(attachmentFilename);
						return filename;
					}
				}
				catch (Exception ex)
				{
					logger.ErrorFormat(
						"Возникла ошибка при попытке перепровести прайс. Не удалось обработать файл {0}. Файл должен быть письмом (*.eml)\n{1}",
						filename, ex);
					throw;
				}
			}
			throw new Exception(String.Format("В архиве '{0}' не удалось найти архив '{1}' или файл прайс листа {2}", filename, archFileName, externalFileName));
		}

		public void RetransPrice(WcfCallParameter priceItemId)
		{
			RetransPrice(priceItemId, Settings.Default.BasePath);
		}

		private void RetransPrice(WcfCallParameter paramPriceItemId, string sourceDir)
		{
			var priceItemId = Convert.ToUInt64(paramPriceItemId.Value);

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

			if (File.Exists(destinationFile))
				throw new FaultException<string>(MessagePriceInQueue, new FaultReason(MessagePriceInQueue));

			if ((!File.Exists(sourceFile)) && (!File.Exists(destinationFile)))
				throw new FaultException<string>(MessagePriceNotFound, new FaultReason(MessagePriceNotFound));

			if (!File.Exists(sourceFile))
				throw new FaultException<string>(MessagePriceNotFound, new FaultReason(MessagePriceNotFound));
				
			File.Move(sourceFile, destinationFile);
		}

		public void RetransErrorPrice(WcfCallParameter priceItemId)
		{
			RetransPrice(priceItemId, Settings.Default.ErrorFilesPath);
		}

		public string[] ErrorFiles()
		{
			return Directory.GetFiles(Settings.Default.ErrorFilesPath);
		}

		public string[] InboundPriceItemIds()
		{
			var count = PriceItemList.list.Count;
			var priceItemIdList = new string[count];
			for (var index = 0; index < count; index++)
				 priceItemIdList[index] = Convert.ToString(PriceItemList.list[index].PriceItemId);
			return priceItemIdList;
		}

		public Stream BaseFile(uint priceItemId)
		{
			var row = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(),
@"
select p.FileExtention
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
where pim.Id = ?PriceItemId", new MySqlParameter("?PriceItemId", priceItemId));
			var extention = row["FileExtention"];

			var baseFile = Path.Combine(Path.GetFullPath(Settings.Default.BasePath), priceItemId.ToString() + extention);
			var inboundFile = Path.Combine(Path.GetFullPath(Settings.Default.InboundPath), priceItemId.ToString() + extention);

			if (!File.Exists(baseFile) && !File.Exists(inboundFile))
				throw new FaultException<string>(MessagePriceNotFound, 
					new FaultReason(MessagePriceNotFound));
			if (!File.Exists(baseFile) && File.Exists(inboundFile))
				throw new FaultException<string>(MessagePriceInQueue, 
					new FaultReason(MessagePriceInQueue));

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
				throw new FaultException<string>(MessagePriceNotFoundInArchive,
					new FaultReason(MessagePriceNotFoundInArchive));
			return files[0];
		}
		
		public void PutFileToInbound(FilePriceInfo filePriceInfo)
		{
			var priceItemId = filePriceInfo.PriceItemId;
			var file = filePriceInfo.Stream;

			var row = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(),
@"
select p.FileExtention
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
where pim.Id = ?PriceItemId", new MySqlParameter("?PriceItemId", priceItemId));
			var extention = row["FileExtention"];

			// Удаляем из Base файлы с таким же именем (расширения могут отличаться)
			var oldBaseFilePattern = priceItemId.ToString() + ".*";

			SearchAndDeleteFilesFromDirectory(Settings.Default.BasePath, oldBaseFilePattern);

			// На всякий случай ищем файлы с такими же именами в Inbound0, если есть, удаляем их
			SearchAndDeleteFilesFromDirectory(Settings.Default.InboundPath, oldBaseFilePattern);

			// Получаем полный путь к новому файлу
			var newFile = Path.Combine(Path.GetFullPath(Settings.Default.InboundPath), priceItemId.ToString() + extention);

			// Сохраняем новый файл
			using (var fileStream = File.Create(newFile))
			{
				file.Copy(fileStream);
			}
		}

		public string[] InboundFiles()
		{
			return Directory.GetFiles(Settings.Default.InboundPath);
		}

		public void PutFileToBase(FilePriceInfo filePriceInfo)
		{
			uint priceItemId = filePriceInfo.PriceItemId;
			Stream file = filePriceInfo.Stream;

			var row = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(),
@"
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
				catch (Exception)
				{
					string errorMessage = String.Format("Невозможно удалить из Base старый файл {0}!",
						Path.GetFileName(newBaseFile));
					throw new FaultException<string>(errorMessage, new FaultReason(errorMessage));
				}
			using (var fileStream = File.Create(newBaseFile))
			{
				file.Copy(fileStream);
			}
		}

		private static void SearchAndDeleteFilesFromDirectory(string directoryName, string fileNamePattern)
		{
			var directoryPath = Path.GetFullPath(directoryName);
			var files = Directory.GetFiles(directoryPath, fileNamePattern);
			foreach (var fileName in files)
			{
				try
				{
					File.Delete(fileName);
				}
				catch (Exception)
				{
					var errorMessage = String.Format(@"Невозможно удалить из {0} старый файл", directoryName);
					throw new FaultException<string>(errorMessage, new FaultReason(errorMessage));
				}
			}			
		}

		/// <summary>
		/// Добавляет для перепосылаемого прайса запись в таблицу logs.downlogs.
		/// </summary>
		/// <returns>RowId новой записи</returns>
		private static ulong LogResendPriceAsDownload(ulong priceItemId, string archiveFileName, string extractFileName, LogInformation logInformation)
		{
			const int resultCode = 2;
			var addition = String.Format("Прайс перепослан. Компьютер: {0}; Оператор: {1}", logInformation.ComputerName, logInformation.UserName);

			var query = String.Format(@"
INSERT INTO logs.downlogs (LogTime, Host, PriceItemId, Addition, ResultCode, ArchFileName, ExtrFileName)
VALUES (now(), ""{0}"", {1}, ""{2}"", {3}, ""{4}"", ""{5}""); SELECT last_insert_id()
", Environment.MachineName, priceItemId, addition, resultCode, archiveFileName, extractFileName);

			using (var connectionLog = new MySqlConnection(Literals.ConnectionString()))
			{
				try
				{
					connectionLog.Open();
					var commandLog = new MySqlCommand(query, connectionLog);
					var id = Convert.ToUInt64(commandLog.ExecuteScalar());
					return id;
				}
				catch (Exception ex)
				{
					Mailer.SendFromServiceToService("Ошибка логирования при перепосылке прайс-листа", ex.ToString());
					return 0;
				}
			}
		}
	}
}
