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
		//������ �� ������� �����
		protected Thread tWork;
		//����� "������" �����
		protected int SleepTime;
		//����� ���������� "�������" �����������
		protected DateTime lastPing;

		//���������� ��� �����������
		protected MySqlConnection cLog;
        protected MySqlCommand cmdLog;

		//���������� ��� ������
		protected MySqlConnection cWork;
        protected MySqlCommand cmdUpdatePriceDate;

        //��� ���������, �� ������� �������� ������ ����������
        protected string sourceType;

        //��� �������� ��������������� ������
        protected int CurrPriceCode;
        //������� ��������� ����������
        protected string CurrTempPath;
        //������� ������������� ������ � �������
        protected DataRow drCurrent;
        //������� ��������� ����
        protected string CurrFileName;
        //������� ���� �����
        protected DateTime CurrPriceDate;

        public BaseSourceHandler(string sourceType)
		{
            this.sourceType = sourceType;
			Ping();
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
			tWork.Start();
            CreateLogConnection();
            CreateWorkConnection();
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

		//���������� �����������
		public void RestartWork()
		{
			tWork.Abort();
			Thread.Sleep(500);
			tWork = new Thread(new ThreadStart(ThreadWork));
			tWork.Start();
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
				Sleeping();
                Ping();
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
                SourceType());
        }

        protected void CreateWorkConnection()
        {
            cWork = new MySqlConnection(
                String.Format("server={0};username={1}; password={2}; database={3}; pooling=false",
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
            }
            catch (Exception ex)
            {
                FormLog.Log(this.GetType().Name + ".CreateWorkConnection", "{0}", ex);
            }
        }

        protected void OperatorMailSend()
        {
            MailMessage mm = new MailMessage(Settings.Default.SMTPUserError, Settings.Default.SMTPUserCopy,
                String.Format("{0}; {1} ({2})", CurrPriceCode, drCurrent[colRegionName], SourceType()),
                String.Format("��� ����� : {0}\n�����: {1}; {2}\n{3}\n����: {4}",
                    CurrPriceCode, drCurrent[SourcesTable.colShortName], drCurrent[SourcesTable.colRegionName], "", DateTime.Now));
            if (!String.IsNullOrEmpty(CurrFileName))
                mm.Attachments.Add(new Attachment(CurrFileName));
            SmtpClient sc = new SmtpClient(Settings.Default.SMTPHost);
            sc.Send(mm);
        }

        /// <summary>
        /// �������� ���� ������ � ����
        /// </summary>
        /// <param name="UpadatePriceCode"></param>
        /// <param name="UpDT"></param>
        protected void UpdateDB(int UpadatePriceCode, DateTime UpDT)
        {
            if (cWork.State != System.Data.ConnectionState.Open)
                cWork.Open();

            cmdUpdatePriceDate.Parameters["FirmCode"].Value = UpadatePriceCode;
            cmdUpdatePriceDate.Parameters["DT"].Value = UpDT;
            cmdUpdatePriceDate.ExecuteNonQuery();
        }

        protected void SetCurrentPriceCode(DataRow dr)
        {
            drCurrent = dr;
            CurrPriceCode = Convert.ToInt32(dr[SourcesTable.colFirmCode]);
            CurrTempPath = Path.GetFullPath(Settings.Default.TempPath) + Path.DirectorySeparatorChar + CurrPriceCode.ToString();
            if (!Directory.Exists(CurrTempPath))
                Directory.CreateDirectory(CurrTempPath);
            CurrTempPath += Path.DirectorySeparatorChar;
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
        #endregion
    }
}
