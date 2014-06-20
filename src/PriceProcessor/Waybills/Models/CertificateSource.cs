﻿using System;
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

		public CertificateSource()
		{
			Suppliers = new List<Supplier>();
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("FtpSupplierId")]
		public virtual Supplier FtpSupplier { get; set; }

		[Property]
		public string SourceClassName { get; set; }

		[Property]
		public bool SearchInAssortmentPrice { get; set; }

		[Property]
		public DateTime? LastDecodeTableDownload { get; set; }

		/// <summary>
		/// Корневой url для поиска сертификатов
		/// </summary>
		[Property]
		public string LookupUrl { get; set; }

		/// <summary>
		/// Url для поиска таблицы перекодировки, таблица должна быть в формате dbf
		/// </summary>
		[Property]
		public string DecodeTableUrl { get; set; }

		[Property]
		public int Priority { get; set; }

		[Property]
		public bool IsDisabled { get; set; }

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
		void ReadSourceCatalog(CertificateSourceCatalog catalog, DataRow row);
	}
}