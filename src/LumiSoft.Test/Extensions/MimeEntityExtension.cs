using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor;
using LumiSoft.Net.MIME;
using System.IO;

namespace LumiSoftTest.Extensions
{
    public static class MimeEntityExtension
    {
        public static string GetFilename(this MIME_Entity entity)
        {
			try
			{
				if ((entity.ContentDisposition != null) && !String.IsNullOrEmpty(entity.ContentDisposition.Param_FileName))
					return FileHelper.NormalizeFileName(Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentDisposition.Param_FileName)));
			}
			catch {}
			try
			{
				if ((entity.ContentType != null) && !String.IsNullOrEmpty(entity.ContentType.Param_Name))
					return FileHelper.NormalizeFileName(Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentType.Param_Name)));
			}
			catch {}
			return null;
        }
    }
}
