using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Test.Support;
using Test.Support.Suppliers;

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
			if (doc != null) {
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
			var client = TestClient.Create();
			var supplier = TestSupplier.Create();
			var resultList = new List<uint>();
			foreach (var filePath in filePaths) {
				var file = filePath;
				if (!File.Exists(file))
					file = Path.Combine(@"..\..\Data\Waybills\multifile", filePath);

				var log = new TestDocumentLog(supplier, client, Path.GetFileName(filePath));
				using (new SessionScope()) {
					log.SaveAndFlush();
				}
				resultList.Add(log.Id);
				var clientDir = Path.Combine(Settings.Default.DocumentPath, log.AddressId.ToString().PadLeft(3, '0'));
				var documentDir = Path.Combine(clientDir, DocumentType.Waybill + "s");
				var name = String.Format("{0}_{1}({2}){3}",
					log.Id,
					supplier.Name,
					Path.GetFileNameWithoutExtension(file),
					Path.GetExtension(file));

				Common.Tools.FileHelper.CreateDirectoryRecursive(documentDir);
				File.Copy(file, Path.Combine(documentDir, name));
			}
			return DocumentReceiveLog.LoadByIds(resultList.ToArray());
		}
	}
}