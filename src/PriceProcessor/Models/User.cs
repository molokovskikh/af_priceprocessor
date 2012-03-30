using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Users", Schema = "Customers")]
	public class User : ActiveRecordLinqBase<User>
	{
		public User()
		{
			AvaliableAddresses = new List<Address>();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }
		
		[BelongsTo("ClientId", Lazy = FetchWhen.OnInvoke)]
		public virtual Client Client { get; set; }

		[Property]
		public virtual ulong WorkRegionMask { get; set; }

		[HasAndBelongsToMany(typeof (Address),
			Lazy = true,
			ColumnKey = "UserId",
			Schema = "Customers",
			Table = "UserAddresses",
			ColumnRef = "AddressId")]
		public virtual IList<Address> AvaliableAddresses { get; set; }

		[Property]
		public virtual bool Enabled { get; set; }
	}

}