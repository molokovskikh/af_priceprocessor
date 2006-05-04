using System;
using System.Diagnostics;
using System.IO;

namespace Inforoom.Downloader
{
	/// <summary>
	/// Summary description for Archive.
	/// </summary>
	public sealed class ArchiveHlp
	{
		private static string[] FileType = new string[5] {".zip", ".exe", ".arj", ".gz", ".rar"};

        public class ArchiveException : Exception
        {
            public int ExitCode;
            public ArchiveException(int ExitCode)
            {
                this.ExitCode = ExitCode;
            }

            public override string ToString()
            {
                return base.ToString() + " ExitCode : " + ExitCode.ToString();
            }

        }
        /// <summary>
        /// Извлекает все файлы из архива в указанную папку
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="ExtractFolder"></param>
		public static void Extract(string ArchFileName, string ExtractMask, string ExtractFolder)
		{
            Process p = Process.Start("WinRAR", String.Format("x -inul -ibck -y \"{0}\" {1} \"{2}\"", ArchFileName, ExtractMask, ExtractFolder));
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new ArchiveException(p.ExitCode);
		}

        /// <summary>
        /// Проверяет, что файл является архивом
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
		public static bool IsArchive(string ArchFileName)
		{
            string FileExtension = Path.GetExtension(ArchFileName).ToLower();
            return (Array.IndexOf(FileType, FileExtension) > -1);
		}

        public static bool TestArchive(string ArchFileName)
        {
            Process p = Process.Start("WinRAR", String.Format("t -inul -ibck -y \"{0}\"", ArchFileName));
            p.WaitForExit();
            return (p.ExitCode == 0);
        }
	}
}
