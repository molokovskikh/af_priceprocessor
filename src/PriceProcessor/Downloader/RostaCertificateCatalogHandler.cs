using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
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

					Ping();

					var catalogFile = GetCatalogFile(source);

					Ping();

					try {
						if (catalogFile != null)
							ImportCatalogFile(catalogFile);

						Ping();
					}
					finally {
						if (catalogFile != null && File.Exists(catalogFile.LocalFileName))
							File.Delete(catalogFile.LocalFileName);
					}
				}
			}
		}

		protected virtual void ImportCatalogFile(CertificateCatalogFile catalogFile)
		{
			_logger.InfoFormat("Загружен новый каталог сертификатов: date: {0},  fileName: {1}", catalogFile.FileDate, catalogFile.LocalFileName);

			var catalogTable = Dbf.Load(catalogFile.LocalFileName);
			var catalogList = new List<CertificateSourceCatalog>();

			_logger.InfoFormat("Количество строк в новом каталоге сертификатов: {0}", catalogTable.Rows.Count);

			foreach (DataRow row in catalogTable.Rows) {
				var catalog = new CertificateSourceCatalog {
					CertificateSource = catalogFile.Source,
					SerialNumber = row["BATCH_ID"].ToString(),
					SupplierCode = row["CODE"].ToString(),
					OriginFilePath = row["GB_FILES"].ToString()
				};

				var catalogId = SessionHelper.WithSession(
					c => c.CreateSQLQuery(@"
select
	p.CatalogId
from
	documents.SourceSuppliers ss
	inner join usersettings.PricesData pd on pd.FirmCode = ss.SupplierId
	inner join farm.Core0 c on c.PriceCode = pd.PriceCode and c.Code = :supplierCode
	inner join catalogs.Products p on p.Id = c.ProductId
where
	ss.CertificateSourceId = :sourceId
limit 1;
"
						)
						.SetParameter("sourceId", catalogFile.Source.Id)
						.SetParameter("supplierCode", catalog.SupplierCode)
						.UniqueResult());
				uint catalogValue;
				if (catalogId != null && uint.TryParse(catalogId.ToString(), out catalogValue))
					catalog.CatalogProduct = Catalog.Find(catalogId);

				catalogList.Add(catalog);

				if (catalogList.Count % 10000 == 0)
				{
					Ping();
					_logger.DebugFormat("Количество обработанных строк нового каталога сертификатов: {0}", catalogList.Count);
				}
			}

			_logger.InfoFormat("Начата транзакция по обновлению каталога сертификатов");
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
				_logger.InfoFormat("Транзакция по обновлению каталога сертификатов завершена успешно");
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