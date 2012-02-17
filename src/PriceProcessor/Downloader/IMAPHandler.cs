using System;
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
		void Ping();
	}

	public class IMAPHandler
	{
		public IIMAPReader ImapReader { get; private set; }

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

		public void ProcessIMAPFolder()
		{
			using (var imapClient = new IMAP_Client())
			{
				_imapClient = imapClient;
				ConnectToIMAP();

				try
				{
					IMAP_FetchItem[] items;
					do
					{
						ImapReader.Ping();

						items = FetchUIDs();

						ImapReader.Ping();

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

								ImapReader.Ping();
								ImapReader.ProcessMime(Message);
							}
							catch (Exception ex)
							{
								Message = null;
								ImapReader.ProcessBrokenMessage(item, OneItem, ex);
								
								//_logger.Error("Ошибка при обработке письма", ex);
								//_message = null;
								//SendBrokenMessage(item, OneItem, ex);
							}
						}

						//Производим удаление писем
						var sequence = new IMAP_SequenceSet();

						sequence.Parse(String.Join(",", items.Select(i => i.UID.ToString()).ToArray()), long.MaxValue);
						imapClient.DeleteMessages(sequence, true);
					}
					while (items != null && items.Length > 0);
				}
				finally
				{
					Message = null;
				}
			}
		}
 
	}
}