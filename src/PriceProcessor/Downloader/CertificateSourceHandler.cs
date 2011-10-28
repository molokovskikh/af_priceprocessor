using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Downloader
{
	public class CertificateSourceHandler : AbstractHandler
	{
		public CertificateSourceHandler()
		{
			SleepTime = 5;
		}


		protected override void ProcessData()
		{
			using (new SessionScope()) {
				var tasks = CertificateTask.FindAll();

				if (tasks != null && tasks.Length > 0)
					foreach (var certificateTask in tasks) {
						try {
							ProcessTask(certificateTask);
						}
						catch (Exception exception) {
							_logger.WarnFormat("Ошибка при отбработки задачи для сертификата {0} : {1}", certificateTask, exception);
						}
					
					}
			}
		}

		private void ProcessTask(CertificateTask certificateTask)
		{
			var source = CertificateSourceDetector.DetectSource(certificateTask.DocumentLine.Document);

			if (source != null) {
				var files = source.CertificateSourceParser.GetCertificateFiles(certificateTask);

				if (files.Count > 0)
					try {

						CreateCertificate(certificateTask, source.CertificateSourceParser, files);

					}
					finally {
						foreach (var certificateFileEntry in files) {
							if (File.Exists(certificateFileEntry.LocalFile))
								try {
									File.Delete(certificateFileEntry.LocalFile);
								}
								catch (Exception exception) {
									_logger.WarnFormat(
										"Для задачи сертификата {0} возникла ошибка при удалении локального файла {1}: {2}", 
										certificateTask, 
										certificateFileEntry.LocalFile,
										exception);
								}
						}
					
					}
				else {
					_logger.WarnFormat("Для задачи сертификата {0} не были получены файлы", certificateTask);
					using (new TransactionScope()) {
						certificateTask.Delete();
					}
				}
			}
			else {
				_logger.ErrorFormat("Для задачи сертификата {0} не был найден источник", certificateTask);
				using (new TransactionScope()) {
					certificateTask.Delete();
				}
			}
		}

		private CertificateFile FindOrCreate(string originFile, CertificateSource source, string externalFileId)
		{
			var existsCertificateFile =
				CertificateFile.Queryable.FirstOrDefault(
					file => file.CertificateSource.Id == source.Id && file.ExternalFileId == externalFileId);
			return existsCertificateFile ?? new CertificateFile(originFile, source, externalFileId);
		}

		private void CreateCertificate(CertificateTask certificateTask, ICertificateSource source, IList<CertificateFileEntry> files)
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				
				var certificate = Certificate.Queryable.FirstOrDefault(
					c => c.CatalogProduct.Id == certificateTask.CatalogProduct.Id && c.SerialNumber == certificateTask.SerialNumber);

				if (certificate == null)
				{
					certificate = new Certificate {
						CatalogProduct = certificateTask.CatalogProduct,
						SerialNumber = certificateTask.SerialNumber
					};
				}

				foreach (var certificateFileEntry in files) {
					certificateFileEntry.CertificateFile = FindOrCreate(certificateFileEntry.OriginFile, certificateTask.CertificateSource, certificateFileEntry.ExternalFileId);
					certificate.NewFile(certificateFileEntry.CertificateFile);
				}

				certificate.Save();

				certificateTask.Delete();

				var session = ActiveRecordMediator.GetSessionFactoryHolder().CreateSession(typeof (ActiveRecordBase));
				try
				{
					session.CreateSQLQuery(@"
	update  
		documents.Certificates c,
		catalogs.Products p,
		documents.DocumentBodies db
	set
		db.CertificateId = c.Id
	where
		c.Id = :certificateId
		and p.CatalogId = c.catalogId
		and db.ProductId is not null 
		and db.SerialNumber is not null 
		and db.ProductId = p.Id 
		and db.SerialNumber = c.SerialNumber 
		and db.CertificateId is null;
		")
						.SetParameter("certificateId", certificate.Id)
						.ExecuteUpdate();
				}
				finally
				{
					ActiveRecordMediator.GetSessionFactoryHolder().ReleaseSession(session);
				}

				transaction.VoteCommit();
			}


			foreach (var certificateFileEntry in files) {
				var fileName = certificateFileEntry.CertificateFile.Id + ".tif";
				var fullFileName = Path.Combine(Settings.Default.CertificatePath, fileName);

				try {
					if (File.Exists(fullFileName)) {
						File.Delete(fullFileName);
						_logger.InfoFormat(
							"Будет произведено обновление файла сертификата {0} с Id {1} для сертификата {2}", 
							certificateFileEntry.LocalFile,
							certificateFileEntry.CertificateFile.Id,
							certificateTask);
					}

					File.Copy(certificateFileEntry.LocalFile, fullFileName);
				}
				catch (Exception exception) {
					_logger.WarnFormat(
						"При копировании файла {0} для сертификата {1} возникла ошибка: {2}", 
						certificateFileEntry.LocalFile,
						certificateTask, 
						exception);
				}
			}
		}
	}
}