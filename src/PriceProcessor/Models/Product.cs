using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Products", Schema = "Catalogs")]
	public class Product : ActiveRecordLinqBase<Product>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("CatalogId")]
		public virtual Catalog CatalogProduct { get; set; }

		[Property]
		public virtual bool Hidden { get; set; }
	}	
}