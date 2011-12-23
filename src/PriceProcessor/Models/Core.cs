using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Waybills;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Core0", Schema = "Farm")]
	public class Core : ActiveRecordLinqBase<Core>
	{
		[PrimaryKey]
		public ulong Id { get; set; }

		[Property]
		public string Quantity { get; set; }

		[BelongsTo("PriceCode")]
		public Price Price { get; set; }

		[BelongsTo("SynonymCode")]
		public SynonymProduct ProductSynonym { get; set; }

		[BelongsTo("SynonymFirmCrCode")]
		public SynonymFirm ProducerSynonym { get; set; }

		[Property]
		public int? ProductId { get; set; }

		[BelongsTo("ProductId")]
		public Product Product { get; set; }

		[Property]
		public int? CodeFirmCr { get; set; }

		[Property]
		public string Code { get; set; }

		[Property]
		public string CodeCr { get; set; }
	}
}