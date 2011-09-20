using System;
using System.Collections.Generic;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class AptekaHoldingVoronezhCertificateSource : ICertificateSource
	{
		public bool CertificateExists(DocumentLine documentLine)
		{
			return !String.IsNullOrEmpty(documentLine.CertificateFilename) ||
			       !String.IsNullOrEmpty(documentLine.ProtocolFilemame) ||
			       !String.IsNullOrEmpty(documentLine.PassportFilename);
		}

		public IList<CertificateFileEntry> GetCertificateFiles(CertificateTask certificateTask)
		{
			throw new NotImplementedException();
		}

		public void CommitCertificateFiles(CertificateTask certificateTask, IList<CertificateFileEntry> fileEntries)
		{
			throw new NotImplementedException();
		}
	}
}