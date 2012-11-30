using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("PriceItems", Schema = "Usersettings")]
	public class PriceItem
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime PriceDate { get; set; }

		[Property]
		public virtual DateTime LastDownload { get; set; }

		[Property]
		public virtual DateTime LastDownloadDate { get; set; }
	}
}
