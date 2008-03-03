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
using Inforoom.Logging;
using System.Configuration;
using Inforoom.Common;

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

	//������������ � ��������� ���� ���������� ��������� ���������
	public enum DownPriceResultCode
	{ 
		SuccessDownload = 2,
		ErrorProcess = 3,
		ErrorDownload = 5
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
			CreateDirectoryPath();
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

            SimpleLog.Log(this.GetType().Name, "���������� �����");
            try
            {
                StopWork();
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                SimpleLog.Log(this.GetType().Name, "������ ��� �������� ����� : {0}", ex);
            }
			tWork = new Thread(new ThreadStart(ThreadWork));
            try
            {
                StartWork();
            }
            catch (Exception ex)
            {
                SimpleLog.Log(this.GetType().Name, "������ ��� ������� ����� : {0}", ex);
            }
			SimpleLog.Log( this.GetType().Name, "������������� �����");
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
                    SimpleLog.Log(this.GetType().Name, "������ � ����� : {0}", ex);
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
  pui.DateCurPrice,
  pui.DatePrevPrice,
  fr.PriceFMT,
  pf.FileExtention,
  st.SourceType, st.LastDateTime, st.PriceDateTime, 
  st.PricePath,  
  st.EMailTo, st.EMailFrom, 
  st.FTPDir, st.FTPLogin, st.FTPPassword, st.FTPPassiveMode,
  st.HTTPLogin, st.HTTPPassword,
  st.PriceMask, st.ExtrMask
FROM       farm.Sources             as st
INNER JOIN UserSettings.PricesData  AS PD ON PD.PriceCode = st.FirmCode
INNER JOIN farm.FormRules           AS fr ON fr.FirmCode = st.FirmCode
INNER JOIN farm.pricefmts           AS pf ON pf.Format = fr.PriceFMT
INNER JOIN usersettings.ClientsData AS CD ON CD.FirmCode = PD.FirmCode
inner join farm.regions             as r  on r.RegionCode = cd.RegionCode
inner join usersettings.price_update_info pui on pui.PriceCode = PD.PriceCode
WHERE
    st.SourceType = '{3}'
AND cd.FirmStatus   = 1
AND pd.AgencyEnabled= 1", 
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
                cmdUpdatePriceDate.Parameters.Add("?DT", MySqlDbType.Datetime);
				cmdUpdatePriceDate.Parameters.Add("?NowDate", MySqlDbType.Datetime);
				cmdFillSources = new MySqlCommand(GetSQLSources(), cWork);
                daFillSources = new MySqlDataAdapter(cmdFillSources);
            }
            catch (Exception ex)
            {
                SimpleLog.Log(this.GetType().Name + ".CreateWorkConnection", "{0}", ex);
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

        protected void ErrorMailSend(int UID, string ErrorMessage, Stream ms)
        {
			MailMessage mm = new MailMessage(Settings.Default.FarmSystemEmail, Settings.Default.SMTPErrorList,
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
			ms.Position = 0;
			MailMessage mm = new MailMessage(Settings.Default.FarmSystemEmail, GetFailMail(),
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

		protected string GetSuccessAddition(string ArchName, string FileName)
		{
			return String.Format("{0} > {1}", Path.GetFileName(ArchName), Path.GetFileName(FileName));
		}

        protected bool ProcessPriceFile(string InFile, out string ExtrFile)
        {
            ExtrFile = InFile;
            if (ArchiveHelper.IsArchive(InFile))
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
                string NormalName = FileHelper.NormalizeDir(Settings.Default.InboundPath) + CurrPriceCode.ToString() + GetExt();
                try
                {
                    if (File.Exists(NormalName))
                        File.Delete(NormalName);
                    File.Copy(ExtrFile, NormalName);
                    SimpleLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "Price " + (string)drCurrent[SourcesTable.colShortName] + " - " + (string)drCurrent[SourcesTable.colPriceName] + " downloaded/decompressed");
                    return true;
                }
                catch(Exception ex)
                {
                    Logging(CurrPriceCode, String.Format("�� ������� ��������� ���� '{0}' � ������� '{1}'", ExtrFile, NormalName));
                    SimpleLog.Log(this.GetType().Name + CurrPriceCode.ToString(), "Cant move : " + ex.ToString());
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

				SimpleLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "������� ������� ���� : " + CurrFileName);
				if (File.Exists(CurrFileName))
					File.Delete(CurrFileName);
				SimpleLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "���� ������ : " + CurrFileName);
			}
            catch (Exception ex)
			{
				SimpleLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "������ ��� �������� ����� : " + CurrFileName + "  ������ : " + ex.ToString());
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
				cmdLog = new MySqlCommand("insert into logs.downlogs (LogTime, Host, PriceCode, Addition, ResultCode, ArchFileName, ExtrFileName) VALUES (now(), ?Host, ?PriceCode, ?Addition, ?ResultCode, ?ArchFileName, ?ExtrFileName); select last_insert_id()", cLog);
				cmdLog.Parameters.AddWithValue("?Host", Environment.MachineName);
                cmdLog.Parameters.Add("?PriceCode", MySqlDbType.Int64);
                cmdLog.Parameters.Add("?Addition", MySqlDbType.VarString);
				cmdLog.Parameters.Add("?ResultCode", MySqlDbType.Byte);
				cmdLog.Parameters.Add("?ArchFileName", MySqlDbType.VarString);
				cmdLog.Parameters.Add("?ExtrFileName", MySqlDbType.VarString);
			}
			catch(Exception ex)
			{
				SimpleLog.Log( this.GetType().Name + ".CreateLogConnection", "{0}", ex);
			}
		}

        protected void Logging(string Addition)
        {
            Logging(-1, Addition, DownPriceResultCode.ErrorDownload, null, null);
        }

		protected void Logging(int CurrPriceCode, string Addition)
		{
			Logging(CurrPriceCode, Addition, DownPriceResultCode.ErrorDownload, null, null);
		}

		protected UInt64 Logging(int CurrPriceCode, string Addition, DownPriceResultCode resultCode, string ArchFileName, string ExtrFileName)
        {
            if (CurrPriceCode > -1)
                SimpleLog.Log(this.GetType().Name + "." + CurrPriceCode.ToString(), "{0}", Addition);
            else
                SimpleLog.Log(this.GetType().Name, "{0}", Addition);

            if (cLog.State != System.Data.ConnectionState.Open)
                cLog.Open();
            if (CurrPriceCode > -1)
                cmdLog.Parameters["?PriceCode"].Value = CurrPriceCode;
            else
                cmdLog.Parameters["?PriceCode"].Value = 0;
            cmdLog.Parameters["?Addition"].Value = Addition;
			cmdLog.Parameters["?ResultCode"].Value = Convert.ToByte(resultCode);
			cmdLog.Parameters["?ArchFileName"].Value = ArchFileName;
			cmdLog.Parameters["?ExtrFileName"].Value = ExtrFileName;

			bool NeedLogging = true;
			//���� ��� �������� ��������, �� ���������� ��� ������
			//���� ��� ������, �� ���� ���������� � ������� � ���������, �� ��������� �����������, � ������ ������ ��������� ��� ���������
			if (resultCode == DownPriceResultCode.ErrorDownload)
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
            SimpleLog.Log(this.GetType().Name + ".Error", Addition);
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
