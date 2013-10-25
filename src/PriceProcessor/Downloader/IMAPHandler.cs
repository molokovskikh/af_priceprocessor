﻿using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
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
		private ILog log = LogManager.GetLogger(typeof(IMAPHandler));

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
			return _imapClient.FetchMessages(sequenceUids, IMAP_FetchItem_Flags.UID, false, false)
				?? Enumerable.Empty<IMAP_FetchItem>().ToArray();
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
			using (var imapClient = new IMAP_Client()) {
				_imapClient = imapClient;
				ConnectToIMAP();

				try {
					var items = Enumerable.Empty<IMAP_FetchItem>().ToArray();
					var toDelete = new List<IMAP_FetchItem>();
					do {
						toDelete.Clear();
						ImapReader.PingReader();
						items = FetchUIDs();
						if (log.IsDebugEnabled) {
							log.DebugFormat("Получено {0} UIDs", items.Length);
						}
						//обрабатываем мисьма пачками что бы уменьшить вероятность появления дублей
						//при остановке шатной или аварийной остановке
						items = items.Take(100).ToArray();
						ImapReader.PingReader();

						foreach (var item in items) {
							if (log.IsDebugEnabled) {
								log.DebugFormat("Обработка {0} UID", item.UID);
							}

							IMAP_FetchItem[] OneItem = null;
							try {
								OneItem = FetchMessages(item.UID);

								Message = Mime.Parse(OneItem[0].MessageData);

								CurrentUID = item.UID;

								ImapReader.PingReader();
								ImapReader.ProcessMime(Message);
								toDelete.Add(item);
							}
							catch (Exception ex) {
								if (log.IsDebugEnabled) {
									log.Debug(String.Format("Не удалось обработать письмо {0} UID", item.UID), ex);
								}
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

							sequence.Parse(String.Join(",", toDelete.Select(i => i.UID.ToString())), long.MaxValue);
							imapClient.DeleteMessages(sequence, true);
						}
					} while (items.Length > 0 && toDelete.Count > 0);
				}
				finally {
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