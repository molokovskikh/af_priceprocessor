using System.Collections.Generic;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public interface ICertificateSource
	{
		bool CertificateExists(DocumentLine line);
		IList<CertificateFile> GetCertificateFiles(CertificateTask task, ISession session);
	}
}