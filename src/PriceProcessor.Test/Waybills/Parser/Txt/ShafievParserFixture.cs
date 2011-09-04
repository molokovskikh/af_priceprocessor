using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class ShafievParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Shafiev.txt");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("0000008787"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2008, 11, 13)));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00007240"));
			Assert.That(line.Product, Is.EqualTo("������ ������� \"�����\""));
			Assert.That(line.Producer, Is.EqualTo("��� �������� ��� �������"));
			Assert.That(line.Country, Is.EqualTo("�����"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.ProducerCost, Is.EqualTo(142.93));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(144.37));
			Assert.That(line.Nds, Is.EqualTo(10));
		}

		[Test]
		public void Parse_waybill_from_moron()
		{
			var doc = WaybillParser.Parse("09180759.txt");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("9180759"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2010, 07, 27)));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("6797"));
			Assert.That(line.Product, Is.EqualTo("�������� ���. 10��/1�� �10"));
			Assert.That(line.Producer, Is.EqualTo("Biotehnos"));
			Assert.That(line.Country, Is.EqualTo("�������"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCost, Is.EqualTo(1102.59));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(888.7));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.EqualTo("3491009"));
			Assert.That(line.Period, Is.EqualTo("01.09.12"));
			Assert.That(line.Certificates, Is.EqualTo("����.RO.��08.�15964"));
		}

		[Test]
		public void Parse_waybill_with_NdsAmount()
		{
			var doc = WaybillParser.Parse("Oriola_760258_.txt");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("760258"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2011, 9, 2)));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("17468"));
			Assert.That(line.Product, Is.EqualTo("�������� ��-��� 0,1% 10��"));
			Assert.That(line.Producer, Is.EqualTo("�����"));
			Assert.That(line.Country, Is.EqualTo("������"));
			Assert.That(line.Quantity, Is.EqualTo(20));
			Assert.That(line.ProducerCost, Is.EqualTo(3.2));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(3.2));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.EqualTo("180511"));
			Assert.That(line.Period, Is.EqualTo("01.05.14"));
			Assert.That(line.Certificates, Is.EqualTo("����.RU.��01.�42332"));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.NdsAmount, Is.EqualTo(6.40));
			Assert.That(line.Amount, Is.EqualTo(70.40));
			Assert.That(doc.Lines[7].VitallyImportant, Is.True);
			Assert.That(doc.Lines[7].NdsAmount, Is.EqualTo(13.14));
		}
	}
}