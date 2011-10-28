using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.CertificateSources
{
	public class SiaSource : ICertificateSource
	{
		public bool CertificateExists(DocumentLine line)
		{
			return !String.IsNullOrEmpty(line.Code) && !String.IsNullOrEmpty(line.SerialNumber);
		}

		public IList<CertificateFile> GetCertificateFiles(CertificateTask task)
		{
			var client = new WebClient();
			var data = client.UploadValues("http://sds.siachel.ru/sds/index.php", new NameValueCollection {
				{"Kod", task.DocumentLine.Code},
				{"Seria", task.DocumentLine.SerialNumber},
			});
			var text = Encoding.GetEncoding(1251).GetString(data);
			
			var result = new List<CertificateFile>();

			foreach (var file in ParseFiles(text))
			{
				var localFile = Path.GetTempFileName();
				var uri = "http://sds.siachel.ru/DOCS/" + file.Replace("\\", "/");
				client.DownloadFile(uri, localFile);
				result.Add(new CertificateFile(localFile, file, file));
			}
			return result;
		}

		public static IEnumerable<string> ParseFiles(string data)
		{
			var reg = new Regex("DOCS.+?GIF");
			return reg.Matches(data).Cast<Match>().SelectMany(m => m.Captures.Cast<Capture>().Select(c => c.Value));
		}
	}
}