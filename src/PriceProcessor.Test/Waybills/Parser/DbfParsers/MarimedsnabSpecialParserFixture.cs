using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class MarimedsnabSpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(MarimedsnabSpecialParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\NKL_nnnnnnnn.dbf")));

			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 7949 } }; // код поставщика Маримедснаб (Йошкар-Ола)
			Assert.IsTrue(new WaybillFormatDetector().DetectParser(@"..\..\Data\Waybills\NKL_nnnnnnnn.dbf", documentLog) is MarimedsnabSpecialParser);

			var document = WaybillParser.Parse(@"NKL_nnnnnnnn.dbf", documentLog);

			Assert.That(document.Lines.Count, Is.EqualTo(3));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("91386433/1"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("09.09.2009"));

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("5 ДНЕЙ Средство от пота и запаха ног №10 2г"));
			Assert.That(line.Producer, Is.EqualTo("Санкт-Петербургская"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(17.78));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.NdsAmount, Is.EqualTo(9.60));
			Assert.That(line.Amount, Is.EqualTo(62.94));
			Assert.That(line.Code, Is.EqualTo("14576"));
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("220609^POCC RU.AЯ61."));
			Assert.That(line.CertificatesDate, Is.Null);
			Assert.That(line.Period, Is.EqualTo("01.07.2011"));
			Assert.That(line.OrderId, Is.EqualTo(5513373));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.EAN13, Is.EqualTo("4605059001624"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0.00));
			Assert.That(line.Country, Is.Null);
			Assert.That(line.VitallyImportant, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
		}
	}
}