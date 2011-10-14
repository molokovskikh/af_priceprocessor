using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
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
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(17.67000));
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
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(11.59));
		}

		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3795418_Ижевск-Фарм(98736).dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-Иж00000098736"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("11/05/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("211561256"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Аскорбиновая кислота 50мг др №200"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Марбиофарм ОАО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("РОССИЯ_"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(10));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(9.69000));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(10.40909));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("580210"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ05.Д67099"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.03.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(9.69000));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(11.45));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(7.42));
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(IzhevskFarmParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsTrue(IzhevskFarmParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\95472.dbf")));
		}
	}
}
