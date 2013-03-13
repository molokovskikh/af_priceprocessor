using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BelotelovParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("naknadn.dbf");
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Влажная туалетная бумага Mon Rulon 20шт"));
			Assert.That(line.Code, Is.EqualTo("09205"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("Авангард ООО"));
			Assert.That(line.Period, Is.EqualTo("03.02.2013"));
			Assert.That(line.SupplierCost, Is.EqualTo(19.37));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(19.37));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.Unit, Is.EqualTo("шт"));
			Assert.That(line.EAN13, Is.EqualTo("4607091481235"));
			Assert.That(line.OrderId, Is.EqualTo(39676595));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
		}
	}
}
