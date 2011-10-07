using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("CertificateSources", Schema = "documents")]
	public class CertificateSource: ActiveRecordLinqBase<CertificateSource>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("SourceSupplierId")]
		public virtual Supplier SourceSupplier { get; set; }
		
		[Property]
		public string SourceClassName { get; set; }

		[HasAndBelongsToMany(typeof (Supplier),
			Lazy = true,
			ColumnKey = "CertificateSourceId",
			Table = "SourceSuppliers",
			Schema = "Documents",
			ColumnRef = "SupplierId")]
		public virtual IList<Supplier> Suppliers { get; set; }
		
	}
}