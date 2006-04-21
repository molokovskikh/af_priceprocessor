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

        /// <summary>
        /// ��������� ��� ����� �� ������ � ��������� �����
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="ExtractFolder"></param>
		public static void Extract(string FileName, string ExtractFolder)
		{
			Process.Start("WinRAR", String.Format("x -inul -y {0} {1}", FileName, ExtractFolder));
		}

        /// <summary>
        /// ���������, ��� ���� �������� �������
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
		public static bool IsArchive(string FileName)
		{
            string FileExtension = Path.GetExtension(FileName).ToLower();
            return (Array.IndexOf(FileType, FileExtension) > -1);
		}
	}
}
