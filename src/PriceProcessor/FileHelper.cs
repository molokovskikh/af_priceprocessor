using System;
using System.IO;
using Inforoom.Common;
using log4net;

namespace Inforoom.PriceProcessor
{
	public class FileHelper
	{
		private static ILog _log = LogManager.GetLogger(typeof(FileHelper));

		public static bool CheckMask(string shortFileName, string mask)
		{
			Func<string, string, bool> checkAction = (fileName, fileMask) => {
				return (WildcardsHelper.IsWildcards(fileMask) && WildcardsHelper.Matched(fileMask, fileName)) ||
					(String.Compare(fileName, fileMask, true) == 0) ||
					fileName.Equals(fileMask, StringComparison.OrdinalIgnoreCase);
			};

			var matched = checkAction(shortFileName, mask);
			if (!matched)
				matched = checkAction(shortFileName.Replace('?', '_'), mask);
			return matched;
		}

		public static string FindFromArhive(string TempDir, string ExtrMask)
		{
			var files = Directory.GetFiles(TempDir + Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);
			foreach (var file in files) {
				if (CheckMask(Path.GetFileName(file), ExtrMask))
					return file;
			}
			return String.Empty;
		}

		public static string[] TryExtractArchive(string file, string dstDir, string password = null)
		{
			if (ArchiveHelper.IsArchive(file)) {
				if (ArchiveHelper.TestArchive(file)) {
					return ExtractFromArhive(file, dstDir, password);
				}
			}
			return null;
		}

		public static string[] ExtractFromArhive(string file, string dstDir, string password = null)
		{
			global::Common.Tools.FileHelper.InitDir(dstDir);
			ArchiveHelper.Extract(file, "*.*", dstDir + Path.DirectorySeparatorChar, password);
			return Directory.GetFiles(dstDir, "*.*", SearchOption.AllDirectories);
		}

		public static void Safe(Action action)
		{
			try {
				action();
			}
			catch (Exception e) {
				_log.Error("Ошибка на которую можно забить", e);
			}
		}

		public static bool ProcessArchiveIfNeeded(string file, string sufix, string password = null)
		{
			var result = true;
			//Является ли скачанный файл корректным, если нет, то обрабатывать не будем
			if (ArchiveHelper.IsArchive(file)) {
				if (ArchiveHelper.TestArchive(file, password)) {
					try {
						ExtractFromArhive(file, file + sufix, password);
					}
					catch (ArchiveHelper.ArchiveException) {
						result = false;
					}
				}
				else
					result = false;
			}
			return result;
		}
	}
}