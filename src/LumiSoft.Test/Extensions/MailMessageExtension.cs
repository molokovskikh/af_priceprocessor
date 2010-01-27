using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor;
using LumiSoft.Net.Mail;
using LumiSoft.Net.MIME;
using System.IO;

namespace LumiSoftTest.Extensions
{
    public static class MailMessageExtension
    {
        public static string GetFilename(this Mail_Message message)
        {
			try
			{
				if ((message.ContentDisposition != null) && !String.IsNullOrEmpty(message.ContentDisposition.Param_FileName))
					return FileHelper.NormalizeFileName(Path.GetFileName(FileHelper.NormalizeFileName(message.ContentDisposition.Param_FileName)));
			}
			catch {}
			try
			{
				if ((message.ContentType != null) && !String.IsNullOrEmpty(message.ContentType.Param_Name))
					return FileHelper.NormalizeFileName(Path.GetFileName(FileHelper.NormalizeFileName(message.ContentType.Param_Name)));
			}
			catch {}
        	return null;
        }

        public static IEnumerable<MIME_Entity> GetValidAttachements(this Mail_Message message)
        {
            return message.Attachments.Where(m => !String.IsNullOrEmpty(m.GetFilename()));
        }

        public static IEnumerable<string> GetAttachmentFilenames(this Mail_Message message)
        {
            var resultList = new List<string>();
            var attachments = message.GetValidAttachements();
            foreach (var entity in attachments)
                resultList.Add(entity.GetFilename());
            return resultList;
        }
    }
}
