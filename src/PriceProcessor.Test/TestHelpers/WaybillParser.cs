using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.TestHelpers
{
	// Класс для тестов, чтобы не повторять ошибок вроде "забыл добавить новый парсер в DetectParser()"
	public class WaybillParser
	{
		public static Document Parse(string filePath, DocumentReceiveLog log = null)
		{
			var detector = new WaybillFormatDetector();
			if (!File.Exists(filePath))
				filePath = Path.Combine(@"..\..\Data\Waybills\", filePath);
			var parser = detector.DetectParser(filePath, log);
			var parsers = detector.GetSuitableParsers(filePath, log).ToList();
			if (parsers.Count > 1)
				throw new Exception($"Для разбора данного формата подходит более одного парсера, {parsers.Implode()}");
			if (parser == null)
				return null;
			log = log ??
				new DocumentReceiveLog(new Supplier(), new Address { Client = new Client() });
			if (log.ClientCode == null) {
				log.ClientCode = 0;
			}
			var document = new Document(log, parser.GetType().Name);
			var doc = parser.Parse(filePath, document);
			if (doc != null) {
				doc.CalculateValues();
			}
			return doc;
		}

		public static List<DocumentReceiveLog> GetFilesForParsing(ISession session, params string[] filePaths)
		{
			var client = TestClient.Create(session);
			var supplier = TestSupplier.Create(session);
			var resultList = new List<uint>();
			foreach (var filePath in filePaths) {
				var file = filePath;
				if (!File.Exists(file))
					file = Path.Combine(@"..\..\Data\Waybills\multifile", filePath);

				var log = new TestDocumentLog(supplier, client, Path.GetFileName(filePath));
				session.Save(log);
				resultList.Add(log.Id);
				var clientDir = Path.Combine(Settings.Default.DocumentPath, log.Address.Id.ToString().PadLeft(3, '0'));
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