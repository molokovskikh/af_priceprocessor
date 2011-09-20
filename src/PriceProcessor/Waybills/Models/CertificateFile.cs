using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("CertificateFiles", Schema = "documents")]
	public class CertificateFile: ActiveRecordLinqBase<CertificateFile>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("CertificateId")]
		public virtual Certificate Certificate { get; set; }

		/// <summary>
		/// Оригинальное имя файла сертификата
		/// </summary>
		[Property]
		public string OriginFilename { get; set; }

		[BelongsTo("SupplierId")]
		public virtual Supplier Supplier { get; set; }
	}
}