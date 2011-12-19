using System;
using System.Collections.Generic;
using System.IO;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class AptekaHoldingVoronezhCertificateSource : AbstractCertifcateSource, ICertificateSource
	{
		private ILog _logger = LogManager.GetLogger(typeof (AptekaHoldingVoronezhCertificateSource));

		public bool CertificateExists(DocumentLine documentLine)
		{
			return !String.IsNullOrEmpty(documentLine.CertificateFilename) ||
			       !String.IsNullOrEmpty(documentLine.ProtocolFilemame) ||
			       !String.IsNullOrEmpty(documentLine.PassportFilename);
		}

		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var certificatesPath = Path.Combine(Settings.Default.FTPOptBoxPath, task.CertificateSource.FtpSupplier.Id.ToString().PadLeft(3, '0'), "Certificats");

			if (Directory.Exists(certificatesPath)) {

				if (!String.IsNullOrEmpty(task.DocumentLine.CertificateFilename))
					AddFiles(certificatesPath, task.DocumentLine.CertificateFilename, files);

				if (!String.IsNullOrEmpty(task.DocumentLine.ProtocolFilemame))
					AddFiles(certificatesPath, task.DocumentLine.ProtocolFilemame, files);

				if (!String.IsNullOrEmpty(task.DocumentLine.PassportFilename))
					AddFiles(certificatesPath, task.DocumentLine.PassportFilename, files);

			}
			else 
				_logger.WarnFormat("Директория {0} для задачи сертификата {1} не существует", 
					certificatesPath,
					task);
		}

		private void AddFiles(string certificatesPath, string certificateFilenameMask, IList<CertificateFile> list)
		{
			var files = Directory.GetFiles(certificatesPath, certificateFilenameMask + "*");

			foreach (var file in files) {
				var tempFile = Path.GetTempFileName();
				File.Copy(file, tempFile, true);
				list.Add(new CertificateFile(tempFile, Path.GetFileName(file), file) { Extension = ".tif"});
			}
		}
	}
}