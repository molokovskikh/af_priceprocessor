using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class CertificateFileEntry
	{
		public CertificateFileEntry(string localFile)
		{
			LocalFile = localFile;
		}

		public CertificateFileEntry(string originFile, string localFile, string externalFileId)
		{
			OriginFile = originFile;
			LocalFile = localFile;
			ExternalFileId = externalFileId;
		}

		public string OriginFile { get; set; }

		public string LocalFile { get; set; }

		public string ExternalFileId { get; set; }

		public CertificateFile CertificateFile { get; set; }
	}
}