using System;
using System.Globalization;
using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class FarmGroupParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00013602.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00013602"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("17.05.10")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("�00027133"));
			Assert.That(line.Product, Is.EqualTo("��������� (��-� ���. �/� 0,02� �50)"));
			Assert.That(line.Producer, Is.EqualTo("����, ���"));
			Assert.That(line.SerialNumber, Is.EqualTo("161009"));
			Assert.That(line.Period, Is.EqualTo("01.11.2012"));
			Assert.That(line.SupplierCost, Is.EqualTo(6.96));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("���� RU.��05.�08755"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(3.9d));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Country, Is.EqualTo("������"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(6.3273));
			Assert.That(line.ProducerCost, Is.EqualTo(6.09));
		}

		[Test]
		public void Parse_Avesta_Farmatsevtika()
		{
			var doc = WaybillParser.Parse("106836_10.dbf");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("106836"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("29.06.2010")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("43423"));
			Assert.That(line.Product, Is.EqualTo("������ ��� �/� 100�� N1"));
			Assert.That(line.Producer, Is.EqualTo("Pfizer"));
			Assert.That(line.SerialNumber, Is.EqualTo("8312804"));
			Assert.That(line.Period, Is.EqualTo("01.12.2013"));
			Assert.That(line.SupplierCost, Is.EqualTo(472.6600));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("���� FR.��08.�94373"));
			Assert.That(line.SupplierPriceMarkup, Is.Null);
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Country, Is.EqualTo("�������"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(429.6900));
			Assert.That(line.ProducerCost, Is.EqualTo(390.6300));
		}

		[Test]
		public void Parse_PulsBryansk()
		{
			var doc = WaybillParser.Parse("7997577_PulsBryansk(N_11121_11126_00031791).dbf");
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00015"));
			Assert.That(line.Product, Is.EqualTo("�������� /������-��������/ ����. �/������. �24"));
			Assert.That(line.Producer, Is.EqualTo("AGIO"));
			Assert.That(line.SerialNumber, Is.EqualTo("10/14/0022"));
			Assert.That(line.Period, Is.EqualTo("01.09.2013"));
			Assert.That(line.SupplierCost, Is.EqualTo(25.76));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Certificates, Is.EqualTo("���� IN.�M08.�58735"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Country, Is.EqualTo("�����"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(23.42));
			Assert.That(line.ProducerCost, Is.EqualTo(23.42));
		}
	}
}