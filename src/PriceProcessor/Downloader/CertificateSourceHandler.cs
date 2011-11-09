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

		private CertificateFile Find(CertificateFile file)
		{
			return 
				CertificateFile.Queryable.FirstOrDefault(
					e => e.CertificateSource.Id == file.CertificateSource.Id && e.ExternalFileId == file.ExternalFileId);
		}

		private void CreateCertificate(CertificateTask task, ICertificateSource source, IEnumerable<CertificateFile> files)
		{
			var savedFiles = new List<CertificateFile>();

			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				
				var certificate = Certificate.Queryable.FirstOrDefault(
					c => c.CatalogProduct.Id == task.CatalogProduct.Id && c.SerialNumber == task.SerialNumber);

				if (certificate == null)
				{
					certificate = new Certificate {
						CatalogProduct = task.CatalogProduct,
						SerialNumber = task.SerialNumber
					};
				}

				foreach (var file in files) {
					file.CertificateSource = task.CertificateSource;
					var exist = Find(file) ?? file;
					certificate.NewFile(exist);
					if (exist != file)
						exist.LocalFile = file.LocalFile;
					savedFiles.Add(exist);
				}

				certificate.Save();

				task.Delete();

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


			foreach (var file in savedFiles) {
				var fileName = file.RemoteFile;
				var fullFileName = Path.Combine(Settings.Default.CertificatePath, fileName);

				try {
					if (File.Exists(fullFileName)) {
						File.Delete(fullFileName);
						_logger.InfoFormat(
							"Будет произведено обновление файла сертификата {0} с Id {1} для сертификата {2}", 
							file.LocalFile,
							file.Id,
							task);
					}

					File.Copy(file.LocalFile, fullFileName);
				}
				catch (Exception exception) {
					_logger.WarnFormat(
						"При копировании файла {0} для сертификата {1} возникла ошибка: {2}", 
						file.LocalFile,
						task, 
						exception);
				}
			}
		}
	}
}