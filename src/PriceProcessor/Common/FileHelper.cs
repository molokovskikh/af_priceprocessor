using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Inforoom.Common
{
	public sealed class FileHelper
	{
		//Add Path.DirectorySeparatorChar
		public static string NormalizeDir(string InputDir)
		{
			string result = Path.GetFullPath(InputDir);
			if ((result.Length > 0) && (result[result.Length - 1] != Path.DirectorySeparatorChar))
				result += Path.DirectorySeparatorChar;
			return result;
		}

		public static void FileDelete(string FileName)
		{
			int DeleteErrorCount = 0;
			bool DeleteSucces = false;
			do
			{
				try
				{
					File.Delete(FileName);
					DeleteSucces = true;
				}
				catch 
				{
					if (DeleteErrorCount < 10)
					{
						DeleteErrorCount++;
						System.Threading.Thread.Sleep(500);
					}
					else
						throw;
				}
			}
			while (!DeleteSucces);
		}
	}
}
