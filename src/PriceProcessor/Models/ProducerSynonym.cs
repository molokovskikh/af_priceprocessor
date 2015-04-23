using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Formalizer;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("SynonymFirmCr", Schema = "farm")]
	public class ProducerSynonym : ActiveRecordLinqBase<ProducerSynonym>
	{
		public ProducerSynonym()
		{
		}

		public ProducerSynonym(string synonym)
		{
			Synonym = synonym;
		}

		/// <summary>
		/// Id Синонима. Ключевое поле.
		/// </summary>
		[PrimaryKey]
		public int SynonymFirmCrCode { get; set; }

		/// <summary>
		/// Синоним производителя
		/// </summary>
		[Property]
		public string Synonym { get; set; }

		/// <summary>
		/// Прайс-лист
		/// </summary>
		[BelongsTo("PriceCode")]
		public Price Price { get; set; }

		/// <summary>
		/// Код производителя ProducerId
		/// </summary>
		[BelongsTo("CodeFirmCr")]
		public Producer Producer { get; set; }

		/// <summary>
		/// Код, присвоенный поставщиком
		/// </summary>
		[Property]
		public string SupplierCode { get; set; }

		[Property]
		public string Canonical { get; set; }
	}
}