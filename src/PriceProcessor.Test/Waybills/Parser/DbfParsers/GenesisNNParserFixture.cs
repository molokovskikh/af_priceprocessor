using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class GenesisNNParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("GenesisNN.dbf");

			Assert.That(doc.Lines.Count, Is.EqualTo(10));
			var line = doc.Lines[1];
			Assert.That(line.Code, Is.EqualTo("5576697"));
			Assert.That(line.Product, Is.EqualTo("АСКОРБИНОВАЯ К-ТА 0,05 N200 ДРАЖЕ"));
			Assert.That(line.Producer, Is.EqualTo("МАРБИОФАРМ ОАО"));
			Assert.That(line.Country, Is.EqualTo("россия"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.ProducerCost, Is.EqualTo(9.69));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(11));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(9.69));
			Assert.That(line.Period, Is.EqualTo("01.04.2012"));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.SerialNumber, Is.EqualTo("790310"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д87777"));
			Assert.That(line.SupplierCost, Is.EqualTo(12.1));
		}

		[Test]
		public void Parse_RostaKazan_Infanta()
		{
			var doc = WaybillParser.Parse("169976_21.dbf");

			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("169976_21"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("24.12.2010"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("680005347"));
			Assert.That(line.Product, Is.EqualTo("Альфинал таб п/о 5мг х 30"));
			Assert.That(line.Producer, Is.EqualTo("Валента Фармацевтика ОАО - Россия"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCost, Is.EqualTo(233.59));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(227.79));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(313.58));
			Assert.That(line.Period, Is.EqualTo("01.12.2012"));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.SerialNumber, Is.EqualTo("20510"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU ФМ01 Д38736"));
			Assert.That(line.SupplierCost, Is.EqualTo(250.57));
		}

		[Test]
		public void Parse_AptekaHolding_Lipetsk()
		{
			var doc = WaybillParser.Parse("55154_2.dbf");

			Assert.That(doc.Lines.Count, Is.EqualTo(6));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00000455154/"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("08.04.2011"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("7976"));
			Assert.That(line.Product, Is.EqualTo("Бинт эласт медиц ВР  5м х 8см Латвия"));
			Assert.That(line.Producer, Is.EqualTo("Tonus Elast ООО"));
			Assert.That(line.Country, Is.EqualTo("Латвия"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.ProducerCost, Is.EqualTo(50.52));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(50.52));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.Period, Is.EqualTo("01.09.2015"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.SerialNumber, Is.EqualTo("-"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС LV.ИМ25.В02516 от 13.08.09 до 13.08.12 выдан ОС \"Энергия Плюс\" г."));
			Assert.That(line.NdsAmount, Is.EqualTo(10.10));
			Assert.That(line.Amount, Is.EqualTo(111.14));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));

			line = doc.Lines[1];
			Assert.That(line.NdsAmount, Is.EqualTo(11.53));
			Assert.That(line.Amount, Is.EqualTo(126.78));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.45));

		}


	}
}