using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("OrdersList", Schema = "Orders")]
	public class OrderItem : ActiveRecordLinqBase<OrderItem>
	{
		[PrimaryKey("RowId")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual uint? Quantity { get; set; }

		[Property]
		public virtual ulong? CoreId { get; set; }

		[Property]
		public virtual float? Cost { get; set; }

		[Property]
		public virtual string Code { get; set; }

		[BelongsTo("SynonymCode", NotFoundBehaviour = NotFoundBehaviour.Ignore)]
		public virtual ProductSynonym ProductSynonym { get; set; }

		[BelongsTo("SynonymFirmCrCode", NotFoundBehaviour = NotFoundBehaviour.Ignore)]
		public virtual ProducerSynonym ProducerSynonym { get; set; }

		[BelongsTo("OrderId")]
		public virtual OrderHead Order { get; set; }
	}
}