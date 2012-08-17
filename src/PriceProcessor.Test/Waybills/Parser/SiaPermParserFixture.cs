using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class SiaPermParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\R1362131.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(24));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-1362131"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("21/04/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("1780451,0000"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Апилак 10мг Таб. сублингв. Х10"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Вифитех ЗАО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(document.Lines[1].ProducerCostWithoutNDS, Is.EqualTo(12.0900));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(11.8500));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("091109"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ08.Д09294"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.12.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[1].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00000));
			Assert.That(document.Lines[1].RegistryCost, Is.EqualTo(12.0900));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(13.0400));
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
		public void CheckFileFormat()
		{
			Assert.IsFalse(SiaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(SiaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(SiaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(SiaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(SiaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsTrue(SiaPermParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\R1362131.DBF")));
		}
	}
}