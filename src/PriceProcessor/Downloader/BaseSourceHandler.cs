using System;
using System.Threading;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using System.IO;
using System.Data;
using System.Net.Mail;
using System.Collections.Generic;
using ExecuteTemplate;
using System.Configuration;
using Inforoom.Common;
using Inforoom.PriceProcessor;

namespace Inforoom.Downloader
{
    //Класс содержит название полей из таблицы Sources
    public sealed class SourcesTable
    {
        public static string colFirmCode = "FirmCode";
        public static string colPriceCode = "PriceCode";
		public static string colCostCode = "CostCode";
		public static string colPriceItemId = "PriceItemId";
		public static string colShortName = "ShortName";
        public static string colPriceName = "PriceName";
        public static string colRegionName = "RegionName";

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

	/// <summary>
	/// Summary description for BaseSourceHandle.
	/// </summary>
	public abstract class BaseSourceHandler : AbstractHandler
	{
		//Соединение для логирования
		protected MySqlConnection cLog;
        protected MySqlCommand cmdLog;

		//Соединение для работы
		protected MySqlConnection cWork;
        protected MySqlCommand cmdUpdatePriceDate;
        protected MySqlCommand cmdFillSources;
        protected MySqlDataAdapter daFillSources;

        /// <summary>
        /// Таблица с источниками
        /// </summary>
        protected DataTable dtSources = new DataTable();

        /// <summary>
        /// Тип источника, за который отвечает данный обработчик
        /// </summary>
        protected string sourceType;

        /// <summary>
        /// Код текущего обрабатываемого прайса
        /// </summary>
        protected ulong CurrPriceCode;
		protected ulong? CurrCostCode;
		protected ulong CurrPriceItemId;
        /// <summary>
        /// текущая обрабатываема строка в таблице
        /// </summary>
        protected DataRow drCurrent;
        /// <summary>
        /// текущий скачанный файл (положен в директорию TempPath + 'Down' + SourceType)
        /// </summary>
        protected string CurrFileName;
        /// <summary>
        /// временная директория для скачивания файлов (+ TempPath + 'Down' + SourceType)
        /// </summary>
        protected string DownHandlerPath;
		/// <summary>
		/// директория для сохранения файлов для истории 
		/// </summary>
		protected string DownHistoryPath;
		/// <summary>
        /// текущая дата файла
        /// </summary>
        protected DateTime CurrPriceDate;

        protected static string ExtrDirSuffix = "Extr";

        public BaseSourceHandler() :base()
		{
            SleepTime = Settings.Default.HandlerRequestInterval;
		}

		//Запуск обработчика
		public override void StartWork()
		{
            Ping();
			CreateDirectoryPath();
            CreateLogConnection();
            CreateWorkConnection();
            tWork.Start();
        }

		public override void StopWork()
		{
			tWork.Abort();
			if (cLog.State == System.Data.ConnectionState.Open)
				try{ cLog.Close(); } catch{}
            if (cWork.State == System.Data.ConnectionState.Open)
                try { cWork.Close(); }
                catch { }
        }

		protected override void Ping()
		{
			base.Ping();	
			try { cWork.Ping(); } catch{}
			try { cLog.Ping(); } catch { }
		}

        //Типа источника
        protected string SourceType
        { 
            get
            {
                return this.sourceType;
            }
        }

        protected virtual string GetSQLSources()
        {
            return String.Format(@"
SELECT
  pi.Id as PriceItemId,
  cd.FirmCode,
  cd.ShortName,
  pd.PriceCode,
  pd.PriceName,
  if(pd.CostType = 1, pc.CostCode, null) CostCode,
  r.Region as RegionName,
  pi.PriceDate,
  pf.Format,
  pf.FileExtention,
  st.SourceTypeId, 
  st.PricePath,  
  st.EMailTo, st.EMailFrom, 
  st.FTPDir, st.FTPLogin, st.FTPPassword, st.FTPPassiveMode,
  st.HTTPLogin, st.HTTPPassword,
  st.PriceMask, st.ExtrMask
FROM   
  farm.sourcetypes,
  farm.Sources as st,
  usersettings.PriceItems pi,
  usersettings.PricesCosts pc,
  UserSettings.PricesData  as PD,
  usersettings.ClientsData as CD,
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
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and CD.FirmCode = PD.FirmCode
and r.RegionCode = cd.RegionCode
and fr.Id = pi.FormRuleId
and pf.Id = fr.PriceFormatId
and cd.FirmStatus   = 1
and pd.AgencyEnabled= 1", 
                SourceType);
        }

		protected void CreateDirectoryPath()
		{
			DownHandlerPath = FileHelper.NormalizeDir(Settings.Default.TempPath) + "Down" + this.sourceType;
			if (!Directory.Exists(DownHandlerPath))
				Directory.CreateDirectory(DownHandlerPath);
			DownHandlerPath += Path.DirectorySeparatorChar;

			DownHistoryPath = FileHelper.NormalizeDir(Settings.Default.HistoryPath);
			if (!Directory.Exists(DownHistoryPath))
				Directory.CreateDirectory(DownHistoryPath);
		}

        protected void CreateWorkConnection()
        {
            cWork = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
            try
            {
                cWork.Open();
                cmdUpdatePriceDate = new MySqlCommand(
					"UPDATE farm.sources SET LastDateTime = ?NowDate, PriceDateTime = ?DT WHERE FirmCode = ?FirmCode;" +
					"UPDATE usersettings.price_update_info SET DatePrevPrice = DateCurPrice, DateCurPrice = ?NowDate WHERE PriceCode = ?FirmCode;", 
                     cWork);
                cmdUpdatePriceDate.Parameters.Add("?FirmCode", MySqlDbType.Int64);
                cmdUpdatePriceDate.Parameters.Add("?DT", MySqlDbType.DateTime);
				cmdUpdatePriceDate.Parameters.Add("?NowDate", MySqlDbType.DateTime);
				cmdFillSources = new MySqlCommand(GetSQLSources(), cWork);
                daFillSources = new MySqlDataAdapter(cmdFillSources);
            }
            catch (Exception ex)
            {
				_logger.Error("Ошибка на CreateWorkConnection", ex);
            }
        }

        protected void FillSourcesTable()
        {
			ConnectionState oldstate = cWork.State;
			try
			{
				dtSources = MethodTemplate.ExecuteMethod<ExecuteArgs, DataTable>(new ExecuteArgs(), GetSourcesTable, null, cWork, true, null, false,
					delegate(ExecuteArgs args, MySqlException ex)
					{
						Ping();
					});
			}
			catch
			{
				//Если здесь возникает ошибка, то мы пытаемся открыть соединение и сновы запрашивает таблицу с источниками
				if (cWork.State != ConnectionState.Closed)
					try { cWork.Close(); } catch { }
				cWork.Open();
				dtSources = MethodTemplate.ExecuteMethod<ExecuteArgs, DataTable>(new ExecuteArgs(), GetSourcesTable, null, cWork, true, null, false,
					delegate(ExecuteArgs args, MySqlException ex)
					{
						Ping();
					});
			}
		}

		protected virtual DataTable GetSourcesTable(ExecuteArgs e)
		{
			dtSources.Clear();
			daFillSources.SelectCommand.Transaction = e.DataAdapter.SelectCommand.Transaction;
			daFillSources.Fill(dtSources);
			return dtSources;
		}

        protected void ErrorMailSend(int UID, string ErrorMessage, Stream ms)
        {
			MailMessage mm = new MailMessage(Settings.Default.FarmSystemEmail, Settings.Default.SMTPErrorList,
                String.Format("Письмо с UID {0} не было обработано", UID),
                String.Format("UID : {0}\nОшибка : {1}", UID, ErrorMessage));
            if (ms != null)
                mm.Attachments.Add(new Attachment(ms, "Unparse.eml"));
            SmtpClient sc = new SmtpClient(Settings.Default.SMTPHost);
            sc.Send(mm);
        }

		protected virtual string GetFailMail()
		{
			return Settings.Default.SMTPUserFail;
		}

        protected void FailMailSend(string Subject, string FromAddress, string ToAddress, DateTime LetterDate, Stream ms, string AttachNames, string cause)
        {
			ms.Position = 0;
			MailMessage mm = new MailMessage(Settings.Default.FarmSystemEmail, GetFailMail(),
                String.Format("{0} ( {1} )", FromAddress, SourceType),
				String.Format("Тема : {0}\nОт : {1}\nКому : {2}\nДата письма : {3}\nПричина : {4}\n\nСписок приложений :\n{5}", 
                Subject, 
                FromAddress, 
                ToAddress, 
                LetterDate,
				cause,
                AttachNames));
            mm.Attachments.Add(new Attachment(ms, ((String.IsNullOrEmpty(Subject)) ? "Unrec" : Subject ) + ".eml"));
            SmtpClient sc = new SmtpClient(Settings.Default.SMTPHost);
            sc.Send(mm);
        }

        /// <summary>
        /// Обновить дату прайса в базе
        /// </summary>
        /// <param name="UpadatePriceCode"></param>
        /// <param name="UpDT"></param>
        protected void UpdateDB(int UpdatePriceCode, DateTime UpDT)
        {
            if (cWork.State != System.Data.ConnectionState.Open)
                cWork.Open();

            cmdUpdatePriceDate.Parameters["?FirmCode"].Value = UpdatePriceCode;
            cmdUpdatePriceDate.Parameters["?DT"].Value = UpDT;
			cmdUpdatePriceDate.Parameters["?NowDate"].Value = DateTime.Now;
			ExecuteCommand(cmdUpdatePriceDate);
        }

        protected void SetCurrentPriceCode(DataRow dr)
        {
            drCurrent = dr;
            CurrPriceCode = Convert.ToUInt64(dr[SourcesTable.colPriceCode]);
			CurrCostCode = (dr[SourcesTable.colCostCode] is DBNull) ? null : (ulong?)Convert.ToUInt64(dr[SourcesTable.colPriceCode]);
			CurrPriceItemId = Convert.ToUInt64(dr[SourcesTable.colPriceItemId]);
        }

        protected void ExtractFromArhive(string ArchName, string TempDir)
        {
            if (Directory.Exists(TempDir))
                Directory.Delete(TempDir, true);
            Directory.CreateDirectory(TempDir);
            ArchiveHelper.Extract(ArchName, "*.*", TempDir + Path.DirectorySeparatorChar);
        }

        protected string FindFromArhive(string TempDir, string ExtrMask)
        {
            string[] ExtrFiles = Directory.GetFiles(TempDir + Path.DirectorySeparatorChar, ExtrMask, SearchOption.AllDirectories);
            if (ExtrFiles.Length > 0)
                return ExtrFiles[0];
            else
                return String.Empty;
        }

        protected string NormalizeFileName(string InputFilename)
        {
            string PathPart = String.Empty;
            foreach (Char ic in Path.GetInvalidPathChars())
            {
                InputFilename = InputFilename.Replace(ic.ToString(), "");
            }
            //Пытаемся найти последний разделитель директории в пути
            int EndDirPos = InputFilename.LastIndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            if (EndDirPos > -1)
            {
                PathPart = InputFilename.Substring(0, EndDirPos + 1);
                InputFilename = InputFilename.Substring(EndDirPos + 1);
            }
            foreach (Char ic in Path.GetInvalidFileNameChars())
            {
                InputFilename = InputFilename.Replace(ic.ToString(), "");
            }
            return (PathPart + InputFilename);
        }

        protected string GetExt()
        {
			string FileExt = drCurrent[SourcesTable.colFileExtention].ToString();
			if (String.IsNullOrEmpty(FileExt))
				FileExt = ".err";
			return FileExt;
        }

		protected string GetSuccessAddition(string ArchName, string FileName)
		{
			return String.Format("{0} > {1}", Path.GetFileName(ArchName), Path.GetFileName(FileName));
		}

        protected bool ProcessPriceFile(string InFile, out string ExtrFile)
        {
            ExtrFile = InFile;
            if (ArchiveHelper.IsArchive(InFile))
            {
				if ((drCurrent[SourcesTable.colExtrMask] is String) && !String.IsNullOrEmpty(drCurrent[SourcesTable.colExtrMask].ToString()))
					ExtrFile = FindFromArhive(InFile + ExtrDirSuffix, (string)drCurrent[SourcesTable.colExtrMask]);
				else
					ExtrFile = String.Empty;
            }
			if (String.IsNullOrEmpty(ExtrFile))
            {
				Logging(CurrPriceItemId, String.Format("Не удалось найти файл в архиве. Маска файла в архиве : '{0}'", drCurrent[SourcesTable.colExtrMask]));
                return false;
            }
            else
				return true;
        }

        protected void DeleteCurrFile()
        {
            try
            {
                if (Directory.Exists(CurrFileName + ExtrDirSuffix))
                    Directory.Delete(CurrFileName + ExtrDirSuffix, true);
            }
            catch { }
            try
            {

				if (_logger.IsDebugEnabled)
					using (log4net.NDC.Push("." + CurrPriceItemId.ToString()))
						_logger.DebugFormat("Попытка удалить файл : {0}", CurrFileName);
				if (File.Exists(CurrFileName))
					File.Delete(CurrFileName);
				if (_logger.IsDebugEnabled)
					using (log4net.NDC.Push("." + CurrPriceItemId.ToString()))
						_logger.DebugFormat("Файл удален : {0}", CurrFileName);
			}
            catch (Exception ex)
			{
				using (log4net.NDC.Push("." + CurrPriceItemId.ToString()))
					_logger.ErrorFormat("Ошибка при удалении файла {0}:\r\n{1}", CurrFileName, ex);
			}
        }

        protected void ExecuteCommand(MySqlCommand cmd)
        {
			MethodTemplate.ExecuteMethod<ExecuteArgs, object>(
				new ExecuteArgs(),
				delegate(ExecuteArgs args)
				{
					cmd.ExecuteNonQuery();
					return null;
				},
				null,
				cmd.Connection,
				true,
				null,
				false,
				delegate(ExecuteArgs args, MySqlException ex)
				{
					Ping();
				});
        }

		protected UInt64 ExecuteScalar(MySqlCommand cmd)
		{
			return MethodTemplate.ExecuteMethod<ExecuteArgs, UInt64>(
				new ExecuteArgs(), 
				delegate(ExecuteArgs args)
				{
					return Convert.ToUInt64(cmd.ExecuteScalar());
				}, 
				0, 
				cmd.Connection, 
				true, 
				null, 
				false, 
				delegate(ExecuteArgs args, MySqlException ex)
				{
					Ping();
				});
		}

        #region Logging
        protected void CreateLogConnection()
		{
			cLog = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
			try
			{
				cLog.Open();
				cmdLog = new MySqlCommand("insert into logs.downlogs (LogTime, Host, PriceItemId, Addition, ResultCode, ArchFileName, ExtrFileName) VALUES (now(), ?Host, ?PriceItemId, ?Addition, ?ResultCode, ?ArchFileName, ?ExtrFileName); select last_insert_id()", cLog);
				cmdLog.Parameters.AddWithValue("?Host", Environment.MachineName);
				cmdLog.Parameters.Add("?PriceItemId", MySqlDbType.UInt64);
                cmdLog.Parameters.Add("?Addition", MySqlDbType.VarString);
				cmdLog.Parameters.Add("?ResultCode", MySqlDbType.Byte);
				cmdLog.Parameters.Add("?ArchFileName", MySqlDbType.VarString);
				cmdLog.Parameters.Add("?ExtrFileName", MySqlDbType.VarString);
			}
			catch(Exception ex)
			{
				_logger.Error("Ошибка на CreateLogConnection", ex);
			}
		}

        protected void Logging(string Addition)
        {
            Logging(null, Addition, DownPriceResultCode.ErrorDownload, null, null);
        }

		protected void Logging(ulong? CurrPriceItemId, string Addition)
		{
			Logging(CurrPriceItemId, Addition, DownPriceResultCode.ErrorDownload, null, null);
		}

		protected UInt64 Logging(ulong? CurrPriceItemId, string Addition, DownPriceResultCode resultCode, string ArchFileName, string ExtrFileName)
        {
			if (CurrPriceItemId.HasValue)
				using(log4net.NDC.Push("." + CurrPriceItemId.ToString()))
					_logger.InfoFormat("Logging.Addition : {0}", Addition);
			else
				_logger.InfoFormat("Logging.Addition : {0}", Addition);

            if (cLog.State != System.Data.ConnectionState.Open)
                cLog.Open();
			cmdLog.Parameters["?PriceItemId"].Value = CurrPriceItemId;
            cmdLog.Parameters["?Addition"].Value = Addition;
			cmdLog.Parameters["?ResultCode"].Value = Convert.ToByte(resultCode);
			cmdLog.Parameters["?ArchFileName"].Value = ArchFileName;
			cmdLog.Parameters["?ExtrFileName"].Value = ExtrFileName;

			bool NeedLogging = true;
			//Если это успешная загрузка, то сбрасываем все ошибки
			//Если это ошибка, то если дополнение в словаре и совпадает, то запрещаем логирование, в другом случае добавляем или обновляем
			ulong tmpCurrPriceItemId = (CurrPriceItemId.HasValue) ? CurrPriceItemId.Value : 0;
			if (resultCode == DownPriceResultCode.ErrorDownload)
			{
				if (ErrorPriceLogging.ErrorMessages.ContainsKey(tmpCurrPriceItemId))
				{
					if (ErrorPriceLogging.ErrorMessages[tmpCurrPriceItemId] == Addition)
						NeedLogging = false;
					else
						ErrorPriceLogging.ErrorMessages[tmpCurrPriceItemId] = Addition;
				}
				else
					ErrorPriceLogging.ErrorMessages.Add(tmpCurrPriceItemId, Addition);
			}
			else
			{
				if (ErrorPriceLogging.ErrorMessages.ContainsKey(tmpCurrPriceItemId))
					ErrorPriceLogging.ErrorMessages.Remove(tmpCurrPriceItemId);
			}

			if (NeedLogging)
				return ExecuteScalar(cmdLog);
			else
				return 0;
        }

        #endregion
    }
}
