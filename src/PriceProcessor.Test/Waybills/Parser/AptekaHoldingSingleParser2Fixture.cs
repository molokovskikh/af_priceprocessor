using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class AptekaHoldingSingleParser2Fixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\АХ986336.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(4));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-0986336"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("24/12/2009")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("16056"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Адельфан-эзидрекс табл. N250 Индия (не ЖВ)"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Sandoz Private Ltd"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Индия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(328.10));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(332.10));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("AE39L8D"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС IN.ФМ08.Д05084"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.11.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[3].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00000));
			Assert.That(document.Lines[3].RegistryCost, Is.EqualTo(370));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(365.31));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(1.22));
		}

		
		[Test]
		public void Parse_with_asterist_in_registry_cost()
		{
			var doc = WaybillParser.Parse("652077.dbf");
			var line = doc.Lines[0];
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.Product, Is.EqualTo("ДОКТОР МОМ ПАСТИЛ. ОТ КАШЛЯ N20 КЛУБНИКА"));
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\R1362131.DBF")));
			Assert.IsTrue(AptekaHoldingSingleParser2.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\АХ986336.DBF")));
		}
	}
}
