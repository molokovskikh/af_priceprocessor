using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class CertificateFileEntry
	{
		public string OriginFile { get; set; }

		public string LocalFile { get; set; }

		public CertificateFile CertificateFile { get; set; }
	}
}