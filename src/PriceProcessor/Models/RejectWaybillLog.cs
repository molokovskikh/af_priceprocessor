using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Models
{
	public enum RejectReasonType
	{
		[Description("Нет причины")] NoReason = 0,
		[Description("Адрес отключен")] AddressDisable = 1,
		[Description("Клиент отключен")] ClientDisable = 2,
		[Description("Адрес не доступен поставщику")] AddressNoAvailable = 3,
		[Description("Пользователь не обновлялся более месяца")] UserNotUpdate = 4
	}

	[ActiveRecord(Schema = "Logs", Lazy = true)]
	public class RejectWaybillLog
	{
		public RejectWaybillLog()
		{
		}

		public RejectWaybillLog(DocumentReceiveLog documentLog)
		{
			Supplier = documentLog.Supplier;
			ClientCode = documentLog.ClientCode;
			Address = documentLog.Address;
			LogTime = DateTime.Now;
			FileName = documentLog.FileName;
			Addition = documentLog.Comment;
			DocumentSize = documentLog.DocumentSize;
			RejectReason = documentLog.RejectReason;
		}

		[PrimaryKey("RowId")]
		public virtual uint Id { get; set; }

		[BelongsTo("FirmCode")]
		public virtual Supplier Supplier { get; set; }

		[Property]
		public virtual uint? ClientCode { get; set; }

		[BelongsTo("AddressId")]
		public virtual Address Address { get; set; }

		[Property]
		public virtual DateTime LogTime { get; set; }

		[Property]
		public virtual string FileName { get; set; }

		[Property]
		public virtual string Addition { get; set; }

		[Property]
		public virtual long? DocumentSize { get; set; }

		[Property]
		public virtual RejectReasonType RejectReason { get; set; }
	}
}
