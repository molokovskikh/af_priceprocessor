using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.FTP.Client;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class KatrenSource : AbstractCertifcateSource, ICertificateSource, IRemoteFtpSource
	{
		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var catalogId = task.DocumentLine.ProductEntity.CatalogProduct.Id;
			var catalogs = GetSourceCatalog(catalogId, task.SerialNumber);

			if (catalogs.Count == 0)
				return;

			foreach (var certificateSourceCatalog in catalogs) {
				var filename = certificateSourceCatalog.OriginFilePath;
				var mask = String.Format("{0}*{1}", Path.GetFileNameWithoutExtension(filename), Path.GetExtension(filename));

				var dir = Path.Combine(FtpDir, Path.GetDirectoryName(certificateSourceCatalog.OriginFilePath));
				using (var ftpClient = new FTP_Client()) {
					ftpClient.PassiveMode = true;
					ftpClient.Connect(FtpHost, 21);
					ftpClient.Authenticate(FtpUser, FtpPassword);
					ftpClient.SetCurrentDir(dir);
					var ftpFiles = ftpClient.GetList();
					var filesToDownload = ftpFiles.Tables["DirInfo"]
						.AsEnumerable()
						.Where(r => !Convert.ToBoolean(r["IsDirectory"]))
						.Select(r => r["Name"].ToString())
						.Where(n => FileHelper.CheckMask(n, mask))
						.ToList();
					foreach (var file in filesToDownload)
					{
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
		}

		public bool CertificateExists(DocumentLine line)
		{
			return GetSourceCatalog(line.ProductEntity.CatalogProduct.Id, line.SerialNumber).Count > 0;
		}

		private List<CertificateSourceCatalog> GetSourceCatalog(uint catalogId, string serialNumber)
		{
			var name = GetType().Name;
			return CertificateSourceCatalog.Queryable
				.Where(
					c => c.CertificateSource.SourceClassName == name
						&& c.SerialNumber == serialNumber
						&& c.CatalogProduct.Id == catalogId)
				.ToList();
		}

		public string FtpHost
		{
			get { return "orel.katren.ru"; }
		}

		public string FtpDir
		{
			get { return "serts"; }
		}

		public string FtpUser
		{
			get { return "anonymous"; }
		}

		public string FtpPassword
		{
			get { return ""; }
		}

		public string Filename
		{
			get { return "table.dbf"; }
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