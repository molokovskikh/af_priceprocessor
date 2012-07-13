using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("FormLogs", Schema = "Logs")]
	public class FormLog : ActiveRecordLinqBase<FormLog>
	{
		[PrimaryKey]
		public virtual uint RowId { get; set; }

		[Property]
		public virtual DateTime? LogTime { get; set; }

		[Property]
		public virtual uint? PriceItemId { get; set; }

		[Property]
		public virtual string Host { get; set; }

		[Property]
		public virtual int? ResultId { get; set; }

		[Property]
		public virtual uint? Form { get; set; }

		[Property]
		public virtual uint? UnForm { get; set; }

		[Property]
		public virtual uint? Zero { get; set; }

		[Property]
		public virtual uint? Forb { get; set; }

		[Property]
		public virtual uint? TotalSecs { get; set; }

		[Property]
		public virtual string Addition { get; set; }

		[Property]
		public virtual uint? DownloadId { get; set; }
	}
}
