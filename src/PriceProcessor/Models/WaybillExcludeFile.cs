using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("WaybillExcludeFile", Schema = "usersettings")]
	public class WaybillExcludeFile
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Mask { get; set; }

		[BelongsTo]
		public virtual Supplier Supplier { get; set; }
	}

	[ActiveRecord("waybilldirtyfile", Schema = "usersettings")]
	public class WaybillDirtyFile
	{
		public WaybillDirtyFile()
		{
		}

		public WaybillDirtyFile(Supplier supplier, string file, string mask)
		{
			Date = DateTime.Now;
			Supplier = supplier;
			File = file;
			Mask = mask;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual Supplier Supplier { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[Property]
		public virtual string File { get; set; }

		[Property]
		public virtual string Mask { get; set; }
	}
}
