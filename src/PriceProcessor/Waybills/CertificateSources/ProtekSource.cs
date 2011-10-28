﻿using System.Collections.Generic;
using System.IO;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class ProtekSource : ICertificateSource
	{
		public bool CertificateExists(DocumentLine line)
		{
			return line.ProtekDocIds != null && line.ProtekDocIds.Count > 0;
		}

		public IList<CertificateFile> GetCertificateFiles(CertificateTask task)
		{
			var uri = "http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			var result = new List<CertificateFile>();

			new ProtekWaybillHandler().WithService(uri, s => {
				foreach (var id in task.DocumentLine.ProtekDocIds)
				{
					var response = s.getSertImages(new getSertImagesRequest(83674, 1033812, id.DocId));
					var index = 1;
					foreach(var sertImage in response.@return.sertImage)
					{
						var tempFile = Path.GetTempFileName();
						File.WriteAllBytes(tempFile, sertImage.image);
						result.Add(new CertificateFile(tempFile, id.DocId + "-" + index++) {
							Extension = ".tif"
						});
					}
				}
			});

			return result;
		}
	}
}