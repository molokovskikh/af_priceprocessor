using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Common.Tools;
using Inforoom.Downloader;

namespace LumiSoft.Net.Mime
{
	public static class MimeExtention
	{
		public static string GetSHA256Hash(this Mime mime)
		{
			using (var stream = new MultiBufferStream()) {
				if (!String.IsNullOrWhiteSpace(mime.MainEntity.Subject))
					stream.AddBuffer(Encoding.ASCII.GetBytes(mime.MainEntity.Subject));
				if (!String.IsNullOrWhiteSpace(mime.BodyText))
					stream.AddBuffer(Encoding.ASCII.GetBytes(mime.BodyText));
				if (String.IsNullOrWhiteSpace(mime.BodyText) && !String.IsNullOrWhiteSpace(mime.BodyHtml)) {
					var convertedHtml = mime.HtmlToText();
					if (!String.IsNullOrWhiteSpace(convertedHtml))
						stream.AddBuffer(Encoding.ASCII.GetBytes(convertedHtml));
				}

				var attachments = mime.GetValidAttachements().OrderBy(m => m.GetFilename());

				foreach (var attachment in attachments) {
					stream.AddBuffer(attachment.Data);
				}

				if (stream.Length > 0)
					using (var sha256Hash = new SHA256Managed()) {
						var hash = sha256Hash.ComputeHash(stream);

						return Convert.ToBase64String(hash);
					}

				return String.Empty;
			}
		}

		public static uint MailSize(this Mime mime)
		{
			uint mailSize = 0;
			if (!String.IsNullOrWhiteSpace(mime.BodyText))
				mailSize += (uint)mime.BodyText.Length;
			else if (!String.IsNullOrWhiteSpace(mime.BodyHtml))
				mailSize += (uint)mime.BodyHtml.Length;
			mailSize += (uint)mime.GetValidAttachements().Sum(a => a.Data.Length);
			return mailSize;
		}

		public static string HtmlToText(this Mime mime)
		{
			if (String.IsNullOrWhiteSpace(mime.BodyHtml))
				return mime.BodyHtml;

			var html = mime.BodyHtml;
			var converter = new Microsoft.Exchange.Data.TextConverters.HtmlToText(
				Microsoft.Exchange.Data.TextConverters.TextExtractionMode.ExtractText);
			var sb = new StringBuilder();
			using (var sr = new System.IO.StringReader(html))
			using (var sw = new System.IO.StringWriter(sb))
				converter.Convert(sr, sw);
			return sb.ToString();
		}
	}
}