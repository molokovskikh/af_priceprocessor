using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class ServiceMedParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("3762775_���������_4989_.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("4989"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("29.04.2010")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("17383"));
			Assert.That(line.Product, Is.EqualTo("��������� ����������� �������� 3-1 (�����-�2) 6� (����� � ������������)"));
			Assert.That(line.Producer, Is.EqualTo("����� ���"));
			Assert.That(line.Country, Is.EqualTo("������"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("14.07.2011"));
			Assert.That(line.Certificates, Is.EqualTo("���� RU.��46.�68565"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(165.27));
			Assert.That(line.SupplierCost, Is.EqualTo(181.8));
			Assert.That(line.SerialNumber, Is.EqualTo("6048"));
			Assert.That(line.Nds, Is.EqualTo(10));
		}
	}
}