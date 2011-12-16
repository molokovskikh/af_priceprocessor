using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("Mails", Schema = "documents")]
	public class Mail : ActiveRecordLinqBase<Mail>
	{
		public Mail()
		{
			Attachments = new List<Attachment>();
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public DateTime LogTime { get; set; }

		[BelongsTo("SupplierId")]
		public Supplier Supplier { get; set; }

		[Property]
		public bool IsVIPMail { get; set; }

		[Property]
		public string Subject { get; set; }

		[Property]
		public string Body { get; set; }
		
		[HasMany(ColumnKey = "MailId", Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public virtual IList<Attachment> Attachments { get; set; }

	}
}