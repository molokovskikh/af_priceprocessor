using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Models
{
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
	}
}
