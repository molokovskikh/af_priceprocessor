using System.Collections.Generic;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public interface ICertificateSource
	{
		bool CertificateExists(DocumentLine documentLine);
		IList<CertificateFileEntry> GetCertificateFiles(CertificateTask certificateTask);
		void CommitCertificateFiles(CertificateTask certificateTask, IList<CertificateFileEntry> fileEntries);
	}
}