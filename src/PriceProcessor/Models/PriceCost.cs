using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("PricesCosts", Schema = "Usersettings")]
	public class PriceCost : ActiveRecordLinqBase<PriceCost>
	{
		[PrimaryKey("CostCode")]
		public virtual uint Id { get; set;  }

		[BelongsTo("PriceCode")]
		public virtual Price Price { get; set; }

		[Property]
		public virtual uint PriceItemId { get; set; }

		[Property]
		public virtual string CostName { get; set; }
	}
}