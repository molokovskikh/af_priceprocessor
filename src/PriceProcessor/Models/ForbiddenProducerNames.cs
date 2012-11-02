using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("ForbiddenProducers", Schema = "Farm")]
	public class ForbiddenProducerNames
	{
		[PrimaryKey]
		public virtual ulong Id { get; set; }

		[Property]
		public virtual string Name { get; set; }
	}
}
