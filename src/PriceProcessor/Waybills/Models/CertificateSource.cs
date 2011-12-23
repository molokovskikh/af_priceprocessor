using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("CertificateSources", Schema = "documents")]
	public class CertificateSource : ActiveRecordLinqBase<CertificateSource>
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

		public ICertificateSource GetCertificateSource()
		{
			var sourceClassName = SourceClassName;
			Type result = null;
			var types = Assembly.GetExecutingAssembly()
				.GetModules()[0]
				.FindTypes(Module.FilterTypeNameIgnoreCase, sourceClassName);
			if (types.Length > 1)
				throw new Exception(String.Format("Найдено более одного типа с именем {0}", sourceClassName));
			if (types.Length == 1)
				result = types[0];
			if (result == null)
				throw new Exception(String.Format("Класс {0} не найден", sourceClassName));
			return (ICertificateSource)Activator.CreateInstance(result);
		}
	}

	public interface IRemoteFtpSource
	{
		string FtpHost { get; }
		string FtpDir { get; }
		string FtpUser { get;  }
		string FtpPassword { get; }
		string Filename { get; }
		void ReadSourceCatalog(CertificateSourceCatalog catalog, DataRow row);
	}
}