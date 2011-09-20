﻿using System;
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
				var files = source.GetCertificateFiles(certificateTask);

				if (files.Count > 0)
					try {

						CreateCertificate(certificateTask, source, files);

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
				}
			}
			else {
				_logger.WarnFormat("Для задачи сертификата {0} не был найден источник", certificateTask);
			}
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
					var certificateFile = new CertificateFile {
						OriginFilename = Path.GetFileName(certificateFileEntry.OriginFile),
						Supplier = certificateTask.Supplier
					};

					certificate.NewFile(certificateFile);
				}

				certificate.Update();

				certificateTask.Delete();

				transaction.VoteCommit();
			}

			//Здесь надо осуществить копирование сертификатов

			source.CommitCertificateFiles(certificateTask, files);
		}
	}
}