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
			return string.Empty;
		}

		public static void ExtractFromArhive(string ArchName, string TempDir)
		{
			if (Directory.Exists(TempDir))
				Directory.Delete(TempDir, true);
			Directory.CreateDirectory(TempDir);
			ArchiveHelper.Extract(ArchName, "*.*", TempDir + Path.DirectorySeparatorChar);
		}

		public static void ExtractFromArhive(string ArchName, string TempDir, string password)
		{
			if (Directory.Exists(TempDir))
				Directory.Delete(TempDir, true);
			Directory.CreateDirectory(TempDir);
			ArchiveHelper.Extract(ArchName, "*.*", TempDir + Path.DirectorySeparatorChar, password);
		}

		public static string GetSuccessAddition(string ArchName, string FileName)
		{
			return String.Format("{0} > {1}", Path.GetFileName(ArchName), Path.GetFileName(FileName));
		}

		public static string NormalizeFileName(string InputFilename)
		{
			var PathPart = String.Empty;
			foreach (var ic in Path.GetInvalidPathChars()) {
				InputFilename = InputFilename.Replace(ic.ToString(), "");
			}
			//Пытаемся найти последний разделитель директории в пути
			var EndDirPos = InputFilename.LastIndexOfAny(
				new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
			if (EndDirPos > -1) {
				PathPart = InputFilename.Substring(0, EndDirPos + 1);
				InputFilename = InputFilename.Substring(EndDirPos + 1);
			}
			foreach (var ic in Path.GetInvalidFileNameChars()) {
				InputFilename = InputFilename.Replace(ic.ToString(), "");
			}
			return (PathPart + InputFilename);
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
	}
}