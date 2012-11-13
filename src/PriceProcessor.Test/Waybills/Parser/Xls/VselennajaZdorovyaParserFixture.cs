using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Xls
{
	[TestFixture]
	public class VselennajaZdorovyaParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("1702-001.xls");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1704/001"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("01.10.2012")));
			Assert.That(document.Lines.Count, Is.EqualTo(3));

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Капсулы \"Саймы\" 4 кап"));
			Assert.That(line.Producer, Is.EqualTo("Дядя Вася из гаража напротив"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(475.42));
			Assert.That(line.SupplierCost, Is.EqualTo(561));
			Assert.That(line.Quantity, Is.EqualTo(40));
			Assert.That(line.Amount, Is.EqualTo(22439.82));
			Assert.That(line.Country, Is.EqualTo("Китай"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10216110/090411/0017441       "));
		}
	}
}
