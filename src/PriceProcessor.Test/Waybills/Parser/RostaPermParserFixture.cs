using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class RostaPermParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\78349-14.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(2));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("78349/14"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("27/04/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("680004057"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Арбидол капс 100мг х 10"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Фармстандарт-Лексредства - Россия"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(10));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(138));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(131.08));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("400110"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU ФМ05 Д51168"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.02.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.Null);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(144.19));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(-5.01));
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(RostaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(RostaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(RostaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(RostaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(RostaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(RostaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\R1362131.DBF")));
			Assert.IsTrue(RostaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\78349-14.dbf")));
		}
	}
}