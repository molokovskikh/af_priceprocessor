using System;
using System.IO;
using Inforoom.Common;
using log4net;

namespace Inforoom.PriceProcessor
{
	public class FileHelper
	{
		private static ILog _log = LogManager.GetLogger(typeof (FileHelper));

		public static string FindFromArhive(string TempDir, string ExtrMask)
		{
			var ExtrFiles = Directory.GetFiles(TempDir + Path.DirectorySeparatorChar, ExtrMask, SearchOption.AllDirectories);
			if (ExtrFiles.Length > 0)
				return ExtrFiles[0];
			return String.Empty;
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
			foreach (var ic in Path.GetInvalidPathChars())
			{
				InputFilename = InputFilename.Replace(ic.ToString(), "");
			}
			//Пытаемся найти последний разделитель директории в пути
			var EndDirPos = InputFilename.LastIndexOfAny(
				new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar } );
			if (EndDirPos > -1)
			{
				PathPart = InputFilename.Substring(0, EndDirPos + 1);
				InputFilename = InputFilename.Substring(EndDirPos + 1);
			}
			foreach (var ic in Path.GetInvalidFileNameChars())
			{
				InputFilename = InputFilename.Replace(ic.ToString(), "");
			}
			return (PathPart + InputFilename);
		}

		public static void CopyStreams(Stream input, Stream output)
		{
			const int size = 4096;
			var bytes = new byte[4096];
			int numBytes;
			while ((numBytes = input.Read(bytes, 0, size)) > 0)
				output.Write(bytes, 0, numBytes);
		}

		public static void Safe(Action action)
		{
			try
			{
				action();
			}
			catch(Exception e)
			{
				_log.Error("Ошибка на которую можно забить", e);
			}
		}
	}
}