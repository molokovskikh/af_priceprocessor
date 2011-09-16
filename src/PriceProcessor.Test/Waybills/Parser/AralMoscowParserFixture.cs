using System;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class AralMoscowParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Ушакова_О.А.__г.Брянск_пункт__1.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo(Document.GenerateProviderDocumentId()));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("14612"));
			Assert.That(line.Product, Is.EqualTo("Новокаин, 0.5 % 2 мл амп.№10*"));
			Assert.That(line.Producer, Is.EqualTo("Здоровье"));
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.01.2013"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.UA.ФМ01.Д80125"));
			Assert.That(line.SupplierCost, Is.EqualTo(14.1500));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(12.86));
			Assert.That(line.ProducerCost, Is.EqualTo(11.0100));
			Assert.That(line.SerialNumber, Is.EqualTo("141209"));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.RegistryCost, Is.EqualTo(11.0100));		    
		}

		[Test]
		public void Parse_with_nom_and_date()
		{
			var doc = WaybillParser.Parse("5756_2734335.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("2418083"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("09.04.2009"));
			Assert.That(doc.Lines.Count, Is.EqualTo(32));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("14240"));
			Assert.That(line.Product, Is.EqualTo("Аллохол тб.№10"));
			Assert.That(line.Producer, Is.EqualTo("ДальХимФарм ОАО"));
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(20));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.07.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.RU.ФМ08.Д21199"));
			Assert.That(line.CertificatesDate, Is.EqualTo("07.07.2008"));            
			Assert.That(line.SupplierCost, Is.EqualTo(6.4700));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(5.88));
			Assert.That(line.ProducerCost, Is.EqualTo(5.6000));
			Assert.That(line.SerialNumber, Is.EqualTo("920608"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			line = doc.Lines[2];
			Assert.That(line.CertificatesDate, Is.Null);
		}
	}
}
