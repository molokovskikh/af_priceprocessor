using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("CertificateSourceCatalogs", Schema = "documents")]
	public class CertificateSourceCatalog : ActiveRecordLinqBase<CertificateSourceCatalog>
	{
		[PrimaryKey]
		public uint Id { get; set; }
		
		[BelongsTo("CertificateSourceId")]
		public virtual CertificateSource CertificateSource { get; set; }

		[BelongsTo("CatalogId")]
		public virtual Catalog CatalogProduct { get; set; }

		[Property]
		public string SerialNumber { get; set; }

		/// <summary>
		/// Код продукта поставщика
		/// </summary>
		[Property]
		public string SupplierCode { get; set; }

		/// <summary>
		/// Путь к файлу сертификата поставщика
		/// </summary>
		[Property]
		public string OriginFilePath { get; set; }

	}
}