using System;
using System.Diagnostics;
using System.IO;

namespace Inforoom.Common
{
	/// <summary>
	/// Summary description for Archive.
	/// </summary>
	public sealed class ArchiveHelper
	{
		//������������ ����� �������� �������� � ��������� ����������
		public const int MaxArchiveTimeOut = 180 * 1000;

		private static string[] FileType = new string[5] {".zip", ".exe", ".arj", ".gz", ".rar"};

		private static string SevenZipExePath = @"C:\Program Files\7-Zip\7z.exe";

        public class ArchiveException : Exception
        {
            private int _exitCode;

			public ArchiveException()
				: base("������� ���������������� ���������� ������������� �� ��������.")
			{
				this._exitCode = -1;
			}

			public ArchiveException(int ExitCode)
				: base(String.Format("������� ���������������� ���������� � ������� : {0}.", ExitCode))
            {
                this._exitCode = ExitCode;
			}

			public int ExitCode
			{
				get { return _exitCode; }
			}
        }
        /// <summary>
        /// ��������� ��� ����� �� ������ � ��������� �����
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="ExtractFolder"></param>
		public static void Extract(string ArchFileName, string ExtractMask, string ExtractFolder)
		{
			Process p = Process.Start(SevenZipExePath, String.Format("x -y \"{0}\"  \"-o{1}\" \"{2}\" -r", ArchFileName, ExtractFolder, ExtractMask));
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
        /// ���������, ��� ���� �������� �������
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
			Process p = Process.Start(SevenZipExePath, String.Format("t -y \"{0}\"", ArchFileName));
			bool Stopped = p.WaitForExit(MaxArchiveTimeOut);
			if (!Stopped)
				try { p.Kill(); } catch { }
			return (Stopped && (p.ExitCode == 0));
        }
	}
}
