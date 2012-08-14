using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class ProtekSource : AbstractCertifcateSource, ICertificateSource
	{
		public bool CertificateExists(DocumentLine line)
		{
			var exists = line.ProtekDocIds != null && line.ProtekDocIds.Count > 0;
			if (!exists)
				line.CertificateError = "Поставщик не предоставляет сертификаты для данной позиции";
			return exists;
		}

		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var supplierId = task.DocumentLine.Document.FirmCode;

			var config = WaybillProtekHandler.Configs.FirstOrDefault(c => c.SupplierId == supplierId);
			if (config == null)
				throw new Exception(String.Format("Не найдена конфигурация для получения сертификатов от поставщика № {0}", supplierId));

			new WaybillProtekHandler().WithService(config.Url, s => {
				foreach (var id in task.DocumentLine.ProtekDocIds)
				{
					var response = s.getSertImages(new getSertImagesRequest(config.ClientId, config.InstanceId, id.DocId));
					var index = 1;
					foreach(var sertImage in response.@return.sertImage)
					{
						var tempFile = Path.GetTempFileName();
						File.WriteAllBytes(tempFile, sertImage.image);
						files.Add(new CertificateFile(tempFile, id.DocId + "-" + index++) {
							Extension = ".tif"
						});
					}
				}
			});

			if (files.Count == 0)
				task.DocumentLine.CertificateError = "Поставщик не предоставил ни одного сертификата";
		}

		protected List<CertificateSourceCatalog> GetSourceCatalog(uint catalogId, string serialNumber)
		{
			var name = GetType().Name;
			return CertificateSourceCatalog.Queryable
				.Where(
					c => c.CertificateSource.SourceClassName == name
						&& c.SerialNumber == serialNumber
							&& c.CatalogProduct.Id == catalogId)
				.ToList();
		}
	}
}