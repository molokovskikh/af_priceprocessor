using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using NHibernate;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public abstract class FtpCertifcateSource : AbstractCertifcateSource
	{
		protected List<CertificateSourceCatalog> GetSourceCatalog(uint catalogId, string serialNumber)
		{
			var name = GetType().Name;
			return CertificateSourceCatalog.Queryable
				.Where(
				c => c.CertificateSource.SourceClassName == name
					&& c.SerialNumber == serialNumber
					&& c.CatalogProduct.Id == catalogId)
				.ToList();
		}

		public override bool CertificateExists(DocumentLine line)
		{
			var exists = GetSourceCatalog(line.ProductEntity.CatalogProduct.Id, line.SerialNumber).Count > 0;
			if (!exists)
				line.CertificateError = "Нет записи в таблице перекодировки";
			return exists;
		}
	}

	public abstract class AbstractCertifcateSource : ICertificateSource
	{
		protected ISession Session;
		protected ILog Log;
		protected FileCleaner Cleaner;

		protected AbstractCertifcateSource()
		{
			Log = LogManager.GetLogger(GetType());
			Cleaner = new FileCleaner();
		}

		protected AbstractCertifcateSource(FileCleaner cleaner)
			: this()
		{
			Cleaner = cleaner;
		}

		public abstract bool CertificateExists(DocumentLine line);
		public abstract void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files);

		public IList<CertificateFile> GetCertificateFiles(CertificateTask task, ISession session)
		{
			var result = new List<CertificateFile>();
			Session = session;

			try {
				GetFilesFromSource(task, result);
			}
			catch {
				Cleaner.Dispose();

				//Удаляем временные закаченные файлы
				result.ForEach(f => {
					try {
						if (File.Exists(f.LocalFile))
							File.Delete(f.LocalFile);
					}
					catch (Exception exception) {
						Log.WarnFormat("Ошибка при удалении временного файла {0} по задаче {1}: {2}", f.LocalFile, task, exception);
					}
				});
				throw;
			}
			finally {
				Session = null;
			}

			return result;
		}
	}
}