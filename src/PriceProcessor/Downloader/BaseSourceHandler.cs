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
using FileHelper=Inforoom.Common.FileHelper;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

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
		/// <summary>
		/// ������� � �����������
		/// </summary>
		protected DataTable dtSources = new DataTable();

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

			base.StartWork();
		}

		public override void StopWork()
		{
			base.StopWork();

			if (!tWork.Join(maxJoinTime))
				_logger.ErrorFormat("������� ����� �� ������������ �� {0} �����������.", maxJoinTime);
		}

		//���� ���������
		protected string SourceType { get; set;  }

		protected virtual string GetSQLSources()
		{
			return GetSourcesCommand(SourceType);
		}

		public static string GetSourcesCommand(string type)
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
  pi.LastDownload,
  st.RequestInterval,
  st.LastSuccessfulCheck
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
				type);
		}

		protected void CreateDirectoryPath()
		{
			DownHandlerPath = FileHelper.NormalizeDir(Settings.Default.TempPath) + "Down" + SourceType;
			if (!Directory.Exists(DownHandlerPath))
				Directory.CreateDirectory(DownHandlerPath);
			DownHandlerPath += Path.DirectorySeparatorChar;

			DownHistoryPath = FileHelper.NormalizeDir(Settings.Default.HistoryPath);
			if (!Directory.Exists(DownHistoryPath))
				Directory.CreateDirectory(DownHistoryPath);
		}

		protected void FillSourcesTable()
		{
			using(var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				dtSources.Clear();
				connection.Open();
				var adapter = new MySqlDataAdapter(GetSQLSources(), connection);
				adapter.Fill(dtSources);
			}
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
#if !DEBUG
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
				mm.Attachments.Add(new Attachment(ms, "�������� ������.eml"));
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
		/// Id
		/// </summary>
		/// <param name="addressId">Id � ������� Addresses
		/// ���� ����� ������� LegacyId, �� � ��� ���������� ��������� Addresses.Id</param>
		/// <returns></returns>
		protected uint? GetClientIdByAddress(ref uint? addressId)
		{
			if (addressId == null)
				return null;

			var address = addressId;

			var clientId = With.Connection<uint?>(c => {

				var queryGetClientCodeByAddressId = String.Format(@"
SELECT Addr.ClientId
FROM future.Addresses Addr
WHERE Addr.Id = {0}", address);

				var clientCode = MySqlHelper.ExecuteScalar(c, queryGetClientCodeByAddressId);
				return Convert.ToUInt32(clientCode);
			});
			addressId = address;
			return clientId;
		}
	}
}
