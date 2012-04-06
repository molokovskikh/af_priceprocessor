﻿using System;
using System.Collections.Generic;
using System.IO;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class FarmKomplektCertificateSource : AbstractCertifcateSource, ICertificateSource
	{
		private ILog _logger = LogManager.GetLogger(typeof (FarmKomplektCertificateSource));

		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var certificatesPath = Path.Combine(Settings.Default.FTPOptBoxPath, task.CertificateSource.FtpSupplier.Id.ToString().PadLeft(3, '0'), "Certificats");

			if (Directory.Exists(certificatesPath)) {

				if (!String.IsNullOrEmpty(task.DocumentLine.CertificateFilename))
					AddFile(certificatesPath, task.DocumentLine.CertificateFilename, files);

			}
			else 
				_logger.WarnFormat("Директория {0} для задачи сертификата {1} не существует", 
					certificatesPath,
					task);
		}

		private void AddFile(string certificatesPath, string certificateFilename, IList<CertificateFile> list)
		{
			var originFileName = Path.Combine(certificatesPath, certificateFilename);

			if (File.Exists(originFileName)) {
				var tempFile = Path.GetTempFileName();
				File.Copy(originFileName, tempFile, true);
				var certificateFile = new CertificateFile(tempFile, certificateFilename, originFileName);
				if (String.IsNullOrWhiteSpace(certificateFile.Extension) 
					|| (!certificateFile.Extension.Equals(".tif", StringComparison.OrdinalIgnoreCase) && !certificateFile.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
					)
					certificateFile.Extension = ".tif";
				list.Add(certificateFile);
			}
		}

		public bool CertificateExists(DocumentLine line)
		{
			return !String.IsNullOrEmpty(line.CertificateFilename);
		}
	}
}