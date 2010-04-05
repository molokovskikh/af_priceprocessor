using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser.Multifile;
using NUnit.Framework;
using System.IO;

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
			parser = new AptekaHoldingParser();
			document = new Document();
		}

		[Test]
		public void Parse()
		{
			var files = new List<string> {
                @"..\..\Data\Waybills\multifile\h271433.dbf",
				@"..\..\Data\Waybills\multifile\b271433.dbf"
			};
			var mergedFiles = MultifileDocument.Merge(files);
			Assert.That(mergedFiles.Count, Is.EqualTo(1));

			parser.Parse(mergedFiles[0], document);
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
			var files = new List<string> {
                @"..\..\Data\Waybills\multifile\h271433.dbf",
				@"..\..\Data\Waybills\multifile\b271433.dbf"
			};
			var mergedFilePath = @"..\..\Data\Waybills\multifile\merged_h271433.dbf";
			if (File.Exists(mergedFilePath))
				File.Delete(mergedFilePath);

			var mergedFiles = MultifileDocument.Merge(files);			
			Assert.That(mergedFiles.Count, Is.EqualTo(1));
				
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(@"..\..\Data\Waybills\1016416.dbf"));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(@"..\..\Data\Waybills\1016416_char.DBF"));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(@"..\..\Data\Waybills\0000470553.dbf"));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(@"..\..\Data\Waybills\1040150.DBF"));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(@"..\..\Data\Waybills\8916.dbf"));
			Assert.IsFalse(AptekaHoldingParser.CheckFileFormat(@"..\..\Data\Waybills\890579.dbf"));
			Assert.IsTrue(AptekaHoldingParser.CheckFileFormat(mergedFiles[0]));

			File.Delete(mergedFiles[0]);
		}
	}
}
