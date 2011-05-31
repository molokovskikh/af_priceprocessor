using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BaltimorKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\nakladnaya_baltimor.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00025830"));		
			Assert.That(document.DocumentDate, !Is.Null);
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("04.04.2011"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("35255"));
			Assert.That(line.Product, Is.EqualTo("Циклоферон р-р д/ин. 12,5% амп. 2 мл №5"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.ProducerCost, Is.EqualTo(197.07));
			Assert.That(line.SerialNumber, Is.EqualTo("320710"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ03.Д96935"));
			Assert.That(line.CertificatesDate, Is.EqualTo("25.02.2011"));
			Assert.That(line.Period, Is.EqualTo("01.08.2013"));
			Assert.That(line.Producer, Is.EqualTo("Полисан НТФФ ООО"));			
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(213.40));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
			Assert.That(line.RegistryCost, Is.EqualTo(212.84));
			Assert.That(line.Amount, Is.EqualTo(704.22));			
		}

		[Test]
		public void Check_file_format()
		{					
			Assert.IsTrue(BaltimorKazanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\nakladnaya_baltimor.dbf")));		
		}
	}
}
