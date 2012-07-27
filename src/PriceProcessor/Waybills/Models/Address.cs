using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("Addresses", Schema = "Customers", Lazy = true)]
	public class Address : ActiveRecordLinqBase<Address>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property("Address")]
		public virtual string Name { get; set; }

		[BelongsTo("LegalEntityId")]
		public virtual Org Org { get; set; }

		[Property]
		public virtual bool Enabled { get; set; }
	}

	[ActiveRecord("LegalEntities", Schema = "Billing", Lazy = true)]
	public class Org
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string FullName { get; set; }
	}
}