using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using Inforoom.PriceProcessor.Properties;
using LumiSoft.Net.IMAP;
using Inforoom.Common;
using FileHelper = Inforoom.PriceProcessor.FileHelper;
using Inforoom.PriceProcessor;
using System.Net.Mail;


namespace Inforoom.Downloader
{
	public class EMAILSourceHandler : BasePriceSourceHandler
    {
		//������ ��������� UID, �� ������� �� ���� ��� ��� ���������� ������
		protected List<int> errorUIDs;

		//UID �������� ��������������� ������
		protected int currentUID;

    	private Mime _message;

		public EMAILSourceHandler()
		{
			sourceType = "EMAIL";
			errorUIDs = new List<int>();
		}

		protected string GetCorrectEmailAddress(string Source)
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
			using (var c = new IMAP_Client())
			{
				c.Connect(Settings.Default.IMAPHost, 143);
				IMAPAuth(c);
				c.SelectFolder("INBOX");

				try
				{
					IMAP_FetchItem[] items = null;
					List<string> ProcessedUID = null;
					do
					{
						Ping();
						ProcessedUID = new List<string>();
						var sequence_set = new IMAP_SequenceSet();
						sequence_set.Parse("1:*", long.MaxValue);
						items = c.FetchMessages(sequence_set, IMAP_FetchItem_Flags.UID, false, false);
						Ping();

						if ((items != null) && (items.Length > 0))
						{
							foreach (IMAP_FetchItem item in items)
							{
								//Mime m = null;
								IMAP_FetchItem[] OneItem = null;
								try
								{
									var sequence_Mess = new IMAP_SequenceSet();
									sequence_Mess.Parse(item.UID.ToString(), long.MaxValue);
									OneItem = c.FetchMessages(sequence_Mess, IMAP_FetchItem_Flags.Message, false, true);
									_message = Mime.Parse(OneItem[0].MessageData);
									currentUID = item.UID;
									ProcessedUID.Add(item.UID.ToString());
									Ping();
								}
								catch (Exception ex)
								{
									if (!errorUIDs.Contains(item.UID))
									{
										_message = null;
										MemoryStream ms = null;
										if ((OneItem != null) && (OneItem.Length > 0) && (OneItem[0].MessageData != null))
											ms = new MemoryStream(OneItem[0].MessageData);
										ErrorMailSend(item.UID, ex.ToString(), ms);
										errorUIDs.Add(item.UID);
									}
									_logger.Error("On Parse", ex);
								}

								if (_message != null)
								{
									try
									{
										ProcessMime(_message);
									}
									catch (Exception ex)
									{
										if (ProcessedUID.Contains(item.UID.ToString()))
											ProcessedUID.Remove(item.UID.ToString());
										if (!errorUIDs.Contains(item.UID))
										{
											MemoryStream ms = null;
											if ((OneItem != null) && (OneItem.Length > 0) && (OneItem[0].MessageData != null))
												ms = new MemoryStream(OneItem[0].MessageData);
											ErrorMailSend(item.UID, ex.ToString(), ms);
											errorUIDs.Add(item.UID);
										}
										_logger.Error("On Process", ex);
									}
								}

							}//foreach (IMAP_FetchItem) 

						}//(items != null) && (items.Length > 0)

						//���������� �������� �����
						if ((items != null) && (items.Length > 0) && (ProcessedUID.Count > 0))
						{
							string uidseq = String.Empty;
							uidseq = String.Join(",", ProcessedUID.ToArray());
							var sequence_setDelete = new IMAP_SequenceSet();
							sequence_setDelete.Parse(uidseq, long.MaxValue);
							c.DeleteMessages(sequence_setDelete, true);
						}

					}
					while ((items != null) && (items.Length > 0));
				}
				finally
				{
					_message = null;
				}
			}
        }

		protected virtual void IMAPAuth(IMAP_Client c)
		{
			c.Authenticate(Settings.Default.IMAPUser, Settings.Default.IMAPPass);
		}

        private void ProcessMime(Mime m)
        {
            AddressList FromList = GetAddressList(m);

            string ShortFileName = string.Empty;

            // �������� �����������
            string AttachNames = String.Empty;
			string _causeSubject = String.Empty, _causeBody = String.Empty, _systemError = String.Empty;

			m = UueHelper.ExtractFromUue(m, DownHandlerPath);

			// ��������� ������ ����������, ����� ������������ 
			// ��� ��� ������ � �������������� ������
			foreach (MimeEntity ent in m.Attachments)
				if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName) ||
					!String.IsNullOrEmpty(ent.ContentType_Name))
				{
					AttachNames += "\"" + GetShortFileNameFromAttachement(ent) +
						"\"" + Environment.NewLine;
				}

        	if (CheckMime(m, ref _causeSubject, ref _causeBody, ref _systemError))
			{
				// ���� � ������ ���� ��������
				FillSourcesTable();

				if (!ProcessAttachs(m, FromList, ref _causeSubject, ref _causeBody))
					ErrorOnProcessAttachs(m, FromList, AttachNames, _causeSubject, _causeBody);
			}
			else
				ErrorOnCheckMime(m, FromList, AttachNames, _causeSubject, _causeBody, _systemError);
        }

		protected virtual void ErrorOnProcessAttachs(Mime m, AddressList FromList, string AttachNames, string causeSubject, string causeBody)
		{
			SendUnrecLetter(m, FromList, AttachNames, causeBody);
		}

		protected virtual void ErrorOnCheckMime(Mime m, AddressList FromList, string AttachNames, string causeSubject, string causeBody, string systemError)
		{
			SendUnrecLetter(m, FromList, AttachNames, causeBody);
		}

		protected virtual void SendUnrecLetter(Mime m, AddressList FromList, 
			string AttachNames, string cause)
		{
			try
			{
				MemoryStream ms = new MemoryStream(m.ToByteData());
				try
				{
					LumiSoft.Net.SMTP.Client.SmtpClientEx.QuickSendSmartHost(
						Settings.Default.SMTPHost,
						25,
						Environment.MachineName,
						Settings.Default.ServiceMail,
						new string[] { Settings.Default.UnrecLetterMail },
						ms);
				}
				catch { }
				FailMailSend(m.MainEntity.Subject, FromList.ToAddressListString(), 
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, AttachNames, cause);
				Logging(String.Format("������ �� ����������.������� : {0}; ���� :{1}; �� : {2}", 
					cause, m.MainEntity.Subject, FromList.ToAddressListString()));
			}
			catch (Exception exMatch)
			{
				_logger.Error("�� ������� ��������� �������������� ������", exMatch);
			}
		}

		protected virtual bool ProcessAttachs(Mime m, AddressList FromList, 
			ref string causeSubject, ref string causeBody)
		{
			//���� �� ����������� ������ ������ � ����������, ����� - ������ �� ����������
			bool _Matched = false;

			bool CorrectArchive = true;
			string ShortFileName = string.Empty;

			foreach (MimeEntity ent in m.Attachments)
			{
				if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName) || 
					!String.IsNullOrEmpty(ent.ContentType_Name))
				{
					ShortFileName = SaveAttachement(ent);
					CorrectArchive = CheckFile();
					UnPack(m, ref _Matched, FromList, ShortFileName, CorrectArchive);
					DeleteCurrFile();
				}
			}

			if (!_Matched)
				causeBody = "�� ������ ��������.";
			return _Matched;
		}

		/// <summary>
		/// ���������, ��� ������ �������� ��������
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		protected virtual bool CheckMime(Mime m, ref string causeSubject, 
			ref string causeBody, ref string systemError)		
		{
			if (m.Attachments.Length == 0)
			{
				causeBody = "������ �� �������� ��������.";
				systemError = causeBody;
			}
			return m.Attachments.Length > 0;
		}
		
		/// <summary>
		/// ���������� ������ ���������� �������� � ������ ��� � �����������
		/// </summary>
		/// <param name="m"></param>
		/// <param name="Matched"></param>
		/// <param name="FromList"></param>
		/// <param name="ShortFileName"></param>
		/// <param name="CorrectArchive"></param>
		protected virtual void UnPack(Mime m, ref bool Matched, AddressList FromList, 
			string ShortFileName, bool CorrectArchive)
		{
			DataRow[] drLS = null;

			//������ �� ���������� ���� ������ From, ������ ��� ��������. ���� �� ����������� � Sender
			foreach (MailboxAddress mbFrom in FromList.Mailboxes)
			{
				//������ �� ���������� ���� ������ TO, ������ ��� ��������
				foreach (MailboxAddress mba in m.MainEntity.To.Mailboxes)
				{
					drLS = dtSources.Select(String.Format("({0} = '{1}') and ({2} like '*{3}*')",
						SourcesTableColumns.colEMailTo, GetCorrectEmailAddress(mba.EmailAddress),
						SourcesTableColumns.colEMailFrom, mbFrom.EmailAddress));
					foreach (DataRow drS in drLS)
					{
						if ((drS[SourcesTableColumns.colPriceMask] is String) && 
							!String.IsNullOrEmpty(drS[SourcesTableColumns.colPriceMask].ToString()))
						{
							if ((WildcardsHelper.IsWildcards((string)drS[SourcesTableColumns.colPriceMask]) && 
								WildcardsHelper.Matched((string)drS[SourcesTableColumns.colPriceMask], ShortFileName)) ||
								(String.Compare(ShortFileName, (string)drS[SourcesTableColumns.colPriceMask], true) == 0))
							{
								SetCurrentPriceCode(drS);
								if (CorrectArchive)
								{
									string ExtrFile = String.Empty;
									if (ProcessPriceFile(CurrFileName, out ExtrFile))
									{
										Matched = true;
										LogDownloaderPrice(null, DownPriceResultCode.SuccessDownload, 
											Path.GetFileName(CurrFileName), ExtrFile);
									}
									else
									{
										LogDownloaderPrice("�� ������� ���������� ���� '" + 
											Path.GetFileName(CurrFileName) + "'", 
											DownPriceResultCode.ErrorProcess, 
											Path.GetFileName(CurrFileName), null);
									}
								}
								else
								{
									LogDownloaderPrice("�� ������� ����������� ���� '" + 
										Path.GetFileName(CurrFileName) + "'", 
										DownPriceResultCode.ErrorProcess, 
										Path.GetFileName(CurrFileName), null);
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
			AddressList FromList = new AddressList();
			bool SenderFound = false;

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
                foreach (MailboxAddress a in m.MainEntity.From.Mailboxes)
                {
                    //���������, ��� ����� ���-�� ��������
					if (!String.IsNullOrEmpty(a.EmailAddress))
                    {
						// ������� ���������� �����
						string correctAddress = GetCorrectEmailAddress(a.EmailAddress);
						// ���� ����� ���� �������� ����� �������� EMail-�������, �� ��������� � ������
						if (IsMailAddress(correctAddress))
						{
							FromList.Add(new MailboxAddress(correctAddress));
							if (!String.IsNullOrEmpty(senderAddress) && 
								senderAddress.Equals(correctAddress, StringComparison.OrdinalIgnoreCase))
								SenderFound = true;
						}
                    }
                }
            }

			if (!String.IsNullOrEmpty(senderAddress) && !SenderFound)
				FromList.Add(new MailboxAddress(senderAddress));

			// ������ ������ ������� ����������� ���� - ����, 
			// � ���� ������ ������� ������ ���������, ����� ��� ���� � �������
            if (m.MainEntity.To == null)
                m.MainEntity.To = new AddressList();

			return FromList;
		}

		protected bool CheckFile()
		{
			var fileName = CurrFileName;
			var tempExtractDir = CurrFileName + ExtrDirSuffix;
			//�������� �� ��������� ���� ����������, ���� ���, �� ������������ �� �����
			if (ArchiveHelper.IsArchive(fileName))
			{
				if (ArchiveHelper.TestArchive(fileName))
				{
					try
					{
						FileHelper.ExtractFromArhive(fileName, tempExtractDir);
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
			string ShortFileName = GetShortFileNameFromAttachement(ent);
			CurrFileName = DownHandlerPath + ShortFileName;
			using (FileStream fs = new FileStream(CurrFileName, FileMode.Create))
			{
				ent.DataToStream(fs);
				fs.Close();
			}
			return ShortFileName;			 
		}

		protected string GetShortFileNameFromAttachement(MimeEntity ent)
		{
			var shortFileName = String.Empty;
			// � ��������� ������� ContentDisposition_FileName �� ���������,
			// ����� ������� �� ContentType_Name
			if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName))
				shortFileName = Path.GetFileName(
					FileHelper.NormalizeFileName(ent.ContentDisposition_FileName));
			else
				shortFileName = Path.GetFileName
					(FileHelper.NormalizeFileName(ent.ContentType_Name));
			return shortFileName;
		}

    }
}
