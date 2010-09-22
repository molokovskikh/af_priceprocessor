using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class AntKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("�_889.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("�  889"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.09.2010"));
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("���.���\"������� �\"-120��."));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo(null));
			Assert.That(line.Country, Is.EqualTo("������"));
			Assert.That(line.Producer, Is.EqualTo("��� ���"));
			Assert.That(line.Period, Is.EqualTo(null));
			Assert.That(line.SerialNumber, Is.EqualTo(null));
			Assert.That(line.SupplierCost, Is.EqualTo(145));
			Assert.That(line.ProducerCost, Is.EqualTo(145));
		}
	}
}