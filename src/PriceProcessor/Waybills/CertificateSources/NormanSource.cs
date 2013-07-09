using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class NormanSource : AbstractCertifcateSource
	{
		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var line = task.DocumentLine;
			var file = line.CertificateFilename;
			var client = new WebClient();
			client.Encoding = Encoding.GetEncoding(1251);
			var request = WebRequest.Create("http://www.norman-plus.ru/cert/sert_handler.ashx");
			request.Method = "POST";
			using (var stream = new StreamWriter(request.GetRequestStream(), Encoding.GetEncoding(1251))) {
				stream.Write(file);
			}
			using (var response = (HttpWebResponse)request.GetResponse()) {
				if (response.StatusCode != HttpStatusCode.OK) {
					line.CertificateError = String.Format("Поставщик не предоставил сертификат, код ошибки {0}", response.StatusCode);
					return;
				}
				using (var result = response.GetResponseStream()) {
					var localFile = Path.GetTempFileName();
					using (var f = File.OpenWrite(localFile)) {
						files.Add(new CertificateFile(localFile, file, file));
						result.CopyTo(f);
					}
				}
			}
		}

		public override bool CertificateExists(DocumentLine line)
		{
			if (String.IsNullOrEmpty(line.CertificateFilename)) {
				line.CertificateError = "Поставщик не указал имя файла сертификата в накладной";
				return false;
			}
			return true;
		}
	}
}