using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class IzhevskFarmParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\95472.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(12));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-Иж00000095472"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("19/04/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("213935823"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Аира корневища пачка 50г №1"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Фирма Здоровье ЗАО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("РОССИЯ_"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(17.67000));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(19.71818));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("020609"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ08.Д34002"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.07.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[4].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00000));
			Assert.That(document.Lines[4].RegistryCost, Is.EqualTo(10.17000));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(21.69));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(@"..\..\Data\Waybills\1016416.dbf"));
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(@"..\..\Data\Waybills\1016416_char.DBF"));
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(@"..\..\Data\Waybills\0000470553.dbf"));
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(@"..\..\Data\Waybills\1040150.DBF"));
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(@"..\..\Data\Waybills\8916.dbf"));
			Assert.IsTrue(IzhevskFarmParser.CheckFileFormat(@"..\..\Data\Waybills\95472.dbf"));
		}
	}
}
