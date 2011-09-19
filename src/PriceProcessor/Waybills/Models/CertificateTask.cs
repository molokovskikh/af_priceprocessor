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

		[BelongsTo("CatalogId")]
		public virtual Catalog CatalogProduct { get; set; }

		/// <summary>
		/// Серийный номер продукта
		/// </summary>
		[Property]
		public string SerialNumber { get; set; }

		[BelongsTo("DocumentBodyId")]
		public virtual DocumentLine DocumentLine { get; set; }
		
	}
}