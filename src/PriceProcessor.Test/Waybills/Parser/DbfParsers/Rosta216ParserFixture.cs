using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Rosta216ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("11923621.dbf");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("11923621"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("12.11.2012")));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("192"));
			Assert.That(line.Product, Is.EqualTo("АМБРОБЕНЕ ТБ 0,03 №20"));
			Assert.That(line.SerialNumber, Is.EqualTo("M16353"));
			Assert.That(line.Quantity, Is.EqualTo(20));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(87.19));
			Assert.That(line.NdsAmount, Is.EqualTo(174.38));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE ФМ08 Д98813 Д"));
			Assert.That(line.Period, Is.EqualTo("01.03.2017"));
			Assert.That(line.Country, Is.EqualTo("ГЕРМАНИЯ"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(86.24));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(1.1));
			Assert.That(line.Producer, Is.EqualTo("Merckle - Германия"));
			Assert.That(line.SupplierCost, Is.EqualTo(95.91));
			Assert.That(line.Amount, Is.EqualTo(1918.18));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130032/200812/0005326/1"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.CountryCode, Is.EqualTo("276"));
		}
	}
}
