using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Inforoom.Common;
using LumiSoft.Net.Mime;

namespace Inforoom.PriceProcessor.Helpers
{
	public static class UueHelper
	{
		/// <summary>
		/// Проверяет, является ли указанное mime сообщение
		/// закодированным в UUE
		/// </summary>
		/// <param name="mime"></param>
		/// <returns></returns>
		private static bool IsUue(Mime mime)
		{
			if (mime.MainEntity.Data == null)
				return false;
			var body = Encoding.GetEncoding("koi8-r").GetString(mime.MainEntity.Data);
			var reg = new Regex(@"(.*?\r\n\r\n)?begin\s\d\d\d");
			return reg.Match(body).Success;
		}

		/// <summary>
		/// Функция обработки тела письма в формате UUE.
		/// </summary>
		/// <param name="mime">Mime элемент письма</param>
		/// <param name="tempPath">Временная директория</param>
		/// <returns>Имя распакованного файла</returns>
		private static string ExtractFileFromUue(Mime mime, string tempPath)
		{
			var extractDir = "ExtractDir";
			//Двойная перекодировка сначала в koi8r -> UTF7 -> default(cp1251)
			var uueFileName = tempPath + "MailTemp.uue";
			using (var file = new FileStream(uueFileName, FileMode.Create)) {
				var body = Encoding.GetEncoding("koi8-r").GetString(mime.MainEntity.Data);
				var index = body.IndexOf("begin ");
				body = body.Substring(index);
				file.Write(Encoding.Default.GetBytes(body), 0, Encoding.Default.GetByteCount(body));
				file.Flush();
				file.Close();
			}
			try {
				if (ArchiveHelper.TestArchive(uueFileName)) {
					// Если файл является архивом
					try {
						FileHelper.ExtractFromArhive(uueFileName, uueFileName + extractDir);
						string[] fileList = Directory.GetFiles(uueFileName + extractDir);
						if (fileList.Length > 0) {
							if (File.Exists(tempPath + Path.GetFileName(fileList[0])))
								File.Delete(tempPath + Path.GetFileName(fileList[0]));
							File.Move(fileList[0], tempPath + Path.GetFileName(fileList[0]));
							return Path.GetFileName(fileList[0]);
						}
					}
					catch (ArchiveHelper.ArchiveException) {
					}
				}
			}
			finally {
				// Удаляем за собой созданную директорию
				if (Directory.Exists(uueFileName + extractDir))
					try {
						Directory.Delete(uueFileName + extractDir, true);
					}
					catch {
					}
			}
			return String.Empty;
		}

		public static Mime ExtractFromUue(Mime mimeMessage, string tempPath)
		{
			if ((mimeMessage.Attachments.Length == 0) &&
				(IsUue(mimeMessage))) {
				string shortFileName = ExtractFileFromUue(mimeMessage, tempPath);
				if (!String.IsNullOrEmpty(shortFileName)) {
					MimeEntity uueAttach = new MimeEntity();
					uueAttach.ContentType = MediaType_enum.Application_octet_stream;
					uueAttach.ContentDisposition = ContentDisposition_enum.Attachment;
					uueAttach.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
					uueAttach.ContentDisposition_FileName = shortFileName;
					uueAttach.ContentType_Name = shortFileName;
					uueAttach.DataFromFile(tempPath + shortFileName);
					if (mimeMessage.MainEntity.ContentType != MediaType_enum.Multipart_mixed) {
						mimeMessage.MainEntity.Data = null;
						mimeMessage.MainEntity.ContentType = MediaType_enum.Multipart_mixed;
					}
					mimeMessage.MainEntity.ChildEntities.Add(uueAttach);
				}
			}
			return mimeMessage;
		}
	}
}