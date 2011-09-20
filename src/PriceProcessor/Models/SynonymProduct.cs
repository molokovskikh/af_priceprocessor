using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Formalizer;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Synonym", Schema = "farm")]
	public class SynonymProduct : ActiveRecordLinqBase<SynonymProduct>
	{
		/// <summary>
		/// Id Синонима. Ключевое поле.
		/// </summary>
		[PrimaryKey]
		public int SynonymCode { get; set; }

		/// <summary>
		/// Продукт
		/// </summary>
		[BelongsTo("ProductId")]
		public Product Product { get; set; }

		/// <summary>
		/// Уцененный
		/// </summary>
		[Property]
		public bool Junk { get; set; }

		/// <summary>
		/// Синоним продукта
		/// </summary>
		[Property]
		public string Synonym { get; set; }

		/// <summary>
		/// Прайс-лист
		/// </summary>		
		[BelongsTo("PriceCode")]
		public Price Price { get; set; }
	}
}