using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills
{
	public enum WaybillSourceType
	{
		[Description("FTP АК 'Инфорум'")] FtpInforoom = 4,
		[Description("FTP Поставщика")] FtpSupplier = 5,
		[Description("Email")] Email = 1,
	}

	public class WaybillHelper
	{
		public static void CopyToClientDir(string srcFileName, string destFileNameFormatString)
		{
			if (MultifileDocument.IsMergedDocument(srcFileName)) {
				var headerFile = MultifileDocument.GetHeaderFileName(srcFileName);
				var destFile = String.Format(destFileNameFormatString, Path.GetFileNameWithoutExtension(headerFile));
				DeleteIfExists(destFile);
				File.Move(headerFile, destFile);

				var bodyFile = MultifileDocument.GetBodyFileName(srcFileName);
				destFile = String.Format(destFileNameFormatString, Path.GetFileNameWithoutExtension(bodyFile));
				DeleteIfExists(destFile);
				File.Move(bodyFile, destFile);

				destFile = String.Format(destFileNameFormatString, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(srcFileName)));
				DeleteIfExists(destFile);
				File.Move(srcFileName, destFile);
			}
			else
				File.Move(srcFileName, String.Format(destFileNameFormatString, Path.GetFileNameWithoutExtension(srcFileName)));
		}

		private static void DeleteIfExists(string filePath)
		{
			if (File.Exists(filePath))
				try {
					File.Delete(filePath);
				}
				catch {
				}
		}
	}
}