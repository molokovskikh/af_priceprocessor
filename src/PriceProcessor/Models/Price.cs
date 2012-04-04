using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Formalizer;

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

		[HasMany(ColumnKey = "PriceCode", Inverse = true)]
		public virtual IList<PriceCost> Costs { get; set; }

		[HasMany(ColumnKey = "PriceCode", Cascade = ManyRelationCascadeEnum.AllDeleteOrphan, Lazy = true)]
		public virtual IList<SynonymProduct> ProductSynonyms { get; set; }

		[HasMany(ColumnKey = "PriceCode", Cascade = ManyRelationCascadeEnum.AllDeleteOrphan, Lazy = true)]
		public virtual IList<SynonymFirm> ProducerSynonyms { get; set; }
	}
}