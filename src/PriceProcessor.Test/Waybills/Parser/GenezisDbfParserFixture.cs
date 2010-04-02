using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class GenezisDbfParserFixture
	{
		private GenezisDbfParser parser;
		private Document document;

		[SetUp]
		public void Setup()
		{
			parser = new GenezisDbfParser();
			document = new Document();
		}

		[Test]
		public void Parse()
		{
			parser.Parse(@"..\..\Data\Waybills\890579.dbf", document);
			Assert.That(document.ProviderDocumentId, Is.EqualTo("890579"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("23/03/2010")));
			Assert.That(document.Lines.Count, Is.EqualTo(9));
			Assert.That(document.Lines[0].Code, Is.EqualTo("51408"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АЦИПОЛ КАПС 10МЛН.КОЕ N30"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Российская Федерация"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("ЛЕККО ФФ ЗАО"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(120.80));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(141.79));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(128.90000));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.12.2011"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("002794"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("56"));			
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(@"..\..\Data\Waybills\1016416.dbf"));
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(@"..\..\Data\Waybills\1016416_char.DBF"));
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(@"..\..\Data\Waybills\0000470553.dbf"));
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(@"..\..\Data\Waybills\1040150.DBF"));
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(@"..\..\Data\Waybills\8916.dbf"));
			Assert.IsTrue(GenezisDbfParser.CheckFileFormat(@"..\..\Data\Waybills\890579.dbf"));
		}
	}
}
