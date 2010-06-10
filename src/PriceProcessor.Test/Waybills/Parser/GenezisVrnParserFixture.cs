using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class GenezisVrnParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("656522.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("656522"));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("19880"));
			Assert.That(line.Product, Is.EqualTo("БАЛЬЗАМ ВАЛЕНТИНА ДИКУЛЯ 75 МЛ"));
			Assert.That(line.Producer, Is.EqualTo("КОРОЛЕВФАРМ ООО"));
			Assert.That(line.Country, Is.EqualTo("Российская Федерация"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Period, Is.EqualTo("01.01.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОССRUПР73В34354"));
			Assert.That(line.SupplierCost, Is.EqualTo(113.73));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(96.38000));
			Assert.That(line.ProducerCost, Is.EqualTo(93.94));
			Assert.That(line.SerialNumber, Is.EqualTo("012010"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(2.6));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.Null);
		}

		[Test]
		public void Parse_SiaVrn()
		{
			var doc = WaybillParser.Parse("Р1926798.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo(Document.GenerateProviderDocumentId()));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("12465"));
			Assert.That(line.Product, Is.EqualTo("Цитрамон П Таб. Х10"));
			Assert.That(line.Producer, Is.Null);
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(100));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.12.2013"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д21762"));
			Assert.That(line.SupplierCost, Is.EqualTo(2.7060));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(2.4600));
			Assert.That(line.ProducerCost, Is.EqualTo(2.2500));
			Assert.That(line.SerialNumber, Is.EqualTo("8921109"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(9.33));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.EqualTo(0));
		}
	}
}
