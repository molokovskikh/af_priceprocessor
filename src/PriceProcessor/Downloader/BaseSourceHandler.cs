using System;
using System.Threading;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;
using Inforoom.Downloader.Properties;
using System.IO;
using System.Data;
using System.Net.Mail;
using System.Collections.Generic;
using ExecuteTemplate;

namespace Inforoom.Downloader
{
    //����� �������� �������� ����� �� ������� Sources
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
		public static string colFileExtention = "FileExtention";

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
		public static string colFTPPassiveMode = "FTPPassiveMode";

        public static string colHTTPLogin = "HTTPLogin";
        public static string colHTTPPassword = "HTTPPassword";

        public static string colPriceMask = "PriceMask";
        public static string colExtrMask = "ExtrMask";
    }

	//����� ��� �������� ��������� ������ �� ������� �����-�����
	//��������� �� ���������� ������ �� ��� ����
	public static class ErrorPriceLogging
	{
		public static Dictionary<int, string> ErrorMessages = new Dictionary<int, string>();
	}

	/// <summary>
	/// Summary description for BaseSourceHandle.
	/// </summary>
	public abstract class BaseSourceHandler
	{
		/// <summary>
        /// ������ �� ������� �����
		/// </summary>
		protected Thread tWork;

		/// <summary>
        /// ����� "������" �����
		/// </summary>
		protected int SleepTime;
		/// <summary>
        /// ����� ���������� "�������" �����������
		/// </summary>
		protected DateTime lastPing;

		//���������� ��� �����������
		protected MySqlConnection cLog;
        protected MySqlCommand cmdLog;

		//���������� ��� ������
		protected MySqlConnection cWork;
        protected MySqlCommand cmdUpdatePriceDate;
        protected MySqlCommand cmdFillSources;
        protected MySqlDataAdapter daFillSources;

        /// <summary>
        /// ������� � �����������
        /// </summary>
        protected DataTable dtSources = new DataTable();

        /// <summary>
        /// ��� ���������, �� ������� �������� ������ ����������
        /// </summary>
        protected string sourceType;

        /// <summary>
        /// ��� �������� ��������������� ������
        /// </summary>
        protected int CurrPriceCode;
        /// <summary>
        /// ������� ������������� ������ � �������
        /// </summary>
        protected DataRow drCurrent;
        /// <summary>
        /// ������� ��������� ���� (������� � ���������� TempPath + 'Down' + SourceType)
        /// </summary>
        protected string CurrFileName;
        /// <summary>
        /// ��������� ���������� ��� ���������� ������ (+ TempPath + 'Down' + SourceType)
        /// </summary>
        protected string DownHandlerPath;
		/// <summary>
		/// ���������� ��� ���������� ������ ��� ������� 
		/// </summary>
		protected string DownHistoryPath;
		/// <summary>
        /// ������� ���� �����
        /// </summary>
        protected DateTime CurrPriceDate;

        protected static string ExtrDirSuffix = "Extr";

		//��������� ������, ������� �� ���� ��������� ��� ����������
		protected List<string> knowErrors;

        public BaseSourceHandler()
		{
			knowErrors = new List<string>();
            DownHandlerPath = Path.GetFullPath(Settings.Default.TempPath) + Path.DirectorySeparatorChar + "Down" + this.sourceType;
            if (!Directory.Exists(DownHandlerPath))
                Directory.CreateDirectory(DownHandlerPath);
            DownHandlerPath += Path.DirectorySeparatorChar;

			DownHistoryPath = Path.GetFullPath(Settings.Default.HistoryPath);
			if (!Directory.Exists(DownHistoryPath))
				Directory.CreateDirectory(DownHistoryPath);
			DownHistoryPath += Path.DirectorySeparatorChar;

			tWork = new Thread(new ThreadStart(ThreadWork));
            SleepTime = Settings.Default.RequestInterval;
		}

		//�������� ��?
		public bool Worked
		{
			get
			{
				return DateTime.Now.Subtract(lastPing).TotalMinutes < Settings.Default.Timeout;
			}
		}

		//������ �����������
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
			try { cWork.Ping(); } catch{}
			try { cLog.Ping(); } catch { }
		}

		//���������� �����������
		public void RestartWork()
		{

            FormLog.Log(this.GetType().Name, "���������� �����");
            try
            {
                StopWork();
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                FormLog.Log(this.GetType().Name, "������ ��� �������� ����� : {0}", ex);
            }
			tWork = new Thread(new ThreadStart(ThreadWork));
            try
            {
                StartWork();
            }
            catch (Exception ex)
            {
                FormLog.Log(this.GetType().Name, "������ ��� ������� ����� : {0}", ex);
            }
			FormLog.Log( this.GetType().Name, "������������� �����");
		}

		//�����, � ������� �������������� ������ ����������� ���������
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
                    FormLog.Log(this.GetType().Name, "������ � ����� : {0}", ex);
                }
                Ping();
                Sleeping();
			}
		}

		protected void Sleeping()
		{
			Thread.Sleep(SleepTime * 1000);
		}

		//����� ��� ��������� ������ ��� ������� ��������� - ����
		protected abstract void ProcessData();

        //���� ���������
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
  cd.FirmCode,
  pd.PriceCode,
  cd.ShortName,
  pd.PriceName,
  r.Region as RegionName,
  fr.DateCurPrice,
  fr.DatePrevPrice,
  fr.PriceFMT,
  pf.FileExtention,
  st.SourceType, st.LastDateTime, st.PriceDateTime, 
  st.PricePath,  
  st.EMailPassword, st.EMailTo, st.EMailFrom, 
  st.FTPDir, st.FTPLogin, st.FTPPassword, st.FTPPassiveMode,
  st.HTTPLogin, st.HTTPPassword,
  st.PriceMask, st.ExtrMask
FROM       {0}             as st
INNER JOIN UserSettings.PricesData  AS PD ON PD.PriceCode = st.FirmCode
INNER JOIN {1}           AS fr ON fr.FirmCode = st.FirmCode
INNER JOIN farm.pricefmts           AS pf ON pf.Format = fr.PriceFMT
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
                            "UPDATE {0} SET LastDateTime = ?NowDate, PriceDateTime = ?DT WHERE FirmCode = ?FirmCode;" +
							"UPDATE {1} SET DatePrevPrice = DateCurPrice, DateCurPrice = ?NowDate WHERE FirmCode = ?FirmCode;" +
							"UPDATE usersettings.price_update_info SET DatePrevPrice = DateCurPrice, DateCurPrice = ?NowDate WHERE PriceCode = ?FirmCode;", 
                            Settings.Default.tbSources,
                            Settings.Default.tbFormRules), 
                        cWork);
                cmdUpdatePriceDate.Parameters.Add("?FirmCode", MySqlDbType.Int64);
                cmdUpdatePriceDate.Parameters.Add("?DT", MySqlDbType.Datetime);
				cmdUpdatePriceDate.Parameters.Add("?NowDate", MySqlDbType.Datetime);
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
				//���� ����� ��������� ������, �� �� �������� ������� ���������� � ����� ����������� ������� � �����������
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

        protected void OperatorMailSend()
        {
            MailMessage mm = new MailMessage(Settings.Default.SMTPUserError, Settings.Default.SMTPUserCopy,
                String.Format("{0}; {1} ({2})", CurrPriceCode, drCurrent[SourcesTable.colRegionName], SourceType),
                String.Format("��� ������ : {0}\n�����: {1}; {2}\n{3}\n����: {4}",
                    CurrPriceCode, drCurrent[SourcesTable.colShortName], drCurrent[SourcesTable.colRegionName], "", DateTime.Now));
            if (!String.IsNullOrEmpty(CurrFileName))
                mm.Attachments.Add(new Attachment(CurrFileName));
            SmtpClient sc = new SmtpClient(Settings.Default.SMTPHost);
            sc.Send(mm);
        }

        protected void ErrorMailSend(int UID, string ErrorMessage, Stream ms)
        {
            MailMessage mm = new MailMessage(Settings.Default.SMTPUserError, Settings.Default.SMTPErrorList,
                String.Format("������ � UID {0} �� ���� ����������", UID),
                String.Format("UID : {0}\n������ : {1}", UID, ErrorMessage));
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
			MailMessage mm = new MailMessage(Settings.Default.SMTPUserError, GetFailMail(),
                String.Format("{0} ( {1} )", FromAddress, SourceType),
				String.Format("���� : {0}\n�� : {1}\n���� : {2}\n���� ������ : {3}\n������� : {4}\n\n������ ���������� :\n{5}", 
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
        /// �������� ���� ������ � ����
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
            CurrPriceCode = Convert.ToInt32(dr[SourcesTable.colPriceCode]);
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

        protected string NormalizeFileName(string InputFilename)
        {
            string PathPart = String.Empty;
            foreach (Char ic in Path.GetInvalidPathChars())
            {
                InputFilename = InputFilename.Replace(ic.ToString(), "");
            }
            //�������� ����� ��������� ����������� ���������� � ����
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

        protected bool ProcessPriceFile(string InFile)
        {
            string ExtrFile = InFile;
            if (ArchiveHlp.IsArchive(InFile))
            {
                ExtrFile = FindFromArhive(InFile + ExtrDirSuffix, (string)drCurrent[SourcesTable.colExtrMask]);
            }
            if (ExtrFile == String.Empty)
            {
                Logging(CurrPriceCode, "�� ������� ����� ���� '" + (string)drCurrent[SourcesTable.colExtrMask] + "' � ������");
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
                    FormLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "Price " + (string)drCurrent[SourcesTable.colShortName] + " - " + (string)drCurrent[SourcesTable.colPriceName] + " downloaded/decompressed");
                    return true;
                }
                catch(Exception ex)
                {
                    Logging(CurrPriceCode, String.Format("�� ������� ��������� ���� '{0}' � ������� '{1}'", ExtrFile, NormalName));
                    FormLog.Log(this.GetType().Name + CurrPriceCode.ToString(), "Cant move : " + ex.ToString());
                    return false;
                }
            }
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

				FormLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "������� ������� ���� : " + CurrFileName);
				if (File.Exists(CurrFileName))
					File.Delete(CurrFileName);
				FormLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "���� ������ : " + CurrFileName);
			}
            catch (Exception ex)
			{
				FormLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "������ ��� �������� ����� : " + CurrFileName + "  ������ : " + ex.ToString());
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
                cmdLog = new MySqlCommand(String.Format("insert into {0} (LogTime, AppCode, PriceCode, Addition) VALUES (now(), ?AppCode, ?PriceCode, ?Addition); select last_insert_id()", Settings.Default.tbLogs), cLog);
                cmdLog.Parameters.Add("?AppCode", Settings.Default.AppCode);
                cmdLog.Parameters.Add("?PriceCode", MySqlDbType.Int64);
                cmdLog.Parameters.Add("?Addition", MySqlDbType.VarString);
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

        protected UInt64 Logging(int CurrPriceCode, string Addition)
        {
            if (CurrPriceCode > -1)
                FormLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "{0}", Addition);
            else
                FormLog.Log(this.GetType().Name, "{0}", Addition);

            if (cLog.State != System.Data.ConnectionState.Open)
                cLog.Open();
            if (CurrPriceCode > -1)
                cmdLog.Parameters["?PriceCode"].Value = CurrPriceCode;
            else
                cmdLog.Parameters["?PriceCode"].Value = 0;
            cmdLog.Parameters["?Addition"].Value = Addition;
			bool NeedLogging = true;
			//���� ������ � ����������� �� ������, �� ��� ������, ���� ������, �� ���������� ��� ������
			//���� ���������� � ������� � ���������, �� ��������� �����������, � ������ ������ ��������� ��� ���������
			if (!String.IsNullOrEmpty(Addition))
			{
				if (ErrorPriceLogging.ErrorMessages.ContainsKey(CurrPriceCode))
				{
					if (ErrorPriceLogging.ErrorMessages[CurrPriceCode] == Addition)
						NeedLogging = false;
					else
						ErrorPriceLogging.ErrorMessages[CurrPriceCode] = Addition;
				}
				else
					ErrorPriceLogging.ErrorMessages.Add(CurrPriceCode, Addition);
			}
			else
			{
				if (ErrorPriceLogging.ErrorMessages.ContainsKey(CurrPriceCode))
					ErrorPriceLogging.ErrorMessages.Remove(CurrPriceCode);
			}

			if (NeedLogging)
				return ExecuteScalar(cmdLog);
			else
				return 0;
        }

        protected void LoggingToService(string Addition)
        {
            FormLog.Log(this.GetType().Name + ".Error", Addition);
			if (!knowErrors.Contains(Addition))
            try
            {
                MailMessage mm = new MailMessage("service@analit.net", "service@analit.net",
                    "������ � Downloader", Addition);
                SmtpClient sc = new SmtpClient(Settings.Default.SMTPHost);
                sc.Send(mm);
				knowErrors.Add(Addition);
            }
            catch
            {
            }
        }

        #endregion
    }
}
