using System;
using System.Linq;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;

namespace Inforoom.PriceProcessor.Waybills
{
	public class CertificateSourceDetector
	{
		public static CertificateSource DetectSource(Document document)
		{
			var source = CertificateSource.Queryable.FirstOrDefault(s => s.Suppliers.FirstOrDefault(certificateSupplier => certificateSupplier.Id == document.FirmCode) != null);
			if (source != null) {
				try {
					source.CertificateSourceParser = source.GetCertificateSource();
				}
				catch (Exception exception) {
					var _logger = LogManager.GetLogger(typeof(CertificateSourceDetector));
					_logger.WarnFormat("Ошибка при создании экземпляра для разбора сертификатов {0}: {1}", source.SourceClassName, exception);
				}
				return source.CertificateSourceParser != null ? source : null;
			}
			return null;
		}

		public static void DetectAndParse(Document document)
		{
			var source = DetectSource(document);

			if (source != null) {
				foreach (var documentLine in document.Lines) {
					if (documentLine.ProductEntity != null) {
						var certificate =
							Certificate.Queryable.FirstOrDefault(
								c => c.CatalogProduct.Id == documentLine.ProductEntity.CatalogProduct.Id
									&& c.SerialNumber == documentLine.CertificateSerialNumber
									&& c.CertificateFiles.Any(f => f.CertificateSource.Id == source.Id));
						if (certificate != null)
							documentLine.Certificate = certificate;
						else if (source.CertificateSourceParser.CertificateExists(documentLine))
							document.AddCertificateTask(documentLine, source);
					}
				}
			}
		}
	}
}