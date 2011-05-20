using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class RostaChelyabinskParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("35260_13.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(8));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("35260/13"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("25.04.2011"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("680009432"));
			Assert.That(line.Product, Is.EqualTo("Гевискон таб жев мятные 250мг х 16"));
			Assert.That(line.Producer, Is.EqualTo("Reckitt Benckiser"));
			Assert.That(line.Country, Is.EqualTo("Великобритания"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.ProducerCost, Is.EqualTo(0.00));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(55.11));
			Assert.That(line.SupplierCost, Is.EqualTo(60.62));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));
			Assert.That(line.NdsAmount, Is.EqualTo(27.56));
			Assert.That(line.Amount, Is.EqualTo(303.11));
			Assert.That(line.SerialNumber, Is.EqualTo("018207"));
			Assert.That(line.Period, Is.EqualTo("01.06.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС GB ФМ08 Д85834"));

			line = doc.Lines[1];
			Assert.That(line.Code, Is.EqualTo("52407"));
			Assert.That(line.Product, Is.EqualTo("Изоптин SR 240 таб. п/об. 240 мг х 30"));
			Assert.That(line.Producer, Is.EqualTo("Abbott GmbH & Co.KG - Германия"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(296.28));
			Assert.That(line.ProducerCost, Is.EqualTo(296.28));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(299.89));
			Assert.That(line.SupplierCost, Is.EqualTo(329.88));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(1.22));
			Assert.That(line.NdsAmount, Is.EqualTo(59.98));
			Assert.That(line.Amount, Is.EqualTo(659.76));
			Assert.That(line.SerialNumber, Is.EqualTo("957078D"));
			Assert.That(line.Period, Is.EqualTo("31.07.2013"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE ФМ11 Д67795"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(RostaChelyabinskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\35260_13.dbf")));
		}
	}
}
