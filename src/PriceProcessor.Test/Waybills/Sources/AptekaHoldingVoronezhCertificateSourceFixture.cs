using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Sources
{
	[TestFixture]
	public class AptekaHoldingVoronezhCertificateSourceFixture
	{
		[Test(Description = "для строки из накладной определяем существование сертификатов")]
		public void CertificateExists()
		{
			var aptekaHoldingVoronezhCertificateSource = new AptekaHoldingVoronezhCertificateSource();

			var product = Product.FindFirst();

			var line = new DocumentLine {
				Code = "22651",
				SerialNumber = "835495",
				ProductEntity = product
			};

			Assert.That(aptekaHoldingVoronezhCertificateSource.CertificateExists(line), Is.False);

			line.CertificateFilename = "test_00";

			Assert.That(aptekaHoldingVoronezhCertificateSource.CertificateExists(line), Is.True);
		}
	}
}