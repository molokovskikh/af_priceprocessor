using System;
using Common.MySql;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor;
using log4net;
using MySql.Data.MySqlClient;
using System.IO;
using System.Data;
using System.Net.Mail;
using System.Collections.Generic;
using Inforoom.Common;
using FileHelper = Common.Tools.FileHelper;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.Downloader
{
	//Класс содержит название полей из таблицы Sources
	public sealed class SourcesTableColumns
	{
		public static string colFirmCode = "FirmCode";
		public static string colPriceCode = "PriceCode";
		public static string colCostCode = "CostCode";
		public static string colPriceItemId = "PriceItemId";
		public static string colShortName = "ShortName";
		public static string colPriceName = "PriceName";
		public static string colRegionName = "RegionName";
		public const string ParentSynonym = "ParentSynonym";
		public static string colFileExtention = "FileExtention";

		public static string colPriceDate = "PriceDate";

		//public static string colLastDateTime = "LastDateTime";
		//public static string colPriceDateTime = "PriceDateTime";

		public static string colPricePath = "PricePath";

		public static string colEMailTo = "EMailTo";
		public static string colEMailFrom = "EMailFrom";

		public static string colFTPDir = "FTPDir";
		public static string colFTPLogin = "FTPLogin";
		public static string colFTPPassword = "FTPPassword";
		public static string colFTPPassiveMode = "FTPPassiveMode";

		public static string colHTTPLogin = "HTTPLogin";
		public static string colHTTPPassword = "HTTPPassword";

		public static string colPriceMask = "PriceMask";
		public static string colExtrMask = "ExtrMask";
	}

	//Класс для хранения последней ошибки по каждому прайс-листу
	//Позволяет не логировать ошибки по два раза
	public static class ErrorPriceLogging
	{
		public static Dictionary<ulong, string> ErrorMessages = new Dictionary<ulong, string>();
	}

	//Перечисление с указанием кода результата обработки источника
	public enum DownPriceResultCode
	{
		SuccessDownload = 2,
		ErrorProcess = 3,
		ErrorDownload = 5
	}

	public abstract class BaseSourceHandler : AbstractHandler
	{
		/// <summary>
		/// Таблица с источниками
		/// </summary>
		public DataTable dtSources = new DataTable();

		/// <summary>
		/// Код текущего обрабатываемого прайса
		/// </summary>
		protected ulong CurrPriceCode;

		protected ulong? CurrCostCode;
		protected ulong CurrPriceItemId;
		protected ulong? CurrParentSynonym;

		/// <summary>
		/// текущая обрабатываема строка в таблице
		/// </summary>
		protected DataRow drCurrent;

		/// <summary>
		/// текущий скачанный файл (положен в директорию TempPath + 'Down' + SourceType)
		/// </summary>
		protected string CurrFileName;

		/// <summary>
		/// директория для сохранения файлов для истории
		/// </summary>
		protected string DownHistoryPath;

		/// <summary>
		/// текущая дата файла
		/// </summary>
		protected DateTime CurrPriceDate;

		public const string ExtrDirSuffix = "Extr";

		public BaseSourceHandler()
		{
			SleepTime = Settings.Default.HandlerRequestInterval;
		}

		//Запуск обработчика
		public override void StartWork()
		{
			CreateDirectoryPath();

			base.StartWork();
		}

		public override void HardStop()
		{
			base.HardStop();

			if (!Stoped) {
				if (!tWork.Join(JoinTimeout))
					_logger.ErrorFormat("Рабочая нитка не остановилась за {0} миллисекунд.", JoinTimeout);
			}
		}

		//Типа источника
		protected string SourceType { get; set; }

		protected virtual string GetSQLSources()
		{
			return GetSourcesCommand(SourceType);
		}

		public static string GetSourcesCommand(string type)
		{
			return String.Format(@"
SELECT distinct
  pi.Id as PriceItemId,
  s.Id as FirmCode,
  s.Name as ShortName,
  pd.PriceCode,
  pd.PriceName,
  pd.ParentSynonym,
  if(pd.CostType = 1, pc.CostCode, null) CostCode,
  r.Region as RegionName,
  pi.PriceDate,
  pf.Format,
  pf.FileExtention,
  st.SourceTypeId,
  st.PricePath,
  st.EMailTo, st.EMailFrom,
  st.FTPDir, st.FTPLogin, st.FTPPassword, st.FTPPassiveMode,
  st.ArchivePassword,
  st.HTTPLogin, st.HTTPPassword,
  st.PriceMask, st.ExtrMask,
  pi.LastDownload,
  st.RequestInterval,
  st.LastSuccessfulCheck
FROM
  farm.sourcetypes,
  farm.Sources as st,
  usersettings.PriceItems pi,
  usersettings.PricesCosts pc,
  UserSettings.PricesData  as PD,
  Customers.Suppliers as s,
  farm.regions             as r,
  farm.FormRules           AS fr,
  farm.pricefmts           AS pf
WHERE
	sourcetypes.Type = '{0}'
and st.SourceTypeId = sourcetypes.Id
and (length(st.PriceMask) > 0)
and pi.SourceId = st.ID
and pc.PriceItemId = pi.ID
and PD.PriceCode = pc.PriceCode
and ((pd.CostType = 1) or (exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pd.PriceCode and prd.BaseCost=pc.CostCode)))
and s.Id = PD.FirmCode
and r.RegionCode = s.HomeRegion
and fr.Id = pi.FormRuleId
and pf.Id = fr.PriceFormatId
and s.Disabled = 0
and pd.AgencyEnabled= 1",
				type);
		}

		public void CreateDirectoryPath()
		{
			CreateDownHandlerPath();

			DownHistoryPath = Settings.Default.HistoryPath;
			if (!Directory.Exists(DownHistoryPath))
				Directory.CreateDirectory(DownHistoryPath);
		}

		public void FillSourcesTable()
		{
			using (var connection = new MySqlConnection(ConnectionHelper.DefaultConnectionStringName)) {
				dtSources.Clear();
				connection.Open();
				var adapter = new MySqlDataAdapter(GetSQLSources(), connection);
				adapter.Fill(dtSources);
				if(SourceType == "WAYBILL") {
					if (dtSources.Rows == null) {
						_logger.Info("WaybillEmailSourceHandler: При загрузке источников получили таблицу с null на месте строк");
					}
					else if (dtSources.Rows.Count == 0) {
						_logger.Info("WaybillEmailSourceHandler: При загрузке источников получили пустую таблицу");
					}
				}
			}
		}

		protected static void ErrorMailSend(int UID, string ErrorMessage, Stream ms)
		{
			using (var mm = new MailMessage(
				Settings.Default.FarmSystemEmail,
				Settings.Default.SMTPErrorList,
				String.Format("Письмо с UID {0} не было обработано", UID),
				String.Format("UID : {0}\nОшибка : {1}", UID, ErrorMessage))) {
				if (ms != null)
					mm.Attachments.Add(new Attachment(ms, "Unparse.eml"));
				var sc = new SmtpClient(Settings.Default.SMTPHost);
				sc.Send(mm);
			}
		}

		protected virtual string GetFailMail()
		{
			return Settings.Default.SMTPUserFail;
		}

		protected void FailMailSend(string Subject, string FromAddress, string ToAddress, DateTime LetterDate, Stream ms, string AttachNames, string cause)
		{
#if !DEBUG
			ms.Position = 0;
			using (var mm = new MailMessage(
				Settings.Default.FarmSystemEmail,
				GetFailMail(),
				String.Format("{0} ( {1} )", FromAddress, SourceType),
				String.Format("Тема : {0}\nОт : {1}\nКому : {2}\nДата письма : {3}\nПричина : {4}\n\nСписок приложений :\n{5}",
					Subject,
					FromAddress,
					ToAddress,
					LetterDate,
					cause,
					AttachNames))) {
				mm.Attachments.Add(new Attachment(ms, "Исходное письмо.eml"));
				var sc = new SmtpClient(Settings.Default.SMTPHost);
				sc.Send(mm);
			}
#endif
		}

		protected void SetCurrentPriceCode(DataRow dr)
		{
			drCurrent = dr;
			CurrPriceCode = Convert.ToUInt64(dr[SourcesTableColumns.colPriceCode]);
			CurrCostCode = (dr[SourcesTableColumns.colCostCode] is DBNull) ? null : (ulong?)Convert.ToUInt64(dr[SourcesTableColumns.colPriceCode]);
			CurrPriceItemId = Convert.ToUInt64(dr[SourcesTableColumns.colPriceItemId]);
			CurrParentSynonym = (dr[SourcesTableColumns.ParentSynonym] is DBNull) ? null : (ulong?)Convert.ToUInt64(dr[SourcesTableColumns.ParentSynonym]);
		}

		protected bool ProcessPriceFile(string InFile, out string ExtrFile, ulong sourceTypeId)
		{
			ExtrFile = InFile;
			if (ArchiveHelper.IsArchive(InFile)) {
				if ((drCurrent[SourcesTableColumns.colExtrMask] is String) &&
					!String.IsNullOrEmpty(drCurrent[SourcesTableColumns.colExtrMask].ToString())) {
					ExtrFile = PriceProcessor.FileHelper.FindFromArhive(
						InFile + ExtrDirSuffix, (string)drCurrent[SourcesTableColumns.colExtrMask]);
				}
				else
					ExtrFile = String.Empty;
			}
			if (String.IsNullOrEmpty(ExtrFile)) {
				DownloadLogEntity.Log(sourceTypeId, CurrPriceItemId, String.Format(
					"Не удалось найти файл в архиве. Маска файла в архиве : '{0}'",
					drCurrent[SourcesTableColumns.colExtrMask]));
				return false;
			}
			return true;
		}
	}
}