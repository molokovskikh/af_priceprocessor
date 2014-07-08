using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class SiaSource : AbstractCertifcateSource
	{
		public override bool CertificateExists(DocumentLine line)
		{
			var exists = !String.IsNullOrEmpty(line.Code) && !String.IsNullOrEmpty(line.SerialNumber);
			if (!exists) {
				if (String.IsNullOrEmpty(line.Code))
					line.CertificateError = "Поставщик не указал код";
				else if (String.IsNullOrEmpty(line.SerialNumber))
					line.CertificateError = "Поставщик не указал серию";
			}
			return exists;
		}

		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var client = new WebClient();
			var uri = new Uri(task.CertificateSource.LookupUrl);
			client.Credentials = Util.GetCredentials(uri);
			var data = client.UploadValues(uri, new NameValueCollection {
				{ "Code", task.DocumentLine.Code },
				{ "SerialNumber", task.DocumentLine.SerialNumber },
			});

			var doc = XDocument.Load(new MemoryStream(data));
			foreach (var cert in doc.XPathSelectElements("/ArrayOfCert/Cert")) {
				var fileUri = (string)cert.XPathSelectElement("Uri");
				var name = (string)cert.XPathSelectElement("Name");
				var localFile = Path.GetTempFileName();
				Log.DebugFormat("Будет производиться закачка файла {0} в локальный файл {1}", fileUri, localFile);
				client.DownloadFile(fileUri, localFile);
				files.Add(new CertificateFile(localFile, fileUri, Path.GetFileName(fileUri)) {
					Note = name
				});
			}

			if (files.Count == 0)
				task.DocumentLine.CertificateError = "Поставщик не предоставил ни одного сертификата";
		}
	}
}