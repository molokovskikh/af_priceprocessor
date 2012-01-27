using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using LumiSoft.Net.IMAP;
using Inforoom.Common;
using LumiSoft.Net.SMTP.Client;
using FileHelper = Inforoom.PriceProcessor.FileHelper;
using System.Net.Mail;


namespace Inforoom.Downloader
{
	public class EMAILSourceHandler : BasePriceSourceHandler
	{
		//UID �������� ��������������� ������
		protected int currentUID;

		private Mime _message;

		public EMAILSourceHandler()
		{
			SourceType = "EMAIL";
		}

		public static string GetCorrectEmailAddress(string Source)
		{
			return Source.Replace("'", String.Empty).Trim();
		}

		protected bool IsMailAddress(string address)
		{
			try
			{
#pragma warning disable 168
				var mailAddress = new MailAddress(address);
#pragma warning restore 168
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		protected override void ProcessData()
		{
			var imapSourceFolder = Settings.Default.IMAPSourceFolder;
			using (var imapClient = new IMAP_Client())
			{
				imapClient.Connect(Settings.Default.IMAPHost, 143);
				IMAPAuth(imapClient);
				imapClient.SelectFolder(imapSourceFolder);

				try
				{
					IMAP_FetchItem[] items;
					do
					{
						Ping();
						var sequence_set = new IMAP_SequenceSet();
						sequence_set.Parse("1:*", long.MaxValue);
						items = imapClient.FetchMessages(sequence_set, IMAP_FetchItem_Flags.UID, false, false);
						Ping();

						if (items == null || items.Length == 0)
							continue;

						foreach (var item in items)
						{
							IMAP_FetchItem[] OneItem = null;
							try
							{
								var sequence_Mess = new IMAP_SequenceSet();
								sequence_Mess.Parse(item.UID.ToString(), long.MaxValue);
								OneItem = imapClient.FetchMessages(sequence_Mess, IMAP_FetchItem_Flags.Message, false, true);
								_message = Mime.Parse(OneItem[0].MessageData);
								currentUID = item.UID;
								Ping();
								ProcessMime(_message);
							}
							catch (Exception ex)
							{
								_logger.Error("������ ��� ��������� ������", ex);
								_message = null;
								SendBrokenMessage(item, OneItem, ex);
							}
						}

						//���������� �������� �����
						var sequence = new IMAP_SequenceSet();

						sequence.Parse(String.Join(",", items.Select(i => i.UID.ToString()).ToArray()), long.MaxValue);
						imapClient.DeleteMessages(sequence, true);
					}
					while (items != null && items.Length > 0);
				}
				finally
				{
					_message = null;
				}
			}
		}

		private void SendBrokenMessage(IMAP_FetchItem item, IMAP_FetchItem[] OneItem, Exception ex)
		{
			MemoryStream ms = null;
			if (OneItem != null && OneItem.Length > 0 && OneItem[0].MessageData != null)
				ms = new MemoryStream(OneItem[0].MessageData);
			ErrorMailSend(item.UID, ex.ToString(), ms);
		}

		protected virtual void IMAPAuth(IMAP_Client c)
		{
			c.Authenticate(Settings.Default.IMAPUser, Settings.Default.IMAPPass);
		}

		protected void ProcessMime(Mime m)
		{
			var from = GetAddressList(m);
			m = UueHelper.ExtractFromUue(m, DownHandlerPath);
			FillSourcesTable();
			try
			{
				CheckMime(m);
				ProcessAttachs(m, from);
			}
			catch (EMailSourceHandlerException e)
			{
				// ��������� ������ ����������, ����� ������������ 
				// ��� ��� ������ � �������������� ������
				ErrorOnMessageProcessing(m, from, e);
			}
		}

		protected virtual void ErrorOnMessageProcessing(Mime m, AddressList from, EMailSourceHandlerException exception)
		{
			SendUnrecLetter(m, from, exception);
		}

		protected virtual void SendUnrecLetter(Mime m, AddressList from, EMailSourceHandlerException exception)
		{
			try
			{
				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));
				var ms = new MemoryStream(m.ToByteData());
#if !DEBUG
				SmtpClientEx.QuickSendSmartHost(
					Settings.Default.SMTPHost,
					25,
					Environment.MachineName,
					Settings.Default.ServiceMail,
					new[] { Settings.Default.UnrecLetterMail },
					ms);
#endif
				FailMailSend(m.MainEntity.Subject, from.ToAddressListString(), 
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, attachments, exception.Message);
				DownloadLogEntity.Log((ulong)PriceSourceType.EMail, String.Format("������ �� ����������.������� : {0}; ���� :{1}; �� : {2}", 
					exception.Message, m.MainEntity.Subject, from.ToAddressListString()));
			}
			catch (Exception exMatch)
			{
				_logger.Error("�� ������� ��������� �������������� ������", exMatch);
			}
		}

		protected virtual void ProcessAttachs(Mime m, AddressList from)
		{
			//���� �� ����������� ������ ������ � ����������, ����� - ������ �� ����������
			var matched = false;

			var attachmentFileName = string.Empty;

			var attachments = m.GetValidAttachements();
			foreach (var entity in attachments)
			{
				attachmentFileName = SaveAttachement(entity);
				UnPack(m, ref matched, from, attachmentFileName);
				Cleanup();
			}

			if (!matched)
				throw new EMailSourceHandlerException("�� ������ ��������.");
		}

		/// <summary>
		/// ���������, ��� ������ �������� ��������
		/// </summary>
		public virtual void CheckMime(Mime m)
		{
			if (m.Attachments.Length == 0)
				throw new EMailSourceHandlerException("������ �� �������� ��������.");
		}

		/// <summary>
		/// ���������� ������ ���������� �������� � ������ ��� � �����������
		/// </summary>
		private void UnPack(Mime m, ref bool Matched, AddressList FromList, string ShortFileName)
		{
			DataRow[] drLS;

			//������ �� ���������� ���� ������ From, ������ ��� ��������. ���� �� ����������� � Sender
			foreach (var mbFrom in FromList.Mailboxes)
			{
				//������ �� ���������� ���� ������ TO, ������ ��� ��������
				foreach (var mba in m.MainEntity.To.Mailboxes)
				{
					drLS = dtSources.Select(String.Format("({0} = '{1}') and ({2} like '*{3}*')",
						SourcesTableColumns.colEMailTo, GetCorrectEmailAddress(mba.EmailAddress),
						SourcesTableColumns.colEMailFrom, mbFrom.EmailAddress));
					foreach (DataRow drS in drLS)
					{
						if ((drS[SourcesTableColumns.colPriceMask] is String) && 
							!String.IsNullOrEmpty(drS[SourcesTableColumns.colPriceMask].ToString()))
						{
							var priceMask = (string) drS[SourcesTableColumns.colPriceMask];
							var priceMaskIsMatched = FileHelper.CheckMask(ShortFileName, priceMask);
							if (priceMaskIsMatched)
							{
								SetCurrentPriceCode(drS);

								// ������� ���������������
								var CorrectArchive = CheckFile(drS["ArchivePassword"].ToString());

								if (CorrectArchive)
								{
									string ExtrFile = String.Empty;
									if (ProcessPriceFile(CurrFileName, out ExtrFile, (ulong)PriceSourceType.EMail))
									{
										Matched = true;
										LogDownloadedPrice((ulong)PriceSourceType.EMail, Path.GetFileName(CurrFileName), ExtrFile);
									}
									else
									{
										LogDownloaderFail((ulong)PriceSourceType.EMail, "�� ������� ���������� ���� '" + 
											Path.GetFileName(CurrFileName) + "'", 
											Path.GetFileName(CurrFileName));
									}
								}
								else
								{
									LogDownloaderFail((ulong)PriceSourceType.EMail, "�� ������� ����������� ���� '" + 
										Path.GetFileName(CurrFileName) + "'. ���� ���������", 
										Path.GetFileName(CurrFileName));
								}
								drS.Delete();
							}
						}
					}
					dtSources.AcceptChanges();

				}//foreach (MailboxAddress mba in m.MainEntity.To.Mailboxes)

			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)
		}

		protected override void CopyToHistory(UInt64 PriceID)
		{
			string HistoryFileName = DownHistoryPath + PriceID + ".eml";
			string SavedFile = DownHandlerPath + PriceID + ".eml";
			try
			{
				_message.ToFile(SavedFile);
				File.Copy(SavedFile, HistoryFileName);
				File.Delete(SavedFile);
			}
			catch { }
		}

		protected AddressList GetAddressList(Mime m)
		{
			// ��������� ������ ������� From
			var from = new AddressList();
			bool senderFound = false;

			// ����� �� ���� Sender, ����� ���� �� ����������
			string senderAddress = null;
			// ���� ���� ����������� � ����� �� ������
			if ((m.MainEntity.Sender != null) && 
				!String.IsNullOrEmpty(m.MainEntity.Sender.EmailAddress))
			{
				// �������� ���������� �����
				senderAddress = GetCorrectEmailAddress(m.MainEntity.Sender.EmailAddress);
				// ���� ����� ��������� ������������, �� ���������� �������� ����
				if (!IsMailAddress(senderAddress))
					senderAddress = null;
			}
			// ������ ������ ������� ����������� ���� - ����
			if (m.MainEntity.From != null)
			{
				foreach (var a in m.MainEntity.From.Mailboxes)
				{
					//���������, ��� ����� ���-�� ��������
					if (!String.IsNullOrEmpty(a.EmailAddress))
					{
						// ������� ���������� �����
						var correctAddress = GetCorrectEmailAddress(a.EmailAddress);
						// ���� ����� ���� �������� ����� �������� EMail-�������, �� ��������� � ������
						if (IsMailAddress(correctAddress))
						{
							from.Add(new MailboxAddress(correctAddress));
							if (!String.IsNullOrEmpty(senderAddress) && 
								senderAddress.Equals(correctAddress, StringComparison.OrdinalIgnoreCase))
								senderFound = true;
						}
					}
				}
			}

			if (!String.IsNullOrEmpty(senderAddress) && !senderFound)
				from.Add(new MailboxAddress(senderAddress));

			// ������ ������ ������� ����������� ���� - ����, 
			// � ���� ������ ������� ������ ���������, ����� ��� ���� � �������
			if (m.MainEntity.To == null)
				m.MainEntity.To = new AddressList();

			return from;
		}

		protected bool CheckFile()
		{
			return CheckFile(null);
		}

		private bool CheckFile(string archivePassword)
		{
			var fileName = CurrFileName;
			var tempExtractDir = CurrFileName + ExtrDirSuffix;

			//�������� �� ��������� ���� ����������, ���� ���, �� ������������ �� �����
			if (ArchiveHelper.IsArchive(fileName))
			{
				if (ArchiveHelper.TestArchive(fileName, archivePassword))
				{
					try
					{
						FileHelper.ExtractFromArhive(fileName, tempExtractDir, archivePassword);
						return true;
					}
					catch (ArchiveHelper.ArchiveException)
					{
						return false;
					}
				}
				return false;
			}
			return true;
		}

		protected string SaveAttachement(MimeEntity ent)
		{
			var attachmentFileName = GetShortFileNameFromAttachement(ent);
			CurrFileName = DownHandlerPath + attachmentFileName;
			using (var fs = new FileStream(CurrFileName, FileMode.Create))
				ent.DataToStream(fs);
			return attachmentFileName;
		}

		protected static string GetShortFileNameFromAttachement(MimeEntity ent)
		{
			var filename = ent.GetFilename();
			if (filename == null)
				return String.Empty;
			return filename;
		}

	}

	public static class MimeEntityExtentions
	{
		public static IEnumerable<MimeEntity> GetValidAttachements(this Mime mime)
		{
			return mime.Attachments.Where(m => !String.IsNullOrEmpty(m.GetFilename()) && m.Data != null);
		}

		public static IEnumerable<string> GetAttachmentFilenames(this Mime mime)
		{
			var result = new List<string>();
			var attachments = mime.GetValidAttachements();
			foreach (var entity in attachments)
				result.Add(entity.GetFilename());
			return result;
		}

		public static string GetFilename(this MimeEntity entity)
		{
			if (!String.IsNullOrEmpty(entity.ContentDisposition_FileName))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentDisposition_FileName));
			if (!String.IsNullOrEmpty(entity.ContentType_Name))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentType_Name));
			return null;
		}
	}

	public class EmailFromUnregistredMail : EMailSourceHandlerException
	{
		public EmailFromUnregistredMail(string message) : base(message)
		{}

		public EmailFromUnregistredMail(string message, string subject, string body) : base(message, subject, body)
		{}
	}

	public class EMailSourceHandlerException : Exception
	{
		public EMailSourceHandlerException(string message) : base(message)
		{}

		public EMailSourceHandlerException(string message, string subject, string body) : base(message)
		{
			Body = body;
			Subject = subject;
		}

		public string Body { get; set; }
		public string Subject { get; set; }
	}

}
