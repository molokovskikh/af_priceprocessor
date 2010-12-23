using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	class KatrenVrnMusatovParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("203176.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(6));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("203176"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("16881"));
			Assert.That(line.Product, Is.EqualTo("ДИОКСИДИНА 5% 30,0 МАЗЬ"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ01.Д51084"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("Биосинтез ОАО"));
			Assert.That(line.Period, Is.EqualTo("01.08.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("30710"));
			Assert.That(line.SupplierCost, Is.EqualTo(83.38));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(75.8));
			Assert.That(line.ProducerCost, Is.EqualTo(83.38));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
		}
	}
}
