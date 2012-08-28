using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("CertificateSources", Schema = "documents")]
	public class CertificateSource : ActiveRecordLinqBase<CertificateSource>
	{
		public static Assembly Assembly = Assembly.GetExecutingAssembly();

		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("FtpSupplierId")]
		public virtual Supplier FtpSupplier { get; set; }

		[Property]
		public string SourceClassName { get; set; }

		[Property]
		public bool SearchInAssortmentPrice { get; set; }

		[Property]
		public DateTime? FtpFileDate { get; set; }

		[Property]
		public int Priority { get; set; }

		[HasAndBelongsToMany(typeof(Supplier),
			Lazy = true,
			ColumnKey = "CertificateSourceId",
			Table = "SourceSuppliers",
			Schema = "Documents",
			ColumnRef = "SupplierId")]
		public virtual IList<Supplier> Suppliers { get; set; }

		public ICertificateSource CertificateSourceParser;

		public ICertificateSource GetCertificateSource()
		{
			return ReflectionHelper.GetDocumentReader<ICertificateSource>(SourceClassName, Assembly);
		}
	}

	public interface IRemoteFtpSource
	{
		string FtpHost { get; }
		int FtpPort { get; }
		string FtpDir { get; }
		string FtpUser { get; }
		string FtpPassword { get; }
		string Filename { get; }
		void ReadSourceCatalog(CertificateSourceCatalog catalog, DataRow row);
	}
}