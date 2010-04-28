using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser;
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
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(328.10));
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
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(@"..\..\Data\Waybills\1016416.dbf"));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(@"..\..\Data\Waybills\1016416_char.DBF"));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(@"..\..\Data\Waybills\0000470553.dbf"));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(@"..\..\Data\Waybills\1040150.DBF"));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(@"..\..\Data\Waybills\8916.dbf"));
			Assert.IsFalse(AptekaHoldingSingleParser2.CheckFileFormat(@"..\..\Data\Waybills\R1362131.DBF"));
			Assert.IsTrue(AptekaHoldingSingleParser2.CheckFileFormat(@"..\..\Data\Waybills\АХ986336.DBF"));
		}
	}
}
