using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("Mails", Schema = "documents")]
	public class Mail : ActiveRecordLinqBase<Mail>
	{
		public Mail()
		{
			Attachments = new List<Attachment>();
			MailRecipients = new List<MailRecipient>();
		}

		public Mail(MailContext context)
			: this()
		{
			Supplier = context.Suppliers[0];
			SupplierEmail = context.SupplierEmails;
			Subject = context.Subject;
			Body = context.Body;
			LogTime = DateTime.Now;
			SHA256Hash = context.SHA256MailHash;
			IsVIPMail = context.IsMailFromVipSupplier;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public DateTime LogTime { get; set; }

		[BelongsTo("SupplierId")]
		public Supplier Supplier { get; set; }

		[Property]
		public string SupplierEmail { get; set; }

		[Property]
		public bool IsVIPMail { get; set; }

		[Property]
		public string Subject { get; set; }

		[Property]
		public string Body { get; set; }

		[Property]
		public uint Size { get; set; }

		[Property]
		public string SHA256Hash { get; set; }

		[HasMany(ColumnKey = "MailId", Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public virtual IList<Attachment> Attachments { get; set; }

		[HasMany(ColumnKey = "MailId", Inverse = true, Lazy = true, Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<MailRecipient> MailRecipients { get; set; }

	}
}