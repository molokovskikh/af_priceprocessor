using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class RostaCertificateSource : ICertificateSource
	{
		public bool CertificateExists(DocumentLine line)
		{
			var catalog = CertificateSourceCatalog.Queryable
				.Where(
					c =>
					c.CertificateSource.SourceClassName == this.GetType().Name && c.SerialNumber == line.SerialNumber &&
					c.CatalogProduct.Id == line.ProductEntity.CatalogProduct.Id)
				.FirstOrDefault();
			return catalog != null;
		}

		public IList<CertificateFile> GetCertificateFiles(CertificateTask task)
		{
			var result = new List<CertificateFile>();

			var catalogs = CertificateSourceCatalog.Queryable
				.Where(
					c =>
					c.CertificateSource.Id == task.CertificateSource.Id
					&& c.SerialNumber == task.DocumentLine.SerialNumber
					&& c.CatalogProduct.Id == task.DocumentLine.ProductEntity.CatalogProduct.Id)
				.ToList();

			if (catalogs.Count > 0) {
				var tempDowloadDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
				if (!Directory.Exists(tempDowloadDir))
					Directory.CreateDirectory(tempDowloadDir);

				var downloader = new FtpDownloader();

				foreach (var certificateSourceCatalog in catalogs) {
					var dirName = ExtractFtpDir(certificateSourceCatalog.OriginFilePath);
					var fileName = ExtractFileName(certificateSourceCatalog.OriginFilePath);

					var downloadFiles = downloader.GetFilesFromSource(
						Settings.Default.RostaCertificateFtp,
						21,
						dirName,
						Settings.Default.RostaCertificateFtpUserName,
						Settings.Default.RostaCertificateFtpPassword,
						fileName,
						DateTime.MinValue,
						tempDowloadDir);

					if (downloadFiles.Count > 0)
						result.Add(new CertificateFile(
							downloadFiles[0].FileName,
							certificateSourceCatalog.OriginFilePath,
							fileName,
							task.CertificateSource));

				}

			}

			return result;
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

	}
}