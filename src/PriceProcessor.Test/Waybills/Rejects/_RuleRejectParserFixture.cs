using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Rejects.Parser;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using PriceProcessor.Test.Waybills.Rejects.Infrastructure;
using Test.Support;
using Test.Support.Suppliers;
using Address = Inforoom.PriceProcessor.Waybills.Models.Address;
using Org = Inforoom.PriceProcessor.Waybills.Models.Org;

namespace PriceProcessor.Test.Waybills.Rejects
{
	[TestFixture]
	class RuleRejectParserFixture : IntegrationFixture2
	{
		public uint TestRawClientId { get; set; }
		public uint TestRawSupplierId { get; set; }

		[SetUp]
		public void Setup()
		{
			var testRawSupplier = TestSupplier.CreateNaked(session);
			session.Save(testRawSupplier);
			var testRawClient = TestClient.CreateNaked(session);
			session.Save(testRawClient);
			var testRawaddress = testRawClient.Addresses.FirstOrDefault();
			testRawaddress.Payer = testRawSupplier.Payer;

			session.Save(testRawaddress);
			session.Flush();
			TestRawClientId = testRawClient.Id;
			TestRawSupplierId = testRawSupplier.Id;
		}

		private string CopyToRightDirrectory(DocumentReceiveLog log)
		{
			var fi = new FileInfo(log.GetFileName());
			var str = fi.DirectoryName;
			if (!Directory.Exists(str)) {
				Directory.CreateDirectory(str);
			}
			File.Delete(fi.FullName);
			File.Copy(log.FileName, fi.FullName);
			return Directory.GetParent(str).FullName;
		}

		private void CreateRules(Supplier supplier, bool wrongRules = false)
		{
			var rejectDataParser = new RejectDataParser("TestRule", supplier);
			rejectDataParser.Lines.Add(new RejectParserLine(rejectDataParser, "CODE", "Code"));
			rejectDataParser.Lines.Add(new RejectParserLine(rejectDataParser, "NAME", "Product"));
			rejectDataParser.Lines.Add(new RejectParserLine(rejectDataParser, "PRICE", "Cost"));
			rejectDataParser.Lines.Add(new RejectParserLine(rejectDataParser, "QNTZAK", "Ordered"));
			if (wrongRules) {
				rejectDataParser.Lines.Add(new RejectParserLine(rejectDataParser, "SOME_WRONG_FIELD", "Rejected"));
			} else {
				rejectDataParser.Lines.Add(new RejectParserLine(rejectDataParser, "QNTREF", "Rejected"));
			}
			session.Save(rejectDataParser);
		}

		[Test]
		public void ParseByRules_rightReject()
		{
			session.Transaction.Begin();
			var testAddress = session.Query<Address>().FirstOrDefault(s => s.Client.Id == TestRawClientId);
			var testSupplier = session.Query<Supplier>().FirstOrDefault(s => s.Id == TestRawSupplierId);
			CreateRules(testSupplier);
			//Создаем лог, а затем отказ
			var log = new DocumentReceiveLog(testSupplier, testAddress);
			//Имя файла должно быть задано, так как от него будет зависеть работа парсера - сам парсер не проверяет лог на то, что он отказный
			log.FileName = @"..\..\data\rejects\RuleRejectParserFix.dbf";
			log.DocumentType = DocType.Reject;
			session.Save(log);
			session.Transaction.Commit();
			session.Transaction.Begin();

			//дирректория для удаления
			var pathToRemove = CopyToRightDirrectory(log);

			new WaybillService().ParseWaybill(new[] {log.Id});
			var reject = session.Query<RejectHeader>().FirstOrDefault(s => s.Log.Id == log.Id);

			//Проверяем правильность парсинга
			Assert.That(reject.Lines.Count, Is.EqualTo(3));
			Assert.That(reject.Parser, Is.EqualTo("TestRule"));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Хондроксид мазь 5% 30г"));
			Assert.That(line.Code, Is.EqualTo("04949"));
			Assert.That(line.Cost, Is.EqualTo(305.10));
			Assert.That(line.Ordered, Is.EqualTo(1));
			Assert.That(line.Rejected, Is.EqualTo(1));

			if (Directory.Exists(pathToRemove)) {
				Directory.Delete(pathToRemove, true);
			}
		}

		[Test]
		public void ParseByRules_wrongFieldInFile()
		{
			session.Transaction.Begin();
			var testAddress = session.Query<Address>().FirstOrDefault(s => s.Client.Id == TestRawClientId);
			var testSupplier = session.Query<Supplier>().FirstOrDefault(s => s.Id == TestRawSupplierId);

			CreateRules(testSupplier, true);

			//Создаем лог, а затем отказ
			var log = new DocumentReceiveLog(testSupplier, testAddress);
			//Имя файла должно быть задано, так как от него будет зависеть работа парсера - сам парсер не проверяет лог на то, что он отказный
			log.FileName = @"..\..\data\rejects\RuleRejectParserFix.dbf";
			log.DocumentType = DocType.Reject;

			session.Save(log);
			session.Transaction.Commit();
			session.Transaction.Begin();

			//дирректория для удаления
			var pathToRemove = CopyToRightDirrectory(log);

			new WaybillService().ParseWaybill(new[] {log.Id});
			var reject = session.Query<RejectHeader>().FirstOrDefault(s => s.Log.Id == log.Id);

			//Проверяем отсутствие отказа
			Assert.That(reject, Is.Null);

			if (Directory.Exists(pathToRemove)) {
				Directory.Delete(pathToRemove, true);
			}
		}

		[Test]
		public void ParseByRules_noRules()
		{
			session.Transaction.Begin();
			var testAddress = session.Query<Address>().FirstOrDefault(s => s.Client.Id == TestRawClientId);
			var testSupplier = session.Query<Supplier>().FirstOrDefault(s => s.Id == TestRawSupplierId);

			//Создаем лог, а затем отказ
			var log = new DocumentReceiveLog(testSupplier, testAddress);
			//Имя файла должно быть задано, так как от него будет зависеть работа парсера - сам парсер не проверяет лог на то, что он отказный
			log.FileName = @"..\..\data\rejects\RuleRejectParserFix.dbf";
			log.DocumentType = DocType.Reject;

			session.Save(log);
			session.Transaction.Commit();
			session.Transaction.Begin();

			//дирректория для удаления
			var pathToRemove = CopyToRightDirrectory(log);

			new WaybillService().ParseWaybill(new[] {log.Id});
			var reject = session.Query<RejectHeader>().FirstOrDefault(s => s.Log.Id == log.Id);

			//Проверяем отсутствие отказа
			Assert.That(reject, Is.Null);

			if (Directory.Exists(pathToRemove)) {
				Directory.Delete(pathToRemove, true);
			}
		}
	}
}