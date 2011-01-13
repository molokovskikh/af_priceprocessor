using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KatrenVrnMusatovParserFixture
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
			Assert.That(line.ProducerCost, Is.EqualTo(75.80));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			var line1 = doc.Lines[1];
			Assert.That(line1.ProducerCost, Is.EqualTo(116.83));
			Assert.That(line1.SupplierCostWithoutNDS, Is.EqualTo(108.7));
			Assert.That(line1.Nds, Is.EqualTo(10));
		}

		[Test]
		public void Parse_produser_cost_without_nds()
		{
			var doc = WaybillParser.Parse("6098189_Катрен_1209_.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(95));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("6098189_Катрен_1209_"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("6439408"));
			Assert.That(line.Product, Is.EqualTo("ТЕРМОМЕТР ЭЛЕКТРОННЫЙ AMDT-10 (УДАРОСТОЙК КОРПУС)"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo("РОСС US.ИМ04.В06948"));
			Assert.That(line.Country, Is.EqualTo("США"));
			Assert.That(line.Producer, Is.EqualTo("Амрус Энтерпрайзис ЛТД"));
			Assert.That(line.Period, Is.EqualTo("01.11.2015"));
			Assert.That(line.SerialNumber, Is.EqualTo("112010"));
			Assert.That(line.SupplierCost, Is.EqualTo(71.51));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(71.51));
			Assert.That(line.ProducerCost, Is.EqualTo(71.51));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			var line1 = doc.Lines[1];
			Assert.That(line1.ProducerCost, Is.EqualTo(33.90));
			Assert.That(line1.SupplierCostWithoutNDS, Is.EqualTo(33.90));
			Assert.That(line1.Nds, Is.EqualTo(10));
		}
	}
}
