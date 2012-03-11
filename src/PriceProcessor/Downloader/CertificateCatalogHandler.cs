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

	public class CertificateCatalogHandler : AbstractHandler
	{
		public CertificateCatalogHandler()
		{
			SleepTime = 5;
		}
		
		protected override void ProcessData()
		{
			using (new SessionScope()) {
				var sources = CertificateSource.Queryable.ToArray();
				foreach (var source in sources)
				{
					var ftpSource = source.GetCertificateSource() as IRemoteFtpSource;
					if (ftpSource != null)
						DownloadFile(source, ftpSource);
				}
			}
		}

		private void DownloadFile(CertificateSource source, IRemoteFtpSource ftpSource)
		{
			Cleanup();

			Ping();

			var catalogFile = GetCatalogFile(ftpSource, source);

			Ping();

			try
			{
				if (catalogFile != null)
					ImportCatalogFile(catalogFile, source, ftpSource);

				Ping();
			}
			finally
			{
				if (catalogFile != null && File.Exists(catalogFile.LocalFileName))
					File.Delete(catalogFile.LocalFileName);
			}
		}

		public virtual void ImportCatalogFile(CertificateCatalogFile catalogFile, CertificateSource source, IRemoteFtpSource ftpSource)
		{
			_logger.InfoFormat("Загружен новый каталог сертификатов: date: {0},  fileName: {1}", catalogFile.FileDate, catalogFile.LocalFileName);

			var catalogTable = Dbf.Load(catalogFile.LocalFileName);
			var catalogList = new List<CertificateSourceCatalog>();

			_logger.InfoFormat("Количество строк в новом каталоге сертификатов: {0}", catalogTable.Rows.Count);

			//Выбираем записи из Core для ассортиментных прайсов поставщиков, которые привязаны к нужному источнику сертификатов
			var filter = "";
			if (source.SearchInAssortmentPrice)
				filter = "and pd.PriceType = 1";
			var cores = SessionHelper.WithSession(
				c => c.CreateSQLQuery(@"
select
	{core.*}
from
	documents.SourceSuppliers ss
	inner join usersettings.PricesData pd on pd.FirmCode = ss.SupplierId
	inner join farm.Core0 {core} on core.PriceCode = pd.PriceCode
	inner join catalogs.Products p on p.Id = core.ProductId
where
	ss.CertificateSourceId = :sourceId
" + filter)
					.AddEntity("core", typeof(Core))
					.SetParameter("sourceId", catalogFile.Source.Id)
					.List<Core>()
			);

			_logger.InfoFormat("Количество загруженных позиций из Core: {0}", cores.Count);

			foreach (DataRow row in catalogTable.Rows) {
				var catalog = new CertificateSourceCatalog {
					CertificateSource = catalogFile.Source,
				};
				ftpSource.ReadSourceCatalog(catalog, row);

				var core = catalog.FindCore(cores);
				if (core != null && core.Product != null)
					catalog.CatalogProduct = core.Product.CatalogProduct;

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

				catalogList.ForEach(c => c.Save());

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

		public virtual CertificateCatalogFile GetCatalogFile(IRemoteFtpSource ftpSource, CertificateSource source)
		{
			var downloader = new FtpDownloader();

			var downloadFiles = downloader.GetFilesFromSource(
				ftpSource.FtpHost,
				21,
				ftpSource.FtpDir,
				ftpSource.FtpUser,
				ftpSource.FtpPassword,
				ftpSource.Filename,
				source.FtpFileDate.HasValue ? source.FtpFileDate.Value : DateTime.MinValue,
				DownHandlerPath);

			if (downloadFiles.Count > 0)
				return new CertificateCatalogFile {
					Source = source,
					FileDate = downloadFiles[0].FileDate,
					LocalFileName = downloadFiles[0].FileName
				};

			return null;
		}
	}
}