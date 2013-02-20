using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	class SiaKrasnodarParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3522002.DBF");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-3522002"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("18.02.2013")));

			var l = doc.Lines[0];
			Assert.That(l.Code, Is.EqualTo("76011"));
			Assert.That(l.Product, Is.EqualTo("Аводарт 0.5мг Капс. Х30"));
			Assert.That(l.Producer, Is.EqualTo("Каталент Франс Байнхайм С.А."));
			Assert.That(l.Country, Is.EqualTo("ФРАНЦИЯ"));
			Assert.That(l.ProducerCost, Is.EqualTo(1884.4900));
			Assert.That(l.SupplierCost, Is.EqualTo(1884.49));
			Assert.That(l.Amount, Is.EqualTo(1884.490));
			Assert.That(l.NdsAmount, Is.EqualTo(188.4490));
			Assert.That(l.Quantity, Is.EqualTo(1));
			Assert.That(l.Period, Is.EqualTo("01.04.2016"));
			Assert.That(l.Certificates, Is.EqualTo("РОСС FR.ФМ01.Д25841"));
			Assert.That(l.SerialNumber, Is.EqualTo("062650C"));
			Assert.That(l.BillOfEntryNumber, Is.EqualTo("10130130/011012/0019410/03"));
			Assert.That(l.EAN13, Is.EqualTo("4607008130676"));
			Assert.That(l.Nds, Is.EqualTo(10));
		}
	}
}
