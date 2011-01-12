using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class AptekaHoldingSingleParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\А0973748.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(34));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-0973748"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("09/12/2009")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("27618"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Sanosan крем защитный от опрелостей с оливк. масл. и молочн. протеином"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Mann&Schroder GmbH"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Германия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(83.92));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(85.60));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("-"));
			Assert.That(document.Lines[1].SerialNumber, Is.EqualTo("AE39L8D"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС.DE.ПК08.В01732 д"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.10.2011"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[3].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00000));
			Assert.That(document.Lines[3].RegistryCost, Is.EqualTo(238.69));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(101.01));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\R1363495.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(40));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-1363495"));
		}

		[Test]
		public void Parse3()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\1281_1087568.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(8));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-1087568"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("29/04/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("3"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("L-тироксин табл. 100мкг N100  (ЖВЛС)"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Berlin-Chemie AG/Menarini Group"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Германия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(107.12));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(104.90));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("93052"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС.DE.ФМ01.Д27143 д"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.08.2011"));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(107.75));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(115.39));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\R1362131.DBF")));
			Assert.IsTrue(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\А0973748.DBF")));
		}

		[Test]
		public void Parse4()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\21796_1272844.dbf");
		}
	}
}
