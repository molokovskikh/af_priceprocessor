using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("MailSendLogs", Schema = "Logs")]
	public class MailSendLog : ActiveRecordLinqBase<MailSendLog>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("UserId")]
		public User User { get; set; }

		[BelongsTo("MailId")]
		public Mail Mail { get; set; }
	}
}