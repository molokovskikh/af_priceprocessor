using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("AttachmentSendLogs", Schema = "Logs")]
	public class AttachmentSendLog : ActiveRecordLinqBase<AttachmentSendLog>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("UserId")]
		public User User { get; set; }

		[BelongsTo("AttachmentId")]
		public Attachment Attachment { get; set; }
	}
}