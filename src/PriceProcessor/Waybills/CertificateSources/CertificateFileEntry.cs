using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class CertificateFileEntry
	{
		public CertificateFileEntry(string localFile)
		{
			LocalFile = localFile;
		}

		public CertificateFileEntry(string originFile, string localFile)
		{
			OriginFile = originFile;
			LocalFile = localFile;
		}

		public string OriginFile { get; set; }

		public string LocalFile { get; set; }

		public CertificateFile CertificateFile { get; set; }
	}
}