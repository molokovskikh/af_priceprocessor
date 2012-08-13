using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Log", Schema = "Analit")]
	public class Log
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime? Date { get; set; }

		[Property]
		public virtual string Message { get; set; }

		[Property]
		public virtual string Exception { get; set; }

		[Property]
		public virtual string Source { get; set; }
	}
}
