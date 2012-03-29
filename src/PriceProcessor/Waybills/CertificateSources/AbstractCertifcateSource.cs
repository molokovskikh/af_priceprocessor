using System;
using System.Collections.Generic;
using System.IO;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
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