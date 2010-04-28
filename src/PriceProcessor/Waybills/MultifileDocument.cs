using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader;
using log4net;

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

		private static bool IsPartialFile(DocumentLog documentLog, string nameStartString)
		{
			return Path.GetFileNameWithoutExtension(documentLog.FileName).StartsWith(nameStartString) &&
				Path.GetExtension(documentLog.FileName).Equals(".dbf", StringComparison.OrdinalIgnoreCase);			
		}

		private static bool IsHeaderFile(DocumentLog file)
		{
			return IsPartialFile(file, "h");
		}

		private static DocumentLog GetSecondFile(DocumentLog documentLog, IEnumerable<DocumentLog> files)
		{
			var headerFile = documentLog.FileName;
			var template = Path.GetFileName(headerFile).Substring(1);
			var directory = Path.GetDirectoryName(headerFile);
			foreach (var file in files)
			{
				if (file.FileName.EndsWith(template) &&
					!headerFile.Equals(file.FileName, StringComparison.OrdinalIgnoreCase) &&
					Path.GetDirectoryName(file.FileName).Equals(directory))
					return file;
			}
			return null;
		}

		private static bool IsBodyFile(DocumentLog documentLog)
		{
			return IsPartialFile(documentLog, "b");
		}

		private static string MergeFiles(DocumentLog headerFile, DocumentLog bodyFile)
		{
			var tableHeader = Dbf.Load(headerFile.GetFileName());
			var tableBody = Dbf.Load(bodyFile.GetFileName());

			var commonColumns = new List<string>();
			foreach (DataColumn column in tableHeader.Columns)
			{
				if (tableBody.Columns.Contains(column.ColumnName))
					commonColumns.Add(column.ColumnName);
				else
					tableBody.Columns.Add(column.ColumnName);
			}
			var headerColumnName = String.Empty;
			var bodyColumnName = String.Empty;
			if (commonColumns.Count != 1)
			{
				headerColumnName = "DOCNUMBER";
				if (!tableHeader.Columns.Contains(headerColumnName))
					headerColumnName = "DOCNUM";

				bodyColumnName = "DOCNUMDER";
				if (!tableBody.Columns.Contains(bodyColumnName))
					bodyColumnName = "DOCNUMBER";
			}
			else
			{
				headerColumnName = commonColumns[0];
				bodyColumnName = commonColumns[0];
			}

			if (!tableHeader.Columns.Contains(headerColumnName) || !tableBody.Columns.Contains(bodyColumnName))
				throw new Exception(String.Format(@"
При объединении двух DBF файлов возникла ошибка. Количество общих колонок отличается от 1 и нет колонок {2} или {3}.
Файл-заголовок: {0}
Файл-тело: {1}", headerFile.FileName, bodyFile.FileName, headerColumnName, bodyColumnName));
			
			foreach (DataRow headerRow in tableHeader.Rows)
			{
				foreach (DataRow bodyRow in tableBody.Rows)
				{
					if (!headerRow[headerColumnName].Equals(bodyRow[bodyColumnName]))
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
			var mergedFileName = Path.Combine(Path.GetTempPath(), MergedPrefix + Path.GetFileName(headerFile.FileName).Substring(1));
			if (File.Exists(mergedFileName))
				File.Delete(mergedFileName);
			Dbf.Save(tableBody, mergedFileName);
			return mergedFileName;
		}

		public static IList<DocumentForParsing> Merge(uint[] ids)
		{
			DocumentLog headerFile = null;
			DocumentLog bodyFile = null;
			try
			{
				var resultList = new List<DocumentForParsing>();
				var documents = ids.Select(id => ActiveRecordBase<DocumentLog>.Find(id)).ToList();
				
				for (var i = 0; i < documents.Count; i++)
				{
					headerFile = null;
					bodyFile = null;
					var file = documents[i];
					if (IsHeaderFile(file))
					{
						headerFile = file;
						bodyFile = GetSecondFile(headerFile, documents);
					}
					if (IsBodyFile(file))
					{
						bodyFile = file;
						headerFile = GetSecondFile(bodyFile, documents);
					}
					if ((headerFile != null) && (bodyFile != null))
					{
						var mergedFile = MergeFiles(headerFile, bodyFile);
						resultList.Add(new DocumentForParsing(file, mergedFile));
						documents.Remove(headerFile);
						documents.Remove(bodyFile);
						i--;
					}
					else
						resultList.Add(new DocumentForParsing(file));						
				}
				return resultList;
			}
			catch(Exception e)
			{
				var _log = LogManager.GetLogger(typeof(MultifileDocument));
				_log.Error("Ошибка при слиянии многофайловых накладных", e);
				WaybillService.SaveWaybill(headerFile.GetFileName());
				WaybillService.SaveWaybill(bodyFile.GetFileName());
				return ids.Select(id => new DocumentForParsing(ActiveRecordBase<DocumentLog>.Find(id))).ToList();;
			}
		}

		public static void DeleteMergedFiles(string fileName)
		{
			if (IsMergedDocument(fileName) && File.Exists(fileName))
				File.Delete(fileName);			
		}

		public static void DeleteMergedFiles(IEnumerable<string> filePaths)
		{
			foreach (var file in filePaths)
				DeleteMergedFiles(file);
		}

		public static void DeleteMergedFiles(IEnumerable<DocumentForParsing> filePaths)
		{
			foreach (var file in filePaths)
				DeleteMergedFiles(file.FileName);
		}
	}
}
