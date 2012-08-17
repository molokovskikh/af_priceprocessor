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
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(145));
		}

		[Test]
		public void KazmedServiceParse()
		{
			var doc = WaybillParser.Parse("001150.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("��00001150"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("27.06.2011"));
			Assert.That(doc.Lines.Count, Is.EqualTo(10));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("���12107"));
			Assert.That(line.Product, Is.EqualTo("���� ������������ ������������� �������� 5�10  ���"));
			Assert.That(line.Unit, Is.EqualTo("��"));
			Assert.That(line.Quantity, Is.EqualTo(30));
			Assert.That(line.SupplierCost, Is.EqualTo(5.20));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.NdsAmount, Is.EqualTo(0.4727));
			Assert.That(line.Amount, Is.EqualTo(156.0000));
			Assert.That(line.Certificates, Is.EqualTo("���� RU.��56.�43157"));
			Assert.That(line.Country, Is.EqualTo("������"));
			Assert.That(line.Producer, Is.EqualTo("��� ���\"�������\""));
			Assert.That(line.Period, Is.EqualTo(null));
			Assert.That(line.SerialNumber, Is.EqualTo(null));
		}
	}
}