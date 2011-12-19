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
	public class SiaSource : AbstractCertifcateSource, ICertificateSource
	{
		public bool CertificateExists(DocumentLine line)
		{
			return !String.IsNullOrEmpty(line.Code) && !String.IsNullOrEmpty(line.SerialNumber);
		}

		public override void GetFilesFromSource(CertificateTask task, IList<CertificateFile> files)
		{
			var client = new WebClient();
			var data = client.UploadValues("http://sds.siachel.ru/sds/index.php", new NameValueCollection {
				{"Kod", task.DocumentLine.Code},
				{"Seria", task.DocumentLine.SerialNumber},
			});
			var text = Encoding.GetEncoding(1251).GetString(data);
			
			foreach (var file in ParseFiles(text))
			{
				var localFile = Path.GetTempFileName();
				var uri = "http://sds.siachel.ru/DOCS/" + file.Replace("\\", "/");
				client.DownloadFile(uri, localFile);
				files.Add(new CertificateFile(localFile, file, file));
			}
		}

		public static IEnumerable<string> ParseFiles(string data)
		{
			var reg = new Regex("DOCS.+?GIF");
			return reg.Matches(data).Cast<Match>().SelectMany(m => m.Captures.Cast<Capture>().Select(c => c.Value));
		}
	}
}