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
		//Максимальное время ожидания операции с процессом распаковки
		public const int MaxArchiveTimeOut = 180 * 1000;

		private static string[] FileType = new string[5] {".zip", ".exe", ".arj", ".gz", ".rar"};

        public class ArchiveException : Exception
        {
            private int _exitCode;

			public ArchiveException()
				: base("Процесс разархивирования завершился принудительно по таймауту.")
			{
				this._exitCode = -1;
			}

			public ArchiveException(int ExitCode)
				: base(String.Format("Процесс разархивирования завершился с ошибкой : {0}.", ExitCode))
            {
                this._exitCode = ExitCode;
			}

			public int ExitCode
			{
				get { return _exitCode; }
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
			bool Stopped = p.WaitForExit(MaxArchiveTimeOut);
			if (Stopped)
			{
				if (p.ExitCode != 0)
					throw new ArchiveException(p.ExitCode);
			}
			else
			{
				try { p.Kill(); } catch { }
				throw new ArchiveException();
			}
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
			bool Stopped = p.WaitForExit(MaxArchiveTimeOut);
			if (!Stopped)
				try { p.Kill(); } catch { }
			return (Stopped && (p.ExitCode == 0));
        }
	}
}
