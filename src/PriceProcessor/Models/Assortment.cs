using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Assortment", Schema = "Catalogs")]
	public class Assortment : ActiveRecordLinqBase<Assortment>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("CatalogId")]
		public Catalog Catalog { get; set; }

		[Property]
		public uint ProducerId { get; set; }

		[Property]
		public bool Checked { get; set; }
	}
}