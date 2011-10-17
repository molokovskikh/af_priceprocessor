using System;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class SiaVrnParserFixture
	{
		[Test]
		public void Parse()
		{
			DateTime dt1 = DateTime.Now;
			var doc = WaybillParser.Parse("�-1873247.DBF");
			DateTime dt2 = DateTime.Now;
			var providerDocId = Document.GenerateProviderDocumentId();
			providerDocId = providerDocId.Remove(providerDocId.Length - 1);

			Assert.IsFalse(doc.ProviderDocumentId.StartsWith(providerDocId));
			Assert.That(doc.DocumentDate, Is.GreaterThanOrEqualTo(dt1));
			Assert.That(doc.DocumentDate, Is.LessThanOrEqualTo(dt2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("�-1873247"));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("2592"));
			Assert.That(line.Product, Is.EqualTo("���������� 140���/���� ����� �����. �����. 10�� ��. � �"));
			Assert.That(line.Producer, Is.EqualTo("MEDA Pharma GmbH & Co. KG"));
			Assert.That(line.Country, Is.EqualTo("��������"));
			Assert.That(line.SerialNumber, Is.EqualTo("8M075A"));
			Assert.That(line.Period, Is.EqualTo("31.10.2011"));
			Assert.That(line.SupplierCost, Is.EqualTo(183.37));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo("���� DE.��08.�51369"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-14.91));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(166.7));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(195.9));
			Assert.That(line.VitallyImportant, Is.False);
		}
	}
}