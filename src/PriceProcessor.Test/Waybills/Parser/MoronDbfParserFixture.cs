using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class MoronDbfParserFixture
	{
		[Test]
		public void Parse()
		{
			var parser = new MoronDbfParser();
			var doc = new Document();
			var document = parser.Parse(@"..\..\Data\Waybills\0000470553.dbf", doc);
			Assert.That(document.Lines.Count, Is.EqualTo(72));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("470553.00"));

			Assert.That(document.Lines[5].Code, Is.EqualTo("26505.00"));
			Assert.That(document.Lines[5].Producer, Is.EqualTo("Дина+"));
			Assert.That(document.Lines[5].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[5].Product, Is.EqualTo("Барсучок бальзам д/детей разогрев 30мл"));
			Assert.That(document.Lines[5].SupplierCost, Is.EqualTo(43.07));
			Assert.That(document.Lines[5].SupplierCostWithoutNDS, Is.EqualTo(36.50));
			Assert.That(document.Lines[5].Period, Is.EqualTo("01/11/2011"));
			Assert.That(document.Lines[5].VitallyImportant, Is.False);
			Assert.That(document.Lines[5].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[5].Certificates, Is.EqualTo("РОСС.RU.АИ11.В00697"));
			Assert.That(document.Lines[5].SerialNumber, Is.EqualTo("1109"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("26/03/2010")));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(@"..\..\Data\Waybills\1016416.dbf"));
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(@"..\..\Data\Waybills\1016416_char.DBF"));
			Assert.IsTrue(MoronDbfParser.CheckFileFormat(@"..\..\Data\Waybills\0000470553.dbf"));
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(@"..\..\Data\Waybills\1040150.DBF"));
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(@"..\..\Data\Waybills\8916.dbf"));
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(@"..\..\Data\Waybills\890579.dbf"));
		}
	}
}
