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
			Assert.That(line.Product, Is.EqualTo("������������ �-�� 0,05 N200 �����"));
			Assert.That(line.Producer, Is.EqualTo("���������� ���"));
			Assert.That(line.Country, Is.EqualTo("������"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.ProducerCost, Is.EqualTo(9.69));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(11));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(9.69));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.SerialNumber, Is.EqualTo("790310"));
			Assert.That(line.Certificates, Is.EqualTo("���� RU.��05.�87777"));
		}
	}
}