using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Common.MySql;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;
using Inforoom.Common;
using System.IO;
using RemotePriceProcessor;
using Common.Tools;
using System.ServiceModel;
using LumiSoft.Net.Mime;
using log4net;
using Inforoom.Downloader;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.PriceProcessor
{
	public class WCFPriceProcessorService : IRemotePriceProcessor, IRemotePriceProcessorOneWay
	{
		private static ILog log = LogManager.GetLogger(typeof(WCFPriceProcessorService));

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
  sp.Name as DFirmName,
  r.Region as DRegion,
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
  Customers.Suppliers sp,
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
and sp.Id = pd.firmcode
and r.regioncode = sp.HomeRegion
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
			if (drFocused["DSourceType"].ToString().Equals("EMAIL", StringComparison.OrdinalIgnoreCase)) {
				// Если файл пришел по e-mail, то это должен быть файл *.eml, открываем его на чтение
				filename = ExtractFileFromAttachment(filename, archFileName, externalFileName);
			}
			var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(archFileName));
			if (ArchiveHelper.IsArchive(filename)) {
				if (File.Exists(tempDirectory))
					File.Delete(tempDirectory);
				if (Directory.Exists(tempDirectory))
					Directory.Delete(tempDirectory, true);
				Directory.CreateDirectory(tempDirectory);
				ArchiveHelper.Extract(filename, externalFileName, tempDirectory, drFocused["ArchivePassword"].ToString());
				filename = FileHelper.FindFromArhive(tempDirectory, externalFileName);
				if (String.IsNullOrEmpty(filename)) {
					var errorMessage = String.Format(
						"Невозможно найти файл {0} в распакованном архиве!", externalFileName);
					throw new FaultException<string>(errorMessage, new FaultReason(errorMessage));
				}
			}

			if (String.IsNullOrEmpty(filename))
				return;

			var priceExtention = drFocused["DFileExtention"].ToString();
			var destinationFile = Path.Combine(Settings.Default.InboundPath,
				"d" + drFocused["DPriceItemId"] + "_" + downlogId + priceExtention);

			if (File.Exists(destinationFile)) {
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
			if (downlogId > 0) {
				destinationFile = Path.Combine(Settings.Default.HistoryPath,
					downlogId + Path.GetExtension(sourceArchiveFileName));
				File.Copy(sourceArchiveFileName, destinationFile);
			}
			if (Directory.Exists(tempDirectory))
				FileHelper.Safe(() => Directory.Delete(tempDirectory, true));
		}

		private string ExtractFileFromAttachment(string filename, string archFileName, string externalFileName)
		{
			using (var fs = new FileStream(filename, FileMode.Open,
				FileAccess.Read, FileShare.Read)) {
				var logger = LogManager.GetLogger(GetType());
				try {
					var message = Mime.Parse(fs);
					message = UueHelper.ExtractFromUue(message, Path.GetTempPath());

					var attachments = message.GetValidAttachements();
					foreach (var entity in attachments) {
						var attachmentFilename = entity.GetFilename();

						if (!FileHelper.CheckMask(attachmentFilename, archFileName) &&
							!FileHelper.CheckMask(attachmentFilename, externalFileName))
							continue;
						attachmentFilename = Path.Combine(Path.GetTempPath(), attachmentFilename);
						entity.DataToFile(attachmentFilename);
						return attachmentFilename;
					}
				}
				catch (Exception ex) {
					logger.ErrorFormat(
						"Возникла ошибка при попытке перепровести прайс. Не удалось обработать файл {0}. Файл должен быть письмом (*.eml)\n{1}",
						filename, ex);
					throw;
				}
			}
			throw new Exception(String.Format("В архиве '{0}' не удалось найти архив '{1}' или файл прайс листа {2}", filename, archFileName, externalFileName));
		}

		public void RetransPriceSmart(uint priceId)
		{
			With.Connection(c => {
				var price = Price.Find(priceId);
				if (price.ParentSynonym != null)
					priceId = price.ParentSynonym.Value;

				var adapter = new MySqlDataAdapter(@"
select
  pc.PriceItemId,
  pf.FileExtention
from
  usersettings.pricesdata pd,
  Customers.Suppliers s,
  usersettings.pricescosts pc,
  usersettings.priceitems pim,
  farm.formrules fr,
  farm.pricefmts pf
where
	(pd.PriceCode = ?priceId or pd.ParentSynonym = ?priceId)
and pd.AgencyEnabled = 1
and s.Id = pd.FirmCode
and s.Disabled = 0
and pc.PriceCode = pd.PriceCode
and (pd.CostType = 1 or pc.BaseCost = 1)
and pim.Id = pc.PriceItemId
and exists(
  select * from Farm.UnrecExp ue
  where ue.PriceItemId = pim.Id
)
and fr.Id = pim.FormRuleId
and pf.Id = fr.PriceFormatId", c);
				adapter.SelectCommand.Parameters.AddWithValue("?priceId", priceId);
				var data = new DataTable();
				adapter.Fill(data);
				foreach (var row in data.Rows.Cast<DataRow>()) {
					try {
						RetransPrice(row, Settings.Default.BasePath);
					}
					catch (Exception e) {
						log.Warn(String.Format("Ошибка при перепроведении прайс листа, priceItemId = {0}", row["PriceItemId"]), e);
					}
				}
			});
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
select pim.Id as PriceItemId, p.FileExtention
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
where pim.Id = ?PriceItemId", new MySqlParameter("?PriceItemId", priceItemId));

			RetransPrice(row, sourceDir);
		}

		private void RetransPrice(DataRow row, string sourceDir)
		{
			var priceItemId = Convert.ToUInt64(row["PriceItemId"]);
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
			return new HistoryFile {
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
			using (var fileStream = File.Create(newFile)) {
				file.CopyTo(fileStream);
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
				try {
					File.Delete(newBaseFile);
				}
				catch (Exception) {
					string errorMessage = String.Format("Невозможно удалить из Base старый файл {0}!",
						Path.GetFileName(newBaseFile));
					throw new FaultException<string>(errorMessage, new FaultReason(errorMessage));
				}
			using (var fileStream = File.Create(newBaseFile)) {
				file.CopyTo(fileStream);
			}
		}

		private static void SearchAndDeleteFilesFromDirectory(string directoryName, string fileNamePattern)
		{
			var directoryPath = Path.GetFullPath(directoryName);
			var files = Directory.GetFiles(directoryPath, fileNamePattern);
			foreach (var fileName in files) {
				try {
					File.Delete(fileName);
				}
				catch (Exception) {
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

			using (var connectionLog = new MySqlConnection(Literals.ConnectionString())) {
				try {
					connectionLog.Open();
					var commandLog = new MySqlCommand(query, connectionLog);
					var id = Convert.ToUInt64(commandLog.ExecuteScalar());
					return id;
				}
				catch (Exception ex) {
					log.Error("Ошибка логирования при перепосылке прайс-листа", ex);
					return 0;
				}
			}
		}

		public string[] FindSynonyms(uint priceItemId)
		{
			long taskId = 0;
			try {
				log.Debug(String.Format("Попытка запуска поиска синонимов для, priceItemId = {0}", priceItemId));
				PriceProcessItem item = PriceProcessItem.GetProcessItem(priceItemId);
				if (item == null) {
					string er = String.Format("Файл прайс-листа не найден (priceItemId = {0})", priceItemId);
					throw new FaultException<string>(er, new FaultReason(er));
				}
				var names = item.GetAllNames();
				if (names == null) {
					string er = String.Format("Прайс-лист не формализован (priceItemId = {0})", priceItemId);
					throw new FaultException<string>(er, new FaultReason(er));
				}
				if (names.Count == 0) {
					string er = String.Format("Не найдено ни одной позиции в прайс-листе (priceItemId = {0})", priceItemId);
					throw new FaultException<string>(er, new FaultReason(er));
				}
				// создаем задачу
				IndexerHandler handler = (IndexerHandler)Monitor.GetInstance().GetHandler(typeof(IndexerHandler));
				taskId = handler.AddTask(names, (uint)item.PriceCode);
			}
			catch (Exception e) {
				log.Warn("Ошибка в функции FindSynonyms:", e);
				throw;
			}
			return new[] { "Success", taskId.ToString() }; // задача успешно создана
		}

		public string[] FindSynonymsResult(string taskId)
		{
			IndexerHandler handler = (IndexerHandler)Monitor.GetInstance().GetHandler(typeof(IndexerHandler));
			SynonymTask task = handler.GetTask(Convert.ToInt64(taskId));
			if (task == null)
				return new[] { "Error", String.Format("Задача {0} не найдена", taskId) };
			if (task.State == TaskState.Error)
				return new[] { "Error", task.Error };
			if (task.State == TaskState.Success)
				return IndexerHandler.TransformToStringArray(task.Matches);
			if (task.State == TaskState.Running)
				return new[] { "Running", task.Rate.ToString() };
			if (task.State == TaskState.Canceled)
				return new[] { "Canceled", task.Rate.ToString() };
			return new[] { task.State.ToString() };
		}

		public void StopFindSynonyms(string taskId)
		{
			IndexerHandler handler = (IndexerHandler)Monitor.GetInstance().GetHandler(typeof(IndexerHandler));
			SynonymTask task = handler.GetTask(Convert.ToInt64(taskId));
			if (task != null) task.Stop();
		}

		public void AppendToIndex(string[] synonymsIds)
		{
			IndexerHandler handler = (IndexerHandler)Monitor.GetInstance().GetHandler(typeof(IndexerHandler));
			IList<int> ids = new List<int>();
			foreach (var sid in synonymsIds) {
				int val;
				if (Int32.TryParse(sid, out val))
					ids.Add(val);
			}
			handler.AppendToIndex(ids);
		}
	}
}
