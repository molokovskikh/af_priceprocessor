using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Downloader
{
	public class CertificateCatalogFile
	{
		public CertificateSource Source { get; set; }
		public DateTime FileDate { get; set; }
		public string LocalFileName { get; set; }
	}

	public class RostaCertificateCatalogHandler : AbstractHandler
	{
		public RostaCertificateCatalogHandler()
		{
			SleepTime = 5;
		}
		
		protected override void ProcessData()
		{
			using (new SessionScope()) {
			    var source = CertificateSource.Queryable.Where(s => s.SourceClassName == typeof(RostaCertificateSource).Name).FirstOrDefault();

				if (source != null) {
					Cleanup();

					var catalogFile = GetCatalogFile(source);

					try {
						if (catalogFile != null)
							ImportCatalogFile(catalogFile);
					}
					finally {
						if (File.Exists(catalogFile.LocalFileName))
							File.Delete(catalogFile.LocalFileName);
					}
				}
			}
		}

		protected virtual void ImportCatalogFile(CertificateCatalogFile catalogFile)
		{
			var catalogTable = Dbf.Load(catalogFile.LocalFileName);
			var catalogList = new List<CertificateSourceCatalog>();
			foreach (DataRow row in catalogTable.Rows) {
				var catalog = new CertificateSourceCatalog {
					CertificateSource = catalogFile.Source,
					SerialNumber = row["BATCH_ID"].ToString(),
					SupplierCode = row["CODE"].ToString(),
					OriginFilePath = row["GB_FILES"].ToString()
				};
				catalogList.Add(catalog);
			}

			using (var transaction = new TransactionScope(OnDispose.Rollback)) {

				var session = ActiveRecordMediator.GetSessionFactoryHolder().CreateSession(typeof (ActiveRecordBase));
				try
				{
					session.CreateSQLQuery(@"
	delete from
		documents.CertificateSourceCatalogs
	where
		CertificateSourceId = :certificateSourceId
		")
						.SetParameter("certificateSourceId", catalogFile.Source.Id)
						.ExecuteUpdate();
				}
				finally
				{
					ActiveRecordMediator.GetSessionFactoryHolder().ReleaseSession(session);
				}

				catalogList.ForEach(c => c.Create());

				var sessionUpdate = ActiveRecordMediator.GetSessionFactoryHolder().CreateSession(typeof (ActiveRecordBase));
				try
				{
					sessionUpdate.CreateSQLQuery(@"
	update
		documents.CertificateSources
	set
		FtpFileDate = :FtpFileDate
	where
		Id = :certificateSourceId
		")
						.SetParameter("certificateSourceId", catalogFile.Source.Id)
						.SetParameter("FtpFileDate", catalogFile.FileDate)
						.ExecuteUpdate();
				}
				finally
				{
					ActiveRecordMediator.GetSessionFactoryHolder().ReleaseSession(sessionUpdate);
				}

				transaction.VoteCommit();
			}
		}

		protected virtual CertificateCatalogFile GetCatalogFile(CertificateSource source)
		{
			var downloader = new FtpDownloader();

			var downloadFiles = downloader.GetFilesFromSource(
				Settings.Default.RostaCertificateFtp,
				21,
				"LIST",
				Settings.Default.RostaCertificateFtpUserName,
				Settings.Default.RostaCertificateFtpPassword,
				"SERT_LIST.DBF",
				source.FtpFileDate.HasValue ? source.FtpFileDate.Value : DateTime.MinValue,
				DownHandlerPath);

			if (downloadFiles.Count > 0)
				return new CertificateCatalogFile{
					Source = source,
					FileDate = downloadFiles[0].FileDate,
					LocalFileName = downloadFiles[0].FileName
				};

			return null;
		}
	}
}