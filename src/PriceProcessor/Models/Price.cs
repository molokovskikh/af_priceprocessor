using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("PricesData", Schema = "Usersettings", DynamicUpdate = true)]
	public class Price : ActiveRecordLinqBase<Price>
	{
		[PrimaryKey("PriceCode")]
		public virtual uint Id { get; set; }

		[BelongsTo("FirmCode")]
		public virtual Supplier Supplier { get; set; }

		[Property]
		public virtual string PriceName { get; set; }

		[Property]
		public virtual uint? ParentSynonym { get; set; }

		[Property]
		public virtual bool IsRejects { get; set; }

		[Property]
		public virtual bool IsRejectCancellations { get; set; }

		[HasMany(ColumnKey = "PriceCode", Inverse = true)]
		public virtual IList<PriceCost> Costs { get; set; }

		[HasMany(ColumnKey = "PriceCode", Cascade = ManyRelationCascadeEnum.AllDeleteOrphan, Lazy = true)]
		public virtual IList<ProductSynonym> ProductSynonyms { get; set; }

		[HasMany(ColumnKey = "PriceCode", Cascade = ManyRelationCascadeEnum.AllDeleteOrphan, Lazy = true)]
		public virtual IList<ProducerSynonym> ProducerSynonyms { get; set; }
	}
}