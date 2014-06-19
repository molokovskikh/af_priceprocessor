using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class NadezhdaFarmCertificateSource : AbstractCertifcateSource
	{
		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var certificatesPath = task.GetLocalPath();

			if (!Directory.Exists(certificatesPath)) {
				Log.WarnFormat("Директория {0} для задачи сертификата {1} не существует",
					certificatesPath,
					task);
				return;
			}

			var foundFiles = task.DocumentLine.CertificateFilename
				.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(n => Path.Combine(certificatesPath, n))
				.Where(f => File.Exists(f))
				.Select(f => new CertificateFile(CopyToTemp(f), Path.GetFileName(f), Path.GetFileName(f)))
				.ToList();

			foundFiles.Each(files.Add);
			if (files.Count == 0)
				task.DocumentLine.CertificateError = "Файл сертификата не найден на ftp Инфорум";
		}

		protected string CopyToTemp(string filename)
		{
			var tempFile = Cleaner.TmpFile();
			File.Copy(filename, tempFile, true);
			return tempFile;
		}

		public override bool CertificateExists(DocumentLine line)
		{
			var exists = !String.IsNullOrEmpty(line.CertificateFilename);
			if (!exists)
				line.CertificateError = "Поставщик не указал имя файла сертификата в накладной";
			return exists;
		}
	}
}