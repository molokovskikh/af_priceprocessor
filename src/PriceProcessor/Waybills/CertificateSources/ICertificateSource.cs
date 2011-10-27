using System.Collections.Generic;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public interface ICertificateSource
	{
		bool CertificateExists(DocumentLine line);
		IList<CertificateFileEntry> GetCertificateFiles(CertificateTask task);
	}
}