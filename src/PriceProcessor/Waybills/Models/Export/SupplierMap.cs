using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models.Export
{
	[ActiveRecord(Schema = "Documents")]
	public class SupplierMap
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("SupplierId")]
		public virtual Supplier Supplier { get; set; }

		[Property]
		public virtual string Name { get; set; }
	}
}