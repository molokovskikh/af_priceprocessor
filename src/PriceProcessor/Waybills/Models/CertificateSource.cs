using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("CertificateSources", Schema = "documents")]
	public class CertificateSource: ActiveRecordLinqBase<CertificateSource>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("FtpSupplierId")]
		public virtual Supplier FtpSupplier { get; set; }
		
		[Property]
		public string SourceClassName { get; set; }

		[HasAndBelongsToMany(typeof (Supplier),
			Lazy = true,
			ColumnKey = "CertificateSourceId",
			Table = "SourceSuppliers",
			Schema = "Documents",
			ColumnRef = "SupplierId")]
		public virtual IList<Supplier> Suppliers { get; set; }

		public ICertificateSource CertificateSourceParser;

		[Property]
		public DateTime? FtpFileDate { get; set; }

	}
}