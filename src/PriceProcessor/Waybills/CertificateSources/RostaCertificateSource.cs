using System;
using System.Collections.Generic;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class RostaCertificateSource : ICertificateSource
	{
		public bool CertificateExists(DocumentLine line)
		{
			throw new NotImplementedException();
		}

		public IList<CertificateFile> GetCertificateFiles(CertificateTask task)
		{
			throw new NotImplementedException();
		}
	}
}