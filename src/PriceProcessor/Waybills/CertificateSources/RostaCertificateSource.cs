using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class RostaCertificateSource : FtpCertifcateSource
	{
		/// <summary>
		/// Для тестов
		/// </summary>
		public string TMPDownloadDir { get; set; }
		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var catalogs = CertificateSourceCatalog.Queryable
				.Where(
				c =>
					c.CertificateSource.Id == task.CertificateSource.Id
						&& c.SerialNumber == task.DocumentLine.SerialNumber
						&& c.CatalogProduct.Id == task.DocumentLine.ProductEntity.CatalogProduct.Id)
				.ToList();

			if (catalogs.Count == 0) {
				task.DocumentLine.CertificateError = "Нет записи в таблице перекодировки";
				return;
			}

			var tempDowloadDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			TMPDownloadDir = tempDowloadDir;
			if (!Directory.Exists(tempDowloadDir))
				Directory.CreateDirectory(tempDowloadDir);

			var downloader = new FtpDownloader();
			try {
				foreach (var certificateSourceCatalog in catalogs) {
					var dirName = ExtractFtpDir(certificateSourceCatalog.OriginFilePath);
					var fileName = ExtractFileName(certificateSourceCatalog.OriginFilePath);

					var downloadFiles = downloader.GetFilesFromSource(
						FtpHost,
						FtpPort,
						dirName,
						FtpUser,
						FtpPassword,
						fileName,
						DateTime.MinValue,
						tempDowloadDir);

					if (downloadFiles.Count > 0)
						files.Add(new CertificateFile(
							downloadFiles[0].FileName,
							certificateSourceCatalog.OriginFilePath,
							fileName,
							task.CertificateSource));
				}
			}
			finally {
				if (!String.IsNullOrEmpty(tempDowloadDir)
					&& Directory.Exists(tempDowloadDir)
					&& Directory.GetDirectories(tempDowloadDir).Length == 0
					&& Directory.GetFiles(tempDowloadDir).Length == 0)
					Directory.Delete(tempDowloadDir);
			}
			if (files.Count == 0)
				task.DocumentLine.CertificateError = "Файл сертификата не найден на ftp поставщика";
		}

		private string ExtractFileName(string originFilePath)
		{
			var result = originFilePath;

			if (result.IndexOf("/") > -1)
				result = result.Substring(result.IndexOf("/") + 1);

			return result;
		}

		private string ExtractFtpDir(string originFilePath)
		{
			var result = String.Empty;

			if (originFilePath.IndexOf("/") > -1)
				result = originFilePath.Substring(0, originFilePath.IndexOf("/"));

			return result;
		}

		public string FtpHost
		{
			get { return Settings.Default.RostaCertificateFtp; }
		}

		public int FtpPort
		{
			get { return 21; }
		}

		public string FtpDir
		{
			get { return "LIST"; }
		}

		public string FtpUser
		{
			get { return Settings.Default.RostaCertificateFtpUserName; }
		}

		public string FtpPassword
		{
			get { return Settings.Default.RostaCertificateFtpPassword; }
		}

		public string Filename
		{
			get { return "SERT_LIST.DBF"; }
		}

		public void ReadSourceCatalog(CertificateSourceCatalog catalog, DataRow row)
		{
			catalog.SerialNumber = row["BATCH_ID"].ToString();
			catalog.SupplierCode = row["CODE"].ToString();
			catalog.OriginFilePath = row["GB_FILES"].ToString();
		}
	}
}