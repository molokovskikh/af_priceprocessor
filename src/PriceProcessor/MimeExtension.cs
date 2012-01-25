using System;
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
				stream.AddBuffer(Encoding.ASCII.GetBytes(mime.MainEntity.Subject));
				stream.AddBuffer(Encoding.ASCII.GetBytes(mime.BodyText));

				var attachments =
					mime.Attachments.Where(m => !string.IsNullOrEmpty(m.GetFilename()) && m.Data != null).OrderBy(m => m.GetFilename());

				foreach (var attachment in attachments) {
					stream.AddBuffer(attachment.Data);
				}

				using (var sha256Hash = new SHA256Managed()) {
					var hash = sha256Hash.ComputeHash(stream);

					return Convert.ToBase64String(hash);
				}
			}
		}
	}
}