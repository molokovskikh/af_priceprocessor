using System;
using System.Collections.Generic;
using System.IO;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class AptekaHoldingVoronezhCertificateSource : AbstractCertifcateSource, ICertificateSource
	{
		public bool CertificateExists(DocumentLine documentLine)
		{
			var exists = !String.IsNullOrEmpty(documentLine.CertificateFilename) ||
				!String.IsNullOrEmpty(documentLine.ProtocolFilemame) ||
				!String.IsNullOrEmpty(documentLine.PassportFilename);
			if (!exists)
				documentLine.CertificateError = "Поставщик не указал имя файла сертификата в накладной";
			return exists;
		}

		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var certificatesPath = Path.Combine(Settings.Default.FTPOptBoxPath, task.CertificateSource.FtpSupplier.Id.ToString().PadLeft(3, '0'), "Certificats");

			if (!Directory.Exists(certificatesPath)) {
				Log.WarnFormat("Директория {0} для задачи сертификата {1} не существует",
					certificatesPath,
					task);
				return;
			}

			AddFiles(task, certificatesPath, task.DocumentLine.CertificateFilename, files);
			AddFiles(task, certificatesPath, task.DocumentLine.ProtocolFilemame, files);
			AddFiles(task, certificatesPath, task.DocumentLine.PassportFilename, files);

			if (files.Count == 0)
				task.DocumentLine.CertificateError = "Файл сертификата не найден на ftp поставщика";
		}

		private void AddFiles(CertificateTask task,
			string certificatesPath,
			string certificateFilenameMask,
			IList<CertificateFile> list)
		{
			if (String.IsNullOrEmpty(certificateFilenameMask))
				return;

			if (certificateFilenameMask.Length < 5) {
				Log.WarnFormat("Для строки документа {0} загрузка сертификатов производиться не будет тк длинна маски '{1}' меньше 5",
					task.DocumentLine.Id,
					certificateFilenameMask);
				return;
			}

			var files = Directory.GetFiles(certificatesPath, certificateFilenameMask + "*");

			foreach (var file in files) {
				var tempFile = Path.GetTempFileName();
				File.Copy(file, tempFile, true);
				list.Add(new CertificateFile(tempFile, Path.GetFileName(file), file) { Extension = ".tif" });
			}
		}
	}
}