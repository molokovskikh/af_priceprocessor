using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	class ProfitmedMoscowParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("6276_101-10.dbf");
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("19.11.2010"));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("6276/101-10"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("71748"));
			Assert.That(line.Product, Is.EqualTo("Аллохол таб п/о N10"));
			Assert.That(line.Quantity, Is.EqualTo(20));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д09932"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("Фармстандарт-Томскхимфарм"));
			Assert.That(line.Period, Is.EqualTo("01.10.2014"));
			Assert.That(line.SerialNumber, Is.EqualTo("770910"));
			Assert.That(line.SupplierCost, Is.EqualTo(5.83));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(5.3));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(5.07));
			Assert.That(line.RegistryCost, Is.EqualTo(null));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.Nds, Is.EqualTo(10));
		}
	}
}
