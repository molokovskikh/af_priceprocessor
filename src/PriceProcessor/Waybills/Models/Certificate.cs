using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("Certificates", Schema = "documents")]
	public class Certificate : ActiveRecordLinqBase<Certificate>
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

		[HasMany(ColumnKey = "CertificateId", Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public virtual IList<CertificateFile> CertificateFiles { get; set; }

		public CertificateFile NewFile()
		{
			return NewFile(new CertificateFile());
		}

		public CertificateFile NewFile(CertificateFile file)
		{
			if (CertificateFiles == null)
				CertificateFiles = new List<CertificateFile>();

			file.Certificate = this;
			CertificateFiles.Add(file);
			return file;
		}

	}
}