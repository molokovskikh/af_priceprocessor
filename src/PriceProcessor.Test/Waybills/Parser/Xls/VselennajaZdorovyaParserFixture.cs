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
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1702/001"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("01.10.2012")));
			Assert.That(document.Lines.Count, Is.EqualTo(4));

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Капсулы \"Саймы\" 4 кап"));
			Assert.That(String.IsNullOrEmpty(line.Producer));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SupplierCost, Is.EqualTo(389.845));
			Assert.That(line.Amount, Is.EqualTo(1121.99));
		}
	}
}
