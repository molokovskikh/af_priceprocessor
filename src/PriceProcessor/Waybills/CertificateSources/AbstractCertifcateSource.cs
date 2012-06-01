using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public abstract class FtpCertifcateSource : AbstractCertifcateSource, ICertificateSource
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

		public bool CertificateExists(DocumentLine line)
		{
			var exists = GetSourceCatalog(line.ProductEntity.CatalogProduct.Id, line.SerialNumber).Count > 0;
			if (!exists)
				line.CertificateError = "Нет записи в таблице перекодировки";
			return exists;
		}
	}

	public abstract class AbstractCertifcateSource
	{
		public abstract void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files);
		protected ILog Log;

		protected AbstractCertifcateSource()
		{
			Log = LogManager.GetLogger(GetType());
		}

		public IList<CertificateFile> GetCertificateFiles(CertificateTask task)
		{
			var result = new List<CertificateFile>();

			try {
				GetFilesFromSource(task, result);
			}
			catch {
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

			return result;
		}
	}
}