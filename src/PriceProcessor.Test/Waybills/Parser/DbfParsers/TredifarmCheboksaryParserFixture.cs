using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class TredifarmCheboksaryParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Накладная.dbf");
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("29.11.2010"));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Л0031725"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("256814"));
			Assert.That(line.Product, Is.EqualTo("Рингер, р-р д/инф., , фл.ПЭ 500 мл, , 10"));
			Assert.That(line.Quantity, Is.EqualTo(1000));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ03.Д11268"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Producer, Is.EqualTo("Гематек ООО / Россия"));
			Assert.That(line.Period, Is.EqualTo("01.09.2012"));
			Assert.That(line.SupplierCost, Is.EqualTo(25.89));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(23.54));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(25.40));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
		}
	}
}