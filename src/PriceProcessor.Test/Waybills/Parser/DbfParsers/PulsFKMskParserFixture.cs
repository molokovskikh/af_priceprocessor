using System;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class PulsFKMskParserFixture
	{
		/// <summary>
		/// Тест для задачи http://redmine.analit.net/issues/34299
		/// </summary>
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("354329.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("354329"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("22/05/2015")));
			
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("9243"));
			Assert.That(line.Product, Is.EqualTo("Актовегин р-р 40 мг/мл амп. 5 мл х 5"));
			Assert.That(line.Country, Is.EqualTo("Австрия"));
			Assert.That(line.Producer, Is.EqualTo("Nycomed Austria GmbH - Австрия"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.03.2019"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС AT ФМ08 Д33890"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(458.10));
			Assert.That(line.SupplierCost, Is.EqualTo(503.91));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(462.06));
			Assert.That(line.SerialNumber, Is.EqualTo("10968672"));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.EAN13, Is.EqualTo("9003638023831"));
		}
	}
}





