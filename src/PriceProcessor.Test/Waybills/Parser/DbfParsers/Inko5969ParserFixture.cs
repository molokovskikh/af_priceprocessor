using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class Inko5969ParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(Inko5969Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\00000064.dbf")));
			var document = WaybillParser.Parse("00000064.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(7));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00000064"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.01.2012"));

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo(@"До и После, ""От пигментных пятен"" крем, 50 мл"));
			Assert.That(line.SerialNumber, Is.EqualTo("09.2011"));
			Assert.That(line.Period, Is.EqualTo("30.03.2013"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АИ 86.Д00384 от 15.07.2010"));
			Assert.That(line.Producer, Is.EqualTo(@"ЗАО ""Твинс Тэк""РОССИЯ"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(1u));
			Assert.That(line.Nds, Is.EqualTo(18u));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(67.94));
			Assert.That(line.NdsAmount, Is.EqualTo(12.23));
			Assert.That(line.Amount, Is.EqualTo(80.17));
			Assert.That(line.SupplierCost, Is.EqualTo(80.17));
		}
	}
}