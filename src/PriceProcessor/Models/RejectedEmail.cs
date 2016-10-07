using System;
using System.ComponentModel;
using Castle.ActiveRecord;

namespace Inforoom.PriceProcessor.Models
{
	public enum RejectedMessageType
	{
		[Description("Неизвестный")] Unknown,
		[Description("Накладная")] Waybills,
		[Description("Отказ")] Reject,
		[Description("Прайс-лист")] Price,
		[Description("Мини-почта")] MiniMail
	}

	[ActiveRecord("EmailRejectLogs", Schema = "logs", SchemaAction = "none")]
	public class RejectedEmail
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime LogTime { get; set; }

		[Property]
		public virtual uint SmtpId { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property(Column = "`From`")]
		public virtual string From { get; set; }

		[Property(Column = "`To`")]
		public virtual string To { get; set; }

		[Property(Column = "`MessageType`")]
		public virtual RejectedMessageType MessageType { get; set; }

		[Property]
		public virtual string Subject { get; set; }
	}
}