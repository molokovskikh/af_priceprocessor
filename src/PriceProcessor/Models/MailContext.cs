using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.MySql;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.Mime;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Models
{
	public class MailContext
	{
		public MailContext()
		{
			Recipients = new List<MailRecipient>();
			Users = new Dictionary<User, MailRecipient>();
		}

		public string SHA256MailHash { get; set; }
		public string SupplierEmails { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public List<Supplier> Suppliers { get; set; }
		public List<MailRecipient> Recipients { get; set; }

		public Dictionary<User, MailRecipient> Users { get; set; }

		public uint BodyLength
		{
			get
			{
				if (Body == null)
					return 0;
				return (uint)Body.Length;
			}
		}


		public void ParseRecipients(Mime mime)
		{
			ParseRecipientAddresses(mime.GetRecipients());

			if (Recipients.Count > 0) {
				foreach (var recipient in Recipients) {
					if (recipient.Status == RecipientStatus.Verified) {
						var users = recipient.GetUsers(Suppliers[0].RegionMask);
						if (users.Count > 0) {
							for (int i = users.Count - 1; i > -1; i--) {
								var mails = ActiveRecordLinqBase<MailSendLog>.Queryable.Where(
									log => log.Mail.LogTime > DateTime.Now.AddDays(-1) && log.Mail.SHA256Hash == SHA256MailHash && log.User.Id == users[i].Id).ToList();
								if (mails.Count > 0)
									users.RemoveAt(i);
							}
							if (users.Count > 0)
								users.ForEach(u => AddUser(u, recipient));
							else
								recipient.Status = RecipientStatus.Duplicate;
						}
						else
							recipient.Status = RecipientStatus.NotAvailable;
					}
				}
			}
		}

		private void ParseRecipientAddresses(string[] emails)
		{
			// Пробегаемся по всем адресам TO и ищем адрес вида
			// <\d+@docs.analit.net> или <\d+@docs.analit.net>
			foreach (var mail in emails) {
				var recipient = MailRecipient.Parse(mail);
				if (recipient != null)
					AddRecipient(recipient);
			}
		}

		public void AddRecipient(MailRecipient recipient)
		{
			if (!Recipients.Exists(r => r.Equals(recipient)))
				Recipients.Add(recipient);
		}

		public void AddUser(User user, MailRecipient recipient)
		{
			if (!Users.Keys.Any(u => u.Id == user.Id))
				Users.Add(user, recipient);
		}

		public List<MailRecipient> VerifiedRecipients
		{
			get { return Recipients.Where(r => r.Status == RecipientStatus.Verified).ToList(); }
		}

		public List<MailRecipient> DiscardedRecipients
		{
			get { return Recipients.Where(r => r.Status != RecipientStatus.Verified && r.Status != RecipientStatus.Duplicate).ToList(); }
		}

		public bool IsMailFromVipSupplier
		{
			get { return Suppliers.Count > 0 && Suppliers[0].Payer == TemplateHolder.Values.VIPMailPayerId; }
		}

		public string GetCauseList()
		{
			return DiscardedRecipients.Select(r => r.Email + " : " + r.Status.GetDescription()).Implode("\r\n");
		}

		public void ParseMime(Mime mime, AddressList fromSupplierList)
		{
			SHA256MailHash = mime.GetSHA256Hash();
			Subject = mime.MainEntity.Subject;
			SupplierEmails = fromSupplierList.Mailboxes.Select(mailbox => mailbox.EmailAddress).Implode();
			if (fromSupplierList.Mailboxes.Length == 0)
				throw new FromParseException(String.Format("Не смогли разобрать список отправителей письма для сопоставления с поставщиками:\r\n{0}", mime.MainEntity.HeaderString));
			Suppliers = GetSuppliersFromList(fromSupplierList.Mailboxes);

			Body = mime.BodyText;
			if (String.IsNullOrWhiteSpace(Body))
				Body = mime.HtmlToText();
		}

		private List<Supplier> GetSuppliersFromList(MailboxAddress[] mailboxes)
		{
			var dtSuppliers = new DataTable();
			var mails = mailboxes.Select(m => "'" + m.EmailAddress + "'").Implode();

			using (var connection = new MySqlConnection(ConnectionHelper.DefaultConnectionStringName)) {
				connection.Open();
				var adapter = new MySqlDataAdapter(@"
select
  s.Id
from
  Customers.Suppliers s
  inner join contacts.contact_groups cg on cg.ContactGroupOwnerId = s.ContactGroupOwnerId and cg.Type = 10
  inner join contacts.contacts c on c.ContactOwnerId = cg.Id and c.Type = 0
where
  c.ContactText in (" + mails + @") group by s.Id union
select
  s.Id
from
  Customers.Suppliers s
  inner join contacts.contact_groups cg on cg.ContactGroupOwnerId = s.ContactGroupOwnerId and cg.Type = 10
  join contacts.Persons p on p.ContactGroupId = cg.id
  join contacts.contacts c on c.ContactOwnerId = p.Id and c.Type = 0
where
  c.ContactText in ("
					+ mails + ") group by s.Id", connection);
				adapter.Fill(dtSuppliers);
			}
			var result = new List<Supplier>();
			using (new SessionScope()) {
				foreach (DataRow dataRow in dtSuppliers.Rows) {
					var id = Convert.ToUInt32(dataRow["Id"]);
					if (!result.Select(s => s.Id).Contains(id))
						result.Add(ActiveRecordBase<Supplier>.Find(id));
				}
			}

			return result;
		}

		public bool IsValidExtension(string extension)
		{
			return TemplateHolder.Values.ExtensionAllow(extension)
				|| (IsMailFromVipSupplier && extension.Match(".html"));
		}
	}
}