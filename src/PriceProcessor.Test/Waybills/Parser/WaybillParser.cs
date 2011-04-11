using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using PriceProcessor.Test.Waybills.Parser.Multifile;
using Test.Support;

namespace PriceProcessor.Test.Waybills.Parser
{
	// Класс для тестов, чтобы не повторять ошибок вроде "забыл добавить новый парсер в DetectParser()"
	public class WaybillParser
	{
		public static Document Parse(string filePath)
		{
			return Parse(filePath, null);
		}

		public static Document Parse(string filePath, DocumentReceiveLog documentLog)
		{
			var detector = new WaybillFormatDetector();
			if (!File.Exists(filePath))
				filePath = Path.Combine(@"..\..\Data\Waybills\", filePath);
			var parser = detector.DetectParser(filePath, documentLog);
			if (!(parser is Avesta_6256_SpecialParser) && !(parser is KazanFarmDbfParser))
				CheckUniqueDbfParser(filePath);
			if (parser == null)
				return null;
			return parser.Parse(filePath, new Document());
		}

		private static void CheckUniqueDbfParser(string file)
		{
			if (Path.GetExtension(file.ToLower()) != ".dbf")
				return;

			var parsers = WaybillFormatDetector.GetSuitableParsers(file, "Dbf").ToList();

			if (parsers.Count > 1)
				throw new Exception(String.Format("Для разбора данного формата подходит более одного парсера, {0}", parsers.Implode()));
		}

		public static IDocumentParser GetParserType(string filePath, DocumentReceiveLog documentLog)
		{
			var detector = new WaybillFormatDetector();
			return detector.DetectParser(filePath, documentLog);
		}

		public static List<DocumentReceiveLog> GetFilesForParsing(params string[] filePaths)
		{
			var resultList = new List<uint>();
			uint documentLogId = 0;
			uint clientCode = 5;
			foreach (var filePath in filePaths)
			{
				var file = filePath;
				if (!File.Exists(file))
					file = Path.Combine(@"..\..\Data\Waybills\multifile", filePath);
				With.Connection(connection => {
					var cmdInsert = new MySqlCommand(@"
INSERT INTO logs.document_logs (FirmCode, ClientCode, FileName, DocumentType)
VALUES (?FirmCode, ?ClientCode, ?FileName, ?DocumentType); select last_insert_id();", connection);

					cmdInsert.Parameters.AddWithValue("?FirmCode", clientCode);
					cmdInsert.Parameters.AddWithValue("?ClientCode", clientCode);
					cmdInsert.Parameters.AddWithValue("?FileName", Path.GetFileName(file));
					cmdInsert.Parameters.AddWithValue("?DocumentType", DocType.Waybill);
					documentLogId = Convert.ToUInt32(cmdInsert.ExecuteScalar());
				});
				resultList.Add(documentLogId);
				var clientDir = Path.Combine(Settings.Default.FTPOptBoxPath, clientCode.ToString().PadLeft(3, '0'));
				var documentDir = Path.Combine(clientDir, DocumentType.Waybill + "s");
				var name = String.Format("{0}_{1}({2}){3}",
					documentLogId,
					"Протек-15",
					Path.GetFileNameWithoutExtension(file),
					Path.GetExtension(file));
				CreateClientDirectory(clientCode);
				File.Copy(file, Path.Combine(documentDir, name));
			}
			return DocumentReceiveLog.LoadByIds(resultList.ToArray());
		}

		private static void CreateClientDirectory(uint clientId)
		{
			var directory = Settings.Default.FTPOptBoxPath;
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			directory = Path.Combine(directory, clientId.ToString().PadLeft(3, '0'));
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			directory = Path.Combine(directory, DocType.Waybill + "s");
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
		}
	}
}
