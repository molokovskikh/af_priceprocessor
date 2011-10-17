using System;
using System.Linq;
using System.Reflection;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;

namespace Inforoom.PriceProcessor.Waybills
{
	public class CertificateSourceDetector
	{

		private static ICertificateSource GetCertificateSource(string sourceClassName)
		{ 
			Type result = null;
			var types = Assembly.GetExecutingAssembly()
								.GetModules()[0]
								.FindTypes(Module.FilterTypeNameIgnoreCase, sourceClassName);
			if (types.Length > 1)
				throw new Exception(String.Format("Найдено более одного типа с именем {0}", sourceClassName));
			if (types.Length == 1)
				result = types[0];
			if (result == null)
				throw new Exception(String.Format("Класс {0} не найден", sourceClassName));
			return (ICertificateSource)Activator.CreateInstance(result);
		}
	
		public static ICertificateSource DetectSource(Document document)
		{
			var source = CertificateSource.Queryable.Where(s => s.SourceSupplier.Id == document.FirmCode).FirstOrDefault();
			if (source != null) {
				ICertificateSource certificateSource = null;
				try {
					certificateSource = GetCertificateSource(source.SourceClassName);
				}
				catch (Exception exception) {
					ILog _logger = LogManager.GetLogger(typeof (CertificateSourceDetector));
					_logger.WarnFormat("Ошибка при создании экземпляра для разбора сертификатов {0}: {1}", source.SourceClassName, exception);
				}
				return certificateSource;
			}
			return null;
		}

		private static bool AllowSerialNumber(DocumentLine documentLine)
		{
			return !String.IsNullOrWhiteSpace(documentLine.SerialNumber) && documentLine.SerialNumber.Trim() != "-";
		}

		public static void DetectAndParse(Document document)
		{
			var source = DetectSource(document);

			if (source != null) {

				foreach (var documentLine in document.Lines) {
					if (documentLine.ProductEntity != null && AllowSerialNumber(documentLine)) {
						var certificateSource = CertificateSource.Queryable.First(s => s.SourceSupplier.Id == document.FirmCode);
						var certificate = 
							Certificate.Queryable.FirstOrDefault(
								c => c.CatalogProduct.Id == documentLine.ProductEntity.CatalogProduct.Id 
									&& c.SerialNumber == documentLine.SerialNumber 
									&& c.CertificateFiles.Any(f => f.CertificateSource.Id == certificateSource.Id));
						if (certificate != null)
							documentLine.Certificate = certificate;
						else 
							if (source.CertificateExists(documentLine))
								document.AddCertificateTask(documentLine, certificateSource);
					}
				}

			}
		}

	}

}