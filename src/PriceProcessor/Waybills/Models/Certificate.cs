using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("Certificates", Schema = "documents")]
	public class Certificate : ActiveRecordLinqBase<Certificate>
	{
		public Certificate()
		{
			CertificateFiles = new List<CertificateFile>();
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("CatalogId")]
		public virtual Catalog CatalogProduct { get; set; }

		/// <summary>
		/// Серийный номер продукта
		/// </summary>
		[Property]
		public string SerialNumber { get; set; }

		[HasAndBelongsToMany(typeof (CertificateFile),
			Lazy = false,
			ColumnKey = "CertificateId",
			Table = "FileCertificates",
			Schema = "Documents",
			ColumnRef = "CertificateFileId",
			Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<CertificateFile> CertificateFiles { get; set; }

		public CertificateFile NewFile()
		{
			return NewFile(new CertificateFile());
		}

		public CertificateFile NewFile(CertificateFile file)
		{
			if (CertificateFiles == null)
				CertificateFiles = new List<CertificateFile>();

			file.Certificates.Add(this);
			CertificateFiles.Add(file);
			return file;
		}

	}
}