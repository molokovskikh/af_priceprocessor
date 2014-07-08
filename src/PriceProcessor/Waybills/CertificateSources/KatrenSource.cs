using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using Common.Tools;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.FTP.Client;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class KatrenSource : FtpCertifcateSource, IRemoteFtpSource
	{
		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var catalogId = task.DocumentLine.ProductEntity.CatalogProduct.Id;
			var catalogs = GetSourceCatalog(catalogId, task.SerialNumber);

			if (catalogs.Count == 0) {
				task.DocumentLine.CertificateError = "Нет записи в таблице перекодировки";
				return;
			}

			foreach (var certificateSourceCatalog in catalogs) {
				var filename = certificateSourceCatalog.OriginFilePath;
				var mask = String.Format("{0}*{1}", Path.GetFileNameWithoutExtension(filename), Path.GetExtension(filename));

				var uri = new Uri(task.CertificateSource.LookupUrl);
				var dir = Path.Combine(Path.GetDirectoryName(uri.AbsolutePath),
					Path.GetDirectoryName(certificateSourceCatalog.OriginFilePath));
				using (var ftpClient = new FTP_Client()) {
					ftpClient.PassiveMode = true;
					ftpClient.Connect(uri.Host, uri.Port);
					var credentials = Util.GetCredentials(uri);
					if (credentials != null)
						ftpClient.Authenticate(credentials.UserName, credentials.Password);
					ftpClient.SetCurrentDir(dir);
					var ftpFiles = ftpClient.GetList();
					var filesToDownload = ftpFiles.Tables["DirInfo"]
						.AsEnumerable()
						.Where(r => !Convert.ToBoolean(r["IsDirectory"]))
						.Select(r => r["Name"].ToString())
						.Where(n => FileHelper.CheckMask(n, mask))
						.ToList();
					foreach (var file in filesToDownload) {
						var tempFileName = Path.GetTempFileName();
						ftpClient.ReceiveFile(file, tempFileName);
						files.Add(new CertificateFile(
							tempFileName,
							certificateSourceCatalog.OriginFilePath,
							file,
							task.CertificateSource));
					}
				}
			}

			if (files.Count == 0)
				task.DocumentLine.CertificateError = "Файл сертификата не найден на ftp поставщика";
		}

		public void ReadSourceCatalog(CertificateSourceCatalog catalog, DataRow row)
		{
			catalog.SupplierCode = row["GOODID"].ToString();
			catalog.SupplierProducerCode = row["VENDORID"].ToString();
			catalog.SerialNumber = row["SERIA"].ToString();
			var id = Convert.ToInt64(row["DOC"]);
			catalog.OriginFilePath = ToOriginFileName(id);
		}

		public static string ToOriginFileName(long id)
		{
			return String.Format("{0}/{1}.gif",
				id.ToString("X2").RightSlice(2),
				id.ToString("X2"));
		}
	}
}