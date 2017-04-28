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
}
