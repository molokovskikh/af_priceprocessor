using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KosmetikOptParserFixture
	{
		[Test]
		public void Parser()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\real_№УТKO0000566_from_16.03.2011.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(17));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("УТKO0000566"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("16.03.2011"));
			
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("УТ000000208"));
			Assert.That(line.Product, Is.EqualTo("Сплат Professional УЛЬТРАКОМПЛЕКС, 100мл, зубная паста (арт.У-115)"));
			Assert.That(line.Producer, Is.EqualTo("ООО \"СПЛАТ-КОСМЕТИКА\""));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.SupplierCost, Is.EqualTo(61.55));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(52.16));
			Assert.That(line.Amount, Is.EqualTo(123.1));
			Assert.That(line.Period, Is.Null);
			Assert.That(line.Country, Is.Null);
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.Null);
			Assert.That(line.ProducerCost, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.SupplierPriceMarkup, Is.Null);
			Assert.That(line.NdsAmount, Is.EqualTo(18.78));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(KosmetikOptParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\real_№УТKO0000566_from_16.03.2011.dbf")));
		}
	}
}
