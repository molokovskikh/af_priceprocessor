using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
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
			var doc = Parse(filePath, null);
			return doc;
		}

		public static Document Parse(string filePath, DocumentReceiveLog documentLog)
		{
			var detector = new WaybillFormatDetector();
			if (!File.Exists(filePath))
				filePath = Path.Combine(@"..\..\Data\Waybills\", filePath);
			var parser = detector.DetectParser(filePath, documentLog);
			if (!detector.IsSpecialParser(parser)) { 
				CheckUniqueDbfParser(filePath);
				CheckUniqueSstParser(filePath);
			}
			if (parser == null)
				return null;
			var doc = parser.Parse(filePath, new Document());
			if(doc != null)
			{
				doc.SetProductId();
				doc.CalculateValues();
			}
			return doc;
		}

		private static void CheckUniqueDbfParser(string file)
		{
			if (Path.GetExtension(file.ToLower()) != ".dbf")
				return;

			var parsers = WaybillFormatDetector.GetSuitableParsers(file, "Dbf").ToList();

			if (parsers.Count > 1)
				throw new Exception(String.Format("Для разбора данного формата подходит более одного парсера, {0}", parsers.Implode()));
		}

		private static void CheckUniqueSstParser(string file)
		{
			if (Path.GetExtension(file.ToLower()) != ".sst")
				return;

			var parsers = WaybillFormatDetector.GetSuitableParsers(file, "Sst").ToList();

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
			uint supplirtId = 5;
			uint clientId = 363;
			uint addressId = 363;
			foreach (var filePath in filePaths)
			{
				var file = filePath;
				if (!File.Exists(file))
					file = Path.Combine(@"..\..\Data\Waybills\multifile", filePath);
				With.Connection(connection => {
					var cmdInsert = new MySqlCommand(@"
INSERT INTO logs.document_logs (FirmCode, ClientCode, AddressId, FileName, DocumentType)
VALUES (?FirmCode, ?ClientCode, ?AddressId, ?FileName, ?DocumentType); select last_insert_id();", connection);

					cmdInsert.Parameters.AddWithValue("?FirmCode", supplirtId);
					cmdInsert.Parameters.AddWithValue("?ClientCode", clientId);
					cmdInsert.Parameters.AddWithValue("?AddressId", addressId);
					cmdInsert.Parameters.AddWithValue("?FileName", Path.GetFileName(file));
					cmdInsert.Parameters.AddWithValue("?DocumentType", DocType.Waybill);
					documentLogId = Convert.ToUInt32(cmdInsert.ExecuteScalar());
				});
				resultList.Add(documentLogId);
				var clientDir = Path.Combine(Settings.Default.DocumentPath, clientId.ToString().PadLeft(3, '0'));
				var documentDir = Path.Combine(clientDir, DocumentType.Waybill + "s");
				var name = String.Format("{0}_{1}({2}){3}",
					documentLogId,
					"Протек-15",
					Path.GetFileNameWithoutExtension(file),
					Path.GetExtension(file));
				CreateClientDirectory(clientId);
				File.Copy(file, Path.Combine(documentDir, name));
			}
			return DocumentReceiveLog.LoadByIds(resultList.ToArray());
		}

		private static void CreateClientDirectory(uint clientId)
		{
			var directory = Settings.Default.DocumentPath;
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
