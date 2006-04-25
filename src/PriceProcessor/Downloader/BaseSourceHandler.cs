using System;
using System.Threading;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;
using Inforoom.Downloader.Properties;
using System.IO;
using System.Data;
using System.Net.Mail;

namespace Inforoom.Downloader
{
    //Класс содержит название полей из таблицы Sources
    public sealed class SourcesTable
    {
        public static string colFirmCode = "FirmCode";
        public static string colPriceCode = "PriceCode";
        public static string colShortName = "ShortName";
        public static string colPriceName = "PriceName";
        public static string colRegionName = "RegionName";

        public static string colDateCurPrice = "DateCurPrice";
        public static string colDatePrevPrice = "DatePrevPrice";

        public static string colPriceFMT = "PriceFMT";

        public static string colSourceType = "SourceType";
        public static string colLastDateTime = "LastDateTime";
        public static string colPriceDateTime = "PriceDateTime";

        public static string colPricePath = "PricePath";

        public static string colEMailPassword = "EMailPassword";
        public static string colEMailTo = "EMailTo";
        public static string colEMailFrom = "EMailFrom";

        public static string colFTPDir = "FTPDir";
        public static string colFTPLogin = "FTPLogin";
        public static string colFTPPassword = "FTPPassword";

        public static string colHTTPLogin = "HTTPLogin";
        public static string colHTTPPassword = "HTTPPassword";

        public static string colPriceMask = "PriceMask";
        public static string colExtrMask = "ExtrMask";
    }
	/// <summary>
	/// Summary description for BaseSourceHandle.
	/// </summary>
	public abstract class BaseSourceHandler
	{
		/// <summary>
        /// Ссылка на рабочую нитку
		/// </summary>
		protected Thread tWork;

		/// <summary>
        /// Время "застоя" нитки
		/// </summary>
		protected int SleepTime;
		/// <summary>
        /// Время последнего "касания" обработчика
		/// </summary>
		protected DateTime lastPing;

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
        protected int CurrPriceCode;
        /// <summary>
        /// текущая временная директория (+ CurrPriceCode)
        /// </summary>
        protected string CurrTempPath;
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
        /// текущая дата файла
        /// </summary>
        protected DateTime CurrPriceDate;

        public BaseSourceHandler(string sourceType)
		{
            this.sourceType = sourceType;
            DownHandlerPath = Path.GetFullPath(Settings.Default.TempPath) + Path.DirectorySeparatorChar + "Down" + this.sourceType;
            if (!Directory.Exists(DownHandlerPath))
                Directory.CreateDirectory(DownHandlerPath);
            DownHandlerPath += Path.DirectorySeparatorChar;
			tWork = new Thread(new ThreadStart(ThreadWork));
            SleepTime = Settings.Default.RequestInterval;
		}

		//Работает ли?
		public bool Worked
		{
			get
			{
				return DateTime.Now.Subtract(lastPing).TotalMinutes < Settings.Default.Timeout;
			}
		}

		//Запуск обработчика
		public void StartWork()
		{
            Ping();
            CreateLogConnection();
            CreateWorkConnection();
            tWork.Start();
        }

		public void StopWork()
		{
			tWork.Abort();
			if (cLog.State == System.Data.ConnectionState.Open)
				try{ cLog.Close(); } catch{}
            if (cWork.State == System.Data.ConnectionState.Open)
                try { cWork.Close(); }
                catch { }
        }

		protected void Ping()
		{
			lastPing = DateTime.Now;
		}

		//Перезапуск обработчика
		public void RestartWork()
		{

            FormLog.Log(this.GetType().Name, "Перезапуск нитки");
            try
            {
                StopWork();
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                FormLog.Log(this.GetType().Name, "Ошибка при останове нитки : {0}", ex);
            }
			tWork = new Thread(new ThreadStart(ThreadWork));
            try
            {
                StartWork();
            }
            catch (Exception ex)
            {
                FormLog.Log(this.GetType().Name, "Ошибка при запуске нитки : {0}", ex);
            }
			FormLog.Log( this.GetType().Name, "Перезапустили нитку");
		}

		//Нитка, в которой осуществляется работа обработчика источника
		protected void ThreadWork()
		{
			while (true)
			{
                try
                {
                    ProcessData();
                }
                catch (ThreadAbortException)
                { }
                catch (Exception ex)
                {
                    FormLog.Log(this.GetType().Name, "Ошибка в нитке : {0}", ex);
                }
                Ping();
                Sleeping();
			}
		}

		protected void Sleeping()
		{
			Thread.Sleep(SleepTime * 1000);
		}

		//Метод для обработки данных для каждого источника - свой
		protected abstract void ProcessData();

        //Типа источника
        protected string SourceType
        { 
            get
            {
                return this.sourceType;
            }
        }

        protected string GetSQLSources()
        {
            return String.Format(@"
SELECT
  cd.FirmCode,
  pd.PriceCode,
  cd.ShortName,
  pd.PriceName,
  r.Region as RegionName,
  fr.DateCurPrice,
  fr.DatePrevPrice,
  fr.PriceFMT,
  st.SourceType, st.LastDateTime, st.PriceDateTime, 
  st.PricePath,  
  st.EMailPassword, st.EMailTo, st.EMailFrom, 
  st.FTPDir, st.FTPLogin, st.FTPPassword, 
  st.HTTPLogin, st.HTTPPassword,
  st.PriceMask, st.ExtrMask
FROM       {0}             as st
INNER JOIN UserSettings.PricesData  AS PD ON PD.PriceCode = st.FirmCode
INNER JOIN {1}           AS fr ON fr.FirmCode = st.FirmCode
INNER JOIN {2} AS CD ON CD.FirmCode = PD.FirmCode
inner join farm.regions             as r  on r.RegionCode = cd.RegionCode
WHERE
    st.SourceType = '{3}'
AND cd.FirmStatus   = 1
AND pd.AgencyEnabled= 1", 
                Settings.Default.tbSources,
                Settings.Default.tbFormRules,
                Settings.Default.tbClientsData,
                SourceType);
        }

        protected void CreateWorkConnection()
        {
            cWork = new MySqlConnection(
                String.Format("server={0};username={1}; password={2}; database={3}; pooling=false; allow zero datetime=true;",
                    Settings.Default.DBServerName,
                    Settings.Default.DBUserName,
                    Settings.Default.DBPass,
                    Settings.Default.DatabaseName)
            );
            try
            {
                cWork.Open();
                cmdUpdatePriceDate = new MySqlCommand(
                        String.Format(
                            "UPDATE {0} SET LastDateTime = now(), PriceDateTime = ?DT WHERE FirmCode = ?FirmCode;" +
                            "UPDATE {1} SET DatePrevPrice = DateCurPrice AND DateCurPrice = now() WHERE FirmCode = ?FirmCode", 
                            Settings.Default.tbSources,
                            Settings.Default.tbFormRules), 
                        cWork);
                cmdUpdatePriceDate.Parameters.Add("FirmCode", MySqlDbType.Int64);
                cmdUpdatePriceDate.Parameters.Add("DT", MySqlDbType.Datetime);
                cmdFillSources = new MySqlCommand(GetSQLSources(), cWork);
                daFillSources = new MySqlDataAdapter(cmdFillSources);
            }
            catch (Exception ex)
            {
                FormLog.Log(this.GetType().Name + ".CreateWorkConnection", "{0}", ex);
            }
        }

        protected void FillSourcesTable()
        {
            dtSources.Clear();
            try
            {
                daFillSources.Fill(dtSources);
            }
            catch (Exception ex)
            {
                FormLog.Log(this.GetType().Name + ".FillSourcesTable", ex.ToString());
            }
        }

        protected void OperatorMailSend()
        {
            MailMessage mm = new MailMessage(Settings.Default.SMTPUserError, Settings.Default.SMTPUserCopy,
                String.Format("{0}; {1} ({2})", CurrPriceCode, drCurrent[SourcesTable.colRegionName], SourceType),
                String.Format("Код прайса : {0}\nФирма: {1}; {2}\n{3}\nДата: {4}",
                    CurrPriceCode, drCurrent[SourcesTable.colShortName], drCurrent[SourcesTable.colRegionName], "", DateTime.Now));
            if (!String.IsNullOrEmpty(CurrFileName))
                mm.Attachments.Add(new Attachment(CurrFileName));
//#if DEBUG
//                mm.Body += Environment.NewLine + Environment.NewLine + CurrFileName;
//#else
//                mm.Attachments.Add(new Attachment(CurrFileName));
//#endif
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

            cmdUpdatePriceDate.Parameters["FirmCode"].Value = UpdatePriceCode;
            cmdUpdatePriceDate.Parameters["DT"].Value = UpDT;
            cmdUpdatePriceDate.ExecuteNonQuery();
        }

        protected void SetCurrentPriceCode(DataRow dr)
        {
            drCurrent = dr;
            CurrPriceCode = Convert.ToInt32(dr[SourcesTable.colPriceCode]);
            CurrTempPath = Path.GetFullPath(Settings.Default.TempPath) + Path.DirectorySeparatorChar + CurrPriceCode.ToString();
            if (!Directory.Exists(CurrTempPath))
                Directory.CreateDirectory(CurrTempPath);
            CurrTempPath += Path.DirectorySeparatorChar;
        }

        protected void ExtractFromArhive(string ArchName, string TempDir)
        {
            if (Directory.Exists(TempDir))
                Directory.Delete(TempDir, true);
            Directory.CreateDirectory(TempDir);
            ArchiveHlp.Extract(ArchName, "*.*", TempDir + Path.DirectorySeparatorChar);
        }

        protected string FindFromArhive(string TempDir, string ExtrMask)
        {
            string[] ExtrFiles = Directory.GetFiles(TempDir + Path.DirectorySeparatorChar, ExtrMask, SearchOption.AllDirectories);
            if (ExtrFiles.Length > 0)
                return ExtrFiles[0];
            else
                return String.Empty;
        }

        protected string NormalizeDir(string InputDir)
        {
            return Path.GetFullPath(InputDir) + Path.DirectorySeparatorChar;
        }

        protected string GetExt()
        {
            string FMT = ((string)drCurrent[SourcesTable.colPriceFMT]).ToUpper();
            if ((FMT == "WIN") || (FMT == "DOS"))
                return ".txt";
            else
                if (FMT == "XLS")
                    return ".xls";
                else
                    if (FMT == "DBF")
                        return ".dbf";
                    else
                        if (FMT == "DB")
                            return ".db";
                        else
                            return ".err";
        }

        protected bool ProcessPriceFile(string InFile)
        {
            string ExtrFile = InFile;
            if (ArchiveHlp.IsArchive(InFile))
            {
                ExtrFile = FindFromArhive(InFile + "Extr", (string)drCurrent[SourcesTable.colExtrMask]);
            }
            if (ExtrFile == String.Empty)
            {
                Logging(CurrPriceCode, "Cant find '" + (string)drCurrent[SourcesTable.colExtrMask] + "' file into archive");
                return false;
            }
            else
            {
                string NormalName = Path.GetFullPath(Settings.Default.InboundPath) + Path.DirectorySeparatorChar + CurrPriceCode.ToString() + GetExt();
                try
                {
                    if (File.Exists(NormalName))
                        File.Delete(NormalName);
                    File.Copy(ExtrFile, NormalName);
                    FormLog.Log(this.GetType().Name + CurrPriceCode.ToString(), "Price " + (string)drCurrent[SourcesTable.colShortName] + " - " + (string)drCurrent[SourcesTable.colPriceName] + " downloaded/decompressed");
                    return true;
                }
                catch(Exception ex)
                {
                    Logging(CurrPriceCode, String.Format("Cant move price {0} to {1} directory", ExtrFile, NormalName));
                    FormLog.Log(this.GetType().Name + CurrPriceCode.ToString(), "Cant move : " + ex.ToString());
                    return false;
                }
            }
        }

        #region Logging
        protected void CreateLogConnection()
		{
			cLog = new MySqlConnection(
				String.Format("server={0};username={1}; password={2}; database={3}; pooling=false",
					Settings.Default.DBServerName,
                    Settings.Default.DBUserName,
                    Settings.Default.DBPass,
                    Settings.Default.DatabaseName)
			);
			try
			{
				cLog.Open();
                cmdLog = new MySqlCommand(String.Format("insert into {0} (LogTime, AppCode, PriceCode, Addition) VALUES (now(), ?AppCode, ?PriceCode, ?Addition)", Settings.Default.tbLogs), cLog);
                cmdLog.Parameters.Add("AppCode", Settings.Default.AppCode);
                cmdLog.Parameters.Add("PriceCode", MySqlDbType.Int64);
                cmdLog.Parameters.Add("Addition", MySqlDbType.String);
			}
			catch(Exception ex)
			{
				FormLog.Log( this.GetType().Name + ".CreateLogConnection", "{0}", ex);
			}
		}

        protected void Logging(string Addition)
        {
            Logging(-1, Addition);
        }

        protected void Logging(int CurrPriceCode, string Addition)
        {
            if (CurrPriceCode > -1)
                FormLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "{0}", Addition);
            else
                FormLog.Log(this.GetType().Name, "{0}", Addition);
            try
            {
                if (cLog.State != System.Data.ConnectionState.Open)
                    cLog.Open();
                if (CurrPriceCode > -1)
                    cmdLog.Parameters["PriceCode"].Value = CurrPriceCode;
                else
                    cmdLog.Parameters["PriceCode"].Value = DBNull.Value;
                cmdLog.Parameters["Addition"].Value = Addition;
                cmdLog.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                FormLog.Log(this.GetType().Name, "Error on Logging : {0}", ex.ToString());
            }
        }

        protected void LoggingToService(string Addition)
        {
            FormLog.Log(this.GetType().Name + ".Error", Addition);
            try
            {
                MailMessage mm = new MailMessage("service@analit.net", "service@analit.net",
                    "Ошибка в Downloader", Addition);
                SmtpClient sc = new SmtpClient(Settings.Default.SMTPHost);
                sc.Send(mm);
            }
            catch
            {
            }
        }

        #endregion
    }
}
