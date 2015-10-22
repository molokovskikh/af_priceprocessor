﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net.Util;
using NHibernate.Linq;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class AvestaSource : AbstractCertifcateSource, IRemoteFtpSource
	{
		public AvestaSource()
		{
		}

		public AvestaSource(FileCleaner cleaner) : base(cleaner)
		{
		}

		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var maps = Session.Query<CertificateSourceCatalog>()
				.Where(c => c.SerialNumber == task.SerialNumber && c.SupplierCode == task.DocumentLine.Code && c.CertificateSource == task.CertificateSource)
				.ToArray();
			if (maps.Length == 0) {
				task.DocumentLine.CertificateError = "Нет записи в таблице перекодировки";
				return;
			}
			foreach (var map in maps.Where(m => !String.IsNullOrWhiteSpace(m.OriginFilePath))) {
				var path = map.OriginFilePath;
				if (path[0] == Path.DirectorySeparatorChar)
					path = path.Slice(1, -1);
				var file = Path.Combine(new Uri(task.CertificateSource.LookupUrl).LocalPath, path);
				if (File.Exists(file)) {
					var tmp = Cleaner.TmpFile();
					File.Copy(file, tmp, true);
					files.Add(new CertificateFile(tmp, map.OriginFilePath, map.OriginFilePath, task.CertificateSource) {
						Note = map.Note
					});
				}
				else {
					task.DocumentLine.CertificateError = String.Format("Файл сертификата '{0}' не найден на ftp АналитФармация", file);
				}
			}
		}

		public override bool CertificateExists(DocumentLine line)
		{
			if (String.IsNullOrEmpty(line.Code)) {
				line.CertificateError = "Поставщик не указал код";
				return false;
			}
			if (String.IsNullOrEmpty(line.SerialNumber)) {
				line.CertificateError = "Поставщик не указал серию";
				return false;
			}
			return true;
		}

		public void ReadSourceCatalog(CertificateSourceCatalog catalog, DataRow row)
		{
			catalog.SupplierCode = row["matcode"].ToString();
			catalog.SerialNumber = row["seria"].ToString();
			catalog.OriginFilePath = row["risfile"].ToString();
			catalog.Note = row["tipdoc"].ToString();
		}
	}
}