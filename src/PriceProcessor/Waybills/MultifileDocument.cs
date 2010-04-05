using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills
{
	public class MultifileDocument
	{
		private const string MergedPrefix = "merged_";

		public static bool IsMergedDocument(string fileName)
		{
			return Path.GetFileName(fileName).StartsWith(MergedPrefix) ||
				Path.GetFileName(fileName).Contains(String.Format("({0}", MergedPrefix));
		}

		private static string GetPartialFileName(string prefix, string mergedFileName)
		{
			if (!IsMergedDocument(mergedFileName))
				return String.Empty;
			return Path.Combine(Path.GetDirectoryName(mergedFileName),
				prefix + Path.GetFileName(mergedFileName).Substring(MergedPrefix.Length));
		}

		public static string GetHeaderFileName(string mergedFileName)
		{
			return GetPartialFileName("h", mergedFileName);
		}

		public static string GetBodyFileName(string mergedFileName)
		{
			return GetPartialFileName("b", mergedFileName);
		}

		private static bool IsPartialFile(string filePath, string nameStartString)
		{
			return Path.GetFileNameWithoutExtension(filePath).StartsWith(nameStartString) &&
				Path.GetExtension(filePath).Equals(".dbf", StringComparison.OrdinalIgnoreCase);			
		}

		private static bool IsHeaderFile(string file)
		{
			return IsPartialFile(file, "h");
		}

		private static string GetSecondFile(string headerFile, IEnumerable<string> files)
		{
			var template = Path.GetFileName(headerFile).Substring(1);
			foreach (var file in files)
				if (file.EndsWith(template) && !headerFile.Equals(file, StringComparison.OrdinalIgnoreCase))
					return file;
			return String.Empty;
		}

		private static bool IsBodyFile(string file)
		{
			return IsPartialFile(file, "b");
		}

		private static string MergeFiles(string headerFilePath, string bodyFilePath)
		{
			var tableHeader = Dbf.Load(headerFilePath);
			var tableBody = Dbf.Load(bodyFilePath);

			var commonColumns = new List<string>();
			foreach (DataColumn column in tableHeader.Columns)
			{
				if (tableBody.Columns.Contains(column.ColumnName))
					commonColumns.Add(column.ColumnName);
				else
					tableBody.Columns.Add(column.ColumnName);
			}
			if (commonColumns.Count != 1)
				throw new Exception(String.Format(@"
При объединении двух DBF файлов возникла ошибка. Количество общих колонок отличается от 1.
Файл-заголовок: {0}
Файл-тело: {1}", headerFilePath, bodyFilePath));
			var idColumn = commonColumns[0];
			foreach (DataRow headerRow in tableHeader.Rows)
			{
				foreach (DataRow bodyRow in tableBody.Rows)
				{
					if (!headerRow[idColumn].Equals(bodyRow[idColumn]))
						continue;
					foreach (DataColumn column in tableHeader.Columns)
					{
						if (commonColumns.Contains(column.ColumnName))
							continue;
						bodyRow[column.ColumnName] = headerRow[column.ColumnName];
					}
				}
			}
			tableBody.AcceptChanges();
			// Path.GetFileName(headerFilePath).Substring(1) потому что первая буква "h" нам не нужна
			var mergedFileName = Path.Combine(Path.GetDirectoryName(headerFilePath),
				MergedPrefix + Path.GetFileName(headerFilePath).Substring(1));
			Dbf.Save(tableBody, mergedFileName);
			return mergedFileName;
		}

		public static List<string> Merge(IList<string> extractedFiles)
		{
			var resultList = new List<string>();
			for (var i = 0; i < extractedFiles.Count; i++)
			{
				var headerFile = String.Empty;
				var bodyFile = String.Empty;
				var file = extractedFiles[i];
				if (IsHeaderFile(file))
				{
					headerFile = file;
					bodyFile = GetSecondFile(headerFile, extractedFiles);
				}
				if (IsBodyFile(file))
				{
					bodyFile = file;
					headerFile = GetSecondFile(bodyFile, extractedFiles);
				}
				if (String.IsNullOrEmpty(headerFile) && String.IsNullOrEmpty(bodyFile))
				{
					resultList.Add(file);
					continue;
				}
				if (!String.IsNullOrEmpty(headerFile) && !String.IsNullOrEmpty(bodyFile))
				{
					var mergedFile = MergeFiles(headerFile, bodyFile);
					resultList.Add(mergedFile);
					i--;
				}
				extractedFiles.Remove(headerFile);
				extractedFiles.Remove(bodyFile);				
			}
			return resultList;
		}
	}
}
