using System;
using System.Collections.Generic;
using Common.Tools;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.IO;
using Test.Support;

namespace PriceProcessor.Test.Waybills.Parser.Multifile
{
	[TestFixture]
	public class AptekaHoldingParserFixture
	{
		private AptekaHoldingParser parser;
		private Document document;

		[SetUp]
		public void Setup()
		{
			TestHelper.RecreateDirectories();
			parser = new AptekaHoldingParser();
			document = new Document();
		}

		private void CreateClientDirectory(uint clientId)
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

		private List<DocumentReceiveLog> GetFilesForParsing(params string[] filePaths)
		{
			var resultList = new List<uint>();
			uint documentLogId = 0;
			uint clientCode = 5;
			foreach (var filePath in filePaths)
			{
				With.Connection(connection => {
					var cmdInsert = new MySqlCommand(@"
INSERT INTO logs.document_logs (FirmCode, ClientCode, FileName, DocumentType)
VALUES (?FirmCode, ?ClientCode, ?FileName, ?DocumentType); select last_insert_id();", connection);

					cmdInsert.Parameters.AddWithValue("?FirmCode", clientCode);
					cmdInsert.Parameters.AddWithValue("?ClientCode", clientCode);
					cmdInsert.Parameters.AddWithValue("?FileName", Path.GetFileName(filePath));
					cmdInsert.Parameters.AddWithValue("?DocumentType", DocType.Waybill);
					documentLogId = Convert.ToUInt32(cmdInsert.ExecuteScalar());
				});
				resultList.Add(documentLogId);
				var clientDir = Path.Combine(Settings.Default.WaybillsPath, clientCode.ToString().PadLeft(3, '0'));
				var documentDir = Path.Combine(clientDir, DocumentType.Waybill + "s");
				var name = String.Format("{0}_{1}({2}){3}",
					documentLogId,
					"Протек-15",
					Path.GetFileNameWithoutExtension(filePath),
					Path.GetExtension(filePath));
				CreateClientDirectory(clientCode);
				File.Copy(filePath, Path.Combine(documentDir, name));
			}
			return DocumentReceiveLog.LoadByIds(resultList.ToArray());
		}

		[Test]
		public void Parse()
		{
			var files = GetFilesForParsing(
                @"..\..\Data\Waybills\multifile\h271433.dbf",
				@"..\..\Data\Waybills\multifile\b271433.dbf"
			);
			var mergedFiles = MultifileDocument.Merge(files);
			Assert.That(mergedFiles.Count, Is.EqualTo(1));

			parser.Parse(mergedFiles[0].FileName, document);
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00000271433/0"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("26/03/2010")));
			Assert.That(document.Lines.Count, Is.EqualTo(6));
			Assert.That(document.Lines[0].Code, Is.EqualTo("30042"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Зинерит пор. д/приг. р-ра фл. с раствор. 30мл Нидерланды"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Нидерланды"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Astellas Pharma Europe B.V."));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(266.10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(292.71));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(266.10));			
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.05.2012"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС.NL.ФМ09.Д00778"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("09E20/02"));
		}

		[Test]
		public void Check_file_format()
		{
			var files = GetFilesForParsing (
                @"..\..\Data\Waybills\multifile\h271433.dbf",
				@"..\..\Data\Waybills\multifile\b271433.dbf"
			);

			var mergedFiles = MultifileDocument.Merge(files);			
			Assert.That(mergedFiles.Count, Is.EqualTo(1));
				
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\890579.dbf")));
			Assert.IsTrue(AptekaHoldingParser.CheckFileFormat(Dbf.Load(mergedFiles[0].FileName)));
		}

		[Test]
		public void Parse_with_znvls()
		{
			var files = GetFilesForParsing (
				@"..\..\Data\Waybills\multifile\h150410_46902_.dbf",
				@"..\..\Data\Waybills\multifile\b150410_46902_.dbf"
			);

			var mergedFiles = MultifileDocument.Merge(files);
			Assert.That(mergedFiles.Count, Is.EqualTo(1));
			parser.Parse(mergedFiles[0].FileName, document);
			Assert.That(document.ProviderDocumentId, Is.EqualTo("46902"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("15/04/2010")));
			Assert.That(document.Lines.Count, Is.EqualTo(9));
			Assert.That(document.Lines[0].Code, Is.EqualTo("2354939"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("ГЕНТОС N20 ТАБЛ"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("АВСТРИЯ"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Рихард Биттнер АГ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(184.3900));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(161.0400));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(146.4));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.05.2012"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС AT.ФМ08.Д15648"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("8379159"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.0000));
		}

		[Test]
		public void Parse_Schipakin()
		{
			var files = GetFilesForParsing(@"..\..\Data\Waybills\multifile\h160410.dbf", @"..\..\Data\Waybills\multifile\b160410.dbf");

			var mergedFiles = MultifileDocument.Merge(files);
			Assert.That(mergedFiles.Count, Is.EqualTo(1));
			parser.Parse(mergedFiles[0].FileName, document);
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Оф000000335"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("15/04/2010")));
			Assert.That(document.Lines.Count, Is.EqualTo(18));
			Assert.That(document.Lines[0].Code, Is.EqualTo("429"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Гольфы Артемис 70 Дэн черные"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("КИТАЙ"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Тайвань"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(4));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(50.67));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(68.76));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(68.76));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(0));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].Period, Is.EqualTo("15.10.2010"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС TW.ИМ25.В02103"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("51754406033007352161"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.0000));
		}
	}
}
