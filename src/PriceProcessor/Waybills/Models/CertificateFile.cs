using System;
using System.Collections.Generic;
using System.IO;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("CertificateFiles", Schema = "documents")]
	public class CertificateFile : ActiveRecordLinqBase<CertificateFile>
	{
		public CertificateFile()
		{
			Certificates = new List<Certificate>();
		}

		public CertificateFile(string localFile, string externalFileId,
			string originFile = null,
			CertificateSource source = null)
			: this()
		{
			LocalFile =localFile;
			ExternalFileId = externalFileId;
			if (!String.IsNullOrEmpty(originFile))
			{
				OriginFilename = Path.GetFileName(originFile);
				Extension = Path.GetExtension(originFile);
			}
			CertificateSource = source;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[HasAndBelongsToMany(typeof (Certificate),
			Lazy = true,
			Inverse = true,
			ColumnKey = "CertificateFileId",
			Table = "FileCertificates",
			Schema = "Documents",
			ColumnRef = "CertificateId")]
		public virtual IList<Certificate> Certificates { get; set; }

		/// <summary>
		/// Оригинальное имя файла сертификата
		/// </summary>
		[Property]
		public string OriginFilename { get; set; }

		[BelongsTo("CertificateSourceId")]
		public virtual CertificateSource CertificateSource { get; set; }

		[Property]
		public string ExternalFileId { get; set; }

		[Property]
		public string Extension { get; set; }

		public string LocalFile { get; set; }

		public string RemoteFile
		{
			get { return Id + Extension; }
		}
	}
}