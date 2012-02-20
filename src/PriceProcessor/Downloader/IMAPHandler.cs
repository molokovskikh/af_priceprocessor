using System;
using System.Collections.Generic;
using System.Linq;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;

namespace Inforoom.PriceProcessor.Downloader
{
	public interface IIMAPReader
	{
		void IMAPAuth(IMAP_Client client);
		void ProcessMime(Mime mime);
		void ProcessBrokenMessage(IMAP_FetchItem item, IMAP_FetchItem[] oneItem, Exception ex);
		void PingReader();
	}

	public class UIDInfo
	{
		public virtual int UID { get; private set; }

		public virtual DateTime CreateTime { get; private set; }

		public UIDInfo(int uid)
		{
			UID = uid;
			CreateTime = DateTime.Now;
		}
	}

	public class IMAPHandler
	{
		public IIMAPReader ImapReader { get; private set; }

		public List<UIDInfo> ErrorInfos { get; set; }

		//Текущее обрабатываемое письмо
		public Mime Message { get; private set; }

		//UID текущего обрабатываемого письма
		public int CurrentUID { get; private set; }

		private IMAP_Client _imapClient;

		public IMAPHandler(IIMAPReader imapReader)
		{
			if (imapReader == null)
				throw new ArgumentNullException("imapReader");
			ImapReader = imapReader;
			ErrorInfos = new List<UIDInfo>();
		}

		public void ConnectToIMAP()
		{
			_imapClient.Connect(Settings.Default.IMAPHost, 143);
			ImapReader.IMAPAuth(_imapClient);
			_imapClient.SelectFolder(Settings.Default.IMAPSourceFolder);
		}

		public IMAP_FetchItem[] FetchUIDs()
		{
			var sequenceUids = new IMAP_SequenceSet();
			sequenceUids.Parse("1:*", long.MaxValue);
			return _imapClient.FetchMessages(sequenceUids, IMAP_FetchItem_Flags.UID, false, false);
		}

		public IMAP_FetchItem[] FetchMessages(int uid)
		{
			var sequenceMessages = new IMAP_SequenceSet();
			sequenceMessages.Parse(uid.ToString(), long.MaxValue);
			return _imapClient.FetchMessages(sequenceMessages, IMAP_FetchItem_Flags.Message, false, true);
		}

		public bool UIDTimeout(UIDInfo info)
		{
			return DateTime.Now.Subtract(info.CreateTime).TotalMinutes > Settings.Default.UIDProcessTimeout;
		}

		public void ProcessIMAPFolder()
		{
			using (var imapClient = new IMAP_Client())
			{
				_imapClient = imapClient;
				ConnectToIMAP();

				try
				{
					IMAP_FetchItem[] items;
					var toDelete = new List<IMAP_FetchItem>();
					do
					{
						toDelete.Clear();

						ImapReader.PingReader();

						items = FetchUIDs();

						ImapReader.PingReader();

						if (items == null || items.Length == 0)
							continue;

						foreach (var item in items)
						{
							IMAP_FetchItem[] OneItem = null;
							try
							{
								OneItem = FetchMessages(item.UID);

								Message = Mime.Parse(OneItem[0].MessageData);

								CurrentUID = item.UID;

								ImapReader.PingReader();
								ImapReader.ProcessMime(Message);
								toDelete.Add(item);
							}
							catch (Exception ex)
							{
								Message = null;
								var errorInfo = GetErrorInfo(item.UID);
								if (UIDTimeout(errorInfo)) {
									ErrorInfos.Remove(errorInfo);
									toDelete.Add(item);
									ImapReader.ProcessBrokenMessage(item, OneItem, ex);
								}
							}
						}

						//Производим удаление писем
						if (toDelete.Count > 0) {
							var sequence = new IMAP_SequenceSet();

							sequence.Parse(String.Join(",", toDelete.Select(i => i.UID.ToString()).ToArray()), long.MaxValue);
							imapClient.DeleteMessages(sequence, true);
						}
					}
					while (items != null && items.Length > 0 && toDelete.Count > 0);
				}
				finally
				{
					Message = null;
				}
			}
		}

		private UIDInfo GetErrorInfo(int uid)
		{
			var info = ErrorInfos.FirstOrDefault(i => i.UID == uid);
			if (info == null) {
				info = new UIDInfo(uid);
				ErrorInfos.Add(info);
			}
			return info;
		}
	}
}