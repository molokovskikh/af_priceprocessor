using System;
using System.Linq;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills
{
	public class CertificateSourceDetector
	{
		public static ICertificateSource DetectSource(Document document)
		{
			//Для воронежской АптекиХолдинг возвращаем источник для разбора
			if (document.FirmCode == 39u)
				return new AptekaHoldingVoronezhCertificateSource();
			return null;
		}

		public static void DetectAndParse(Document document)
		{
			var source = DetectSource(document);

			if (source != null) {

				foreach (var documentLine in document.Lines) {
					if (documentLine.ProductEntity != null && !String.IsNullOrEmpty(documentLine.SerialNumber)) {
						var certificate = 
							Certificate.Queryable.FirstOrDefault(
								c => c.CatalogProduct.Id == documentLine.ProductEntity.CatalogProduct.Id && c.SerialNumber == documentLine.SerialNumber && c.CertificateFiles.Any(f => f.Supplier.Id == document.FirmCode));
						if (certificate != null)
							documentLine.Certificate = certificate;
						else 
							if (source.CertificateExists(documentLine))
								document.AddCertificateTask(documentLine);
					}
				}

			}
		}

	}

}