using System;
using System.Collections.Generic;
using System.IO;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class AptekaHoldingVoronezhCertificateSource : ICertificateSource
	{
		private ILog _logger = LogManager.GetLogger(typeof (AptekaHoldingVoronezhCertificateSource));

		public bool CertificateExists(DocumentLine documentLine)
		{
			return !String.IsNullOrEmpty(documentLine.CertificateFilename) ||
			       !String.IsNullOrEmpty(documentLine.ProtocolFilemame) ||
			       !String.IsNullOrEmpty(documentLine.PassportFilename);
		}

		public IList<CertificateFileEntry> GetCertificateFiles(CertificateTask certificateTask)
		{
			var certificatesPath = Path.Combine(Settings.Default.FTPOptBoxPath, certificateTask.CertificateSource.FtpSupplier.Id.ToString().PadLeft(3, '0'), "Certificats");

			var list = new List<CertificateFileEntry>();

			if (Directory.Exists(certificatesPath)) {

				if (!String.IsNullOrEmpty(certificateTask.DocumentLine.CertificateFilename))
					AddFiles(certificatesPath, certificateTask.DocumentLine.CertificateFilename, list);

				if (!String.IsNullOrEmpty(certificateTask.DocumentLine.ProtocolFilemame))
					AddFiles(certificatesPath, certificateTask.DocumentLine.ProtocolFilemame, list);

				if (!String.IsNullOrEmpty(certificateTask.DocumentLine.PassportFilename))
					AddFiles(certificatesPath, certificateTask.DocumentLine.PassportFilename, list);

			}
			else 
				_logger.WarnFormat("Директория {0} для задачи сертификата {1} не существует", 
					certificatesPath,
					certificateTask);

			return list;
		}

		private void AddFiles(string certificatesPath, string certificateFilenameMask, List<CertificateFileEntry> list)
		{
			var files = Directory.GetFiles(certificatesPath, certificateFilenameMask + "*");

			foreach (var file in files) {
				var tempFile = Path.GetTempFileName();
				File.Copy(file, tempFile, true);
				list.Add(new CertificateFileEntry (file, tempFile, Path.GetFileName(file)));
			}
		}
	}
}