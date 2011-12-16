using System;
using System.IO;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("Attachments", Schema = "documents")]
	public class Attachment : ActiveRecordLinqBase<Attachment>
	{
		public Attachment()
		{
		}

		public Attachment(Mail mail, string localFileName)
		{
			Mail = mail;
			LocalFileName = localFileName;
			FileName = Path.GetFileName(LocalFileName);
			Extension = Path.GetExtension(LocalFileName);
			Size = Convert.ToUInt32(new FileInfo(LocalFileName).Length);
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("MailId")]
		public Mail Mail { get; set; }

		[Property]
		public string FileName { get; set; }
		
		[Property]
		public string Extension { get; set; }

		[Property]
		public uint Size { get; set; }

		public string LocalFileName { get; set; }

		public string GetSaveFileName()
		{
			return Id + Extension;
		}
	}
}