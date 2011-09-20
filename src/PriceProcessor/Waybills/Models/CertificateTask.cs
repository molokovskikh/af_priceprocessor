using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("CertificateTasks", Schema = "documents")]
	public class CertificateTask : ActiveRecordLinqBase<CertificateTask>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("SupplierId")]
		public virtual Supplier Supplier { get; set; }

		[BelongsTo("CatalogId")]
		public virtual Catalog CatalogProduct { get; set; }

		/// <summary>
		/// Серийный номер продукта
		/// </summary>
		[Property]
		public string SerialNumber { get; set; }

		[BelongsTo("DocumentBodyId")]
		public virtual DocumentLine DocumentLine { get; set; }

		public override string ToString()
		{
			return string.Format(
				"CertificateTask Id: {0},  Supplier: {1},  Catalog: {2},  SerialNumber: {3},  DocumentBodyId: {4}", 
				Id, 
				Supplier != null ? Supplier.Id.ToString() : "null",
				CatalogProduct != null ? CatalogProduct.Id.ToString() : "null",
				SerialNumber,
				DocumentLine != null ? DocumentLine.Id.ToString() : "null"
			);
		}
		
	}
}