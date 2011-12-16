using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Users", Schema = "future")]
	public class User : ActiveRecordLinqBase<User>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }
		
	}

}