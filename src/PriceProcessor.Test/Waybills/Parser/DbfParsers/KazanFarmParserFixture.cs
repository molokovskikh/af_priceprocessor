using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KazanFarmParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\P-751955.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(26));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-Kz0000751955"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("01.09.2010")));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("771085185"));
			Assert.That(line.Product, Is.EqualTo("Абактал 400мг таб п/об №10"));
			Assert.That(line.Producer, Is.EqualTo("Лек д.д."));
			Assert.That(line.Country, Is.EqualTo("Словения"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			//Assert.That(line.ProducerCost, Is.EqualTo());
			//Assert.That(line.RegistryCost, Is.EqualTo());
			Assert.That(line.SupplierCost, Is.EqualTo(135.89));
			//Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo());
			Assert.That(line.Period, Is.EqualTo("13.06.1"));
			Assert.That(line.VitallyImportant, Is.Null);
			//Assert.That(line.Nds.Value, Is.EqualTo());
			Assert.That(line.Certificates, Is.EqualTo("РОСС SI.ФМ08.Д59556"));
			Assert.That(line.SerialNumber, Is.EqualTo("AC1716"));
			//Assert.That(line.SupplierPriceMarkup, Is.EqualTo());
		}

		[Test]
		public void Check_file_format()
		{
			//Assert.IsTrue(BagautdinovKazanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\P-751955.dbf")));
		}
	}
}
