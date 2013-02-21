using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class CertificateFixture
	{
		[Test]
		public void Do_not_add_duplicate_files()
		{
			var certificate = new Certificate();
			var file = new CertificateFile();
			certificate.NewFile(file);
			certificate.NewFile(file);
			Assert.That(certificate.CertificateFiles.Count, Is.EqualTo(1));
		}
	}
}