using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Clients", Schema = "Customers")]
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

		[Property("Status")]
		public virtual bool Enabled { get; set; }

		[Property]
		public virtual ulong MaskRegion { get; set; }

		[HasMany(ColumnKey = "ClientId", Inverse = true, Lazy = true, Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<User> Users { get; set; }
	}
}