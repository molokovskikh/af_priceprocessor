using System;
using Inforoom.Downloader.Documents;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Waybills;
using log4net;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using System.IO;
using System.Data;
using System.Net.Mail;
using System.Collections.Generic;
using ExecuteTemplate;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using FileHelper=Inforoom.Common.FileHelper;

namespace Inforoom.Downloader
{
    //����� �������� �������� ����� �� ������� Sources
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

	//����� ��� �������� ��������� ������ �� ������� �����-�����
	//��������� �� ���������� ������ �� ��� ����
	public static class ErrorPriceLogging
	{
		public static Dictionary<ulong, string> ErrorMessages = new Dictionary<ulong, string>();
	}

	//������������ � ��������� ���� ���������� ��������� ���������
	public enum DownPriceResultCode
	{ 
		SuccessDownload = 2,
		ErrorProcess = 3,
		ErrorDownload = 5
	}

	public abstract class BaseSourceHandler : AbstractHandler
	{
		//���������� ��� ������
		protected MySqlConnection _workConnection;
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
        protected ulong CurrPriceCode;
		protected ulong? CurrCostCode;
		protected ulong CurrPriceItemId;
		protected ulong? CurrParentSynonym;
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
		protected ILog _log;

        public BaseSourceHandler()
        {
            SleepTime = Settings.Default.HandlerRequestInterval;
			_log = LogManager.GetLogger(GetType());
		}

		//������ �����������
		public override void StartWork()
		{
			CreateDirectoryPath();
            CreateWorkConnection();

            base.StartWork();
        }

		public override void StopWork()
		{
			base.StopWork();

			if (!tWork.Join(maxJoinTime))
				_logger.ErrorFormat("������� ����� �� ������������ �� {0} �����������.", maxJoinTime);
            if (_workConnection.State == ConnectionState.Open)
                try { _workConnection.Close(); }
                catch { }
        }

		protected override void Ping()
		{
			base.Ping();	
			try { _workConnection.Ping(); } catch{}
		}

        //���� ���������
        protected string SourceType
        { 
            get
            {
                return sourceType;
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
  pi.LastDownload
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
			DownHandlerPath = FileHelper.NormalizeDir(Settings.Default.TempPath) + "Down" + sourceType;
			if (!Directory.Exists(DownHandlerPath))
				Directory.CreateDirectory(DownHandlerPath);
			DownHandlerPath += Path.DirectorySeparatorChar;

			DownHistoryPath = FileHelper.NormalizeDir(Settings.Default.HistoryPath);
			if (!Directory.Exists(DownHistoryPath))
				Directory.CreateDirectory(DownHistoryPath);
		}

        protected void CreateWorkConnection()
        {
            _workConnection = new MySqlConnection(Literals.ConnectionString());
            try
            {
                _workConnection.Open();
                daFillSources = new MySqlDataAdapter(GetSQLSources(), _workConnection);
            }
            catch (Exception ex)
            {
				_logger.Error("������ �� CreateWorkConnection", ex);
            }
        }

        protected void FillSourcesTable()
        {
        	try
			{
				dtSources = MethodTemplate.ExecuteMethod<ExecuteArgs, DataTable>(
					new ExecuteArgs(), GetSourcesTable, null, 
					_workConnection, true, false, delegate { Ping(); });

				if (_log.IsDebugEnabled)
					_log.DebugFormat("��� ����������� {0} {1}", sourceType, dtSources != null ? "��������� ���������� " + dtSources.Rows.Count : "��������� �� ���������");
			}
			catch
			{
				// ���� ����� ��������� ������, �� �� �������� ������� ���������� � 
				// ����� ����������� ������� � �����������
				if (_workConnection.State != ConnectionState.Closed)
					try { _workConnection.Close(); } catch { }
				_workConnection.Open();
				dtSources = MethodTemplate.ExecuteMethod<ExecuteArgs, DataTable>(
					new ExecuteArgs(), GetSourcesTable, null,
					_workConnection, true, false, delegate { Ping(); });
			}
		}

		protected virtual DataTable GetSourcesTable(ExecuteArgs e)
		{
			dtSources.Clear();
			daFillSources.SelectCommand.Transaction = e.DataAdapter.SelectCommand.Transaction;
			daFillSources.Fill(dtSources);
			return dtSources;
		}

        protected static void ErrorMailSend(int UID, string ErrorMessage, Stream ms)
        {
			using (var mm = new MailMessage(
				Settings.Default.FarmSystemEmail, 
				Settings.Default.SMTPErrorList,
				String.Format("������ � UID {0} �� ���� ����������", UID),
				String.Format("UID : {0}\n������ : {1}", UID, ErrorMessage)))
			{
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
			ms.Position = 0;
			using (var mm = new MailMessage(
				Settings.Default.FarmSystemEmail, 
				GetFailMail(),
				String.Format("{0} ( {1} )", FromAddress, SourceType),
				String.Format("���� : {0}\n�� : {1}\n���� : {2}\n���� ������ : {3}\n������� : {4}\n\n������ ���������� :\n{5}",
					Subject,
					FromAddress,
					ToAddress,
					LetterDate,
					cause,
					AttachNames)))
			{
				mm.Attachments.Add(new Attachment(ms, ((String.IsNullOrEmpty(Subject)) ? "Unrec" : Subject) + ".eml"));
				var sc = new SmtpClient(Settings.Default.SMTPHost);
				sc.Send(mm);
			}
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
			if (ArchiveHelper.IsArchive(InFile))
			{
				if ((drCurrent[SourcesTableColumns.colExtrMask] is String) &&
					!String.IsNullOrEmpty(drCurrent[SourcesTableColumns.colExtrMask].ToString()))
				{
					ExtrFile = PriceProcessor.FileHelper.FindFromArhive(
						InFile + ExtrDirSuffix, (string) drCurrent[SourcesTableColumns.colExtrMask]);
				}
				else
					ExtrFile = String.Empty;
			}
			if (String.IsNullOrEmpty(ExtrFile))
			{
				DownloadLogEntity.Log(sourceTypeId, CurrPriceItemId, String.Format(
					"�� ������� ����� ���� � ������. ����� ����� � ������ : '{0}'",
					drCurrent[SourcesTableColumns.colExtrMask]));
				return false;
			}
			return true;
		}

		protected void Cleanup()
		{
			try
			{
				if (Directory.Exists(CurrFileName + ExtrDirSuffix))
					Directory.Delete(CurrFileName + ExtrDirSuffix, true);
			}
			catch (Exception e)
			{
				_logger.Error(String.Format("������ ��� �������� ���������� {0}", CurrFileName + ExtrDirSuffix), e);
			}
			try
			{

				if (_logger.IsDebugEnabled)
					_logger.DebugFormat("������� ������� ���� : {0}", CurrFileName);
				if (File.Exists(CurrFileName))
					File.Delete(CurrFileName);
				if (_logger.IsDebugEnabled)
					_logger.DebugFormat("���� ������ : {0}", CurrFileName);
			}
			catch (Exception ex)
			{
				_logger.ErrorFormat("������ ��� �������� ����� {0}:\r\n{1}", CurrFileName, ex);
			}
		}

		/// <summary>
		/// ������ ��� ������� �� ������� future.Addresses ��
		/// Id ��� LegacyId
		/// </summary>
		/// <param name="addressId">Id ��� LegacyId � ������� Addresses
		/// ���� ����� ������� LegacyId, �� � ��� ���������� ��������� Addresses.Id</param>
		/// <returns></returns>
		protected int? GetClientIdByAddress(ref int? addressId)
		{
			if (addressId == null)
				return null;
			var queryGetClientCodeByLegacyId = String.Format(@"
SELECT Addr.ClientId
FROM future.Addresses Addr
WHERE Addr.LegacyId = {0}", addressId);

			var queryGetClientCodeByAddressId = String.Format(@"
SELECT Addr.ClientId
FROM future.Addresses Addr
WHERE Addr.Id = {0}", addressId);

			var queryGetAddressIdByLegacyId = String.Format(@"
SELECT Addr.Id
FROM future.Addresses Addr
WHERE Addr.LegacyId = {0}", addressId);

			// �������� ������� ��� ������� �� Addresses.LegacyId
			var clientCode = MySqlHelper.ExecuteScalar(_workConnection, queryGetClientCodeByLegacyId);
			if ((clientCode == null) || (clientCode is DBNull))
			{
				// ���� �� ������� �� LegacyId, �������� ������� �� Addresses.Id
				clientCode = MySqlHelper.ExecuteScalar(_workConnection, queryGetClientCodeByAddressId);
				// ���� ������ �� �������, ���������� null
				if ((clientCode == null) || (clientCode is DBNull))
					return null;
			}
			else
				// ���� ������� ��� ������� �� LegacyID, �������� ��� ������ �� LegacyId
				addressId = Convert.ToInt32(MySqlHelper.ExecuteScalar(_workConnection, queryGetAddressIdByLegacyId));
			return Convert.ToInt32(clientCode);
		}
	}
}
