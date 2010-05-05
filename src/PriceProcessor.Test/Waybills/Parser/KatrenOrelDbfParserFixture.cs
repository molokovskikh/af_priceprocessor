using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	public class KatrenOrelDbfParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\83504.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(15));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("83504"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("30.04.2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("31897628"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АМЕЛОТЕКС 0,015 N20 ТАБЛ"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Реплекфарм АО/ФармФирма Сотекс (Россия)"));
			Assert.That(document.Lines[0].Country, Is.Null);
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(250.0000));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(246.7000));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("010110"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д82590"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.09.2011"));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[4].RegistryCost, Is.EqualTo(122.3400));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(271.3700));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(@"..\..\Data\Waybills\1016416.dbf"));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(@"..\..\Data\Waybills\1016416_char.DBF"));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(@"..\..\Data\Waybills\fm21554.dbf"));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(@"..\..\Data\Waybills\1040150.DBF"));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(@"..\..\Data\Waybills\8916.dbf"));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(@"..\..\Data\Waybills\95472.dbf"));
			Assert.IsTrue(KatrenOrelDbfParser.CheckFileFormat(@"..\..\Data\Waybills\83504.dbf"));
		}
	}
}
