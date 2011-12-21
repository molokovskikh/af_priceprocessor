using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Clients", Schema = "Future")]
	public class Client : ActiveRecordLinqBase<Client>
	{
		public Client()
		{
			Users = new List<User>();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }
		
		[HasMany(ColumnKey = "ClientId", Inverse = true, Lazy = true, Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<User> Users { get; set; }
	}

}