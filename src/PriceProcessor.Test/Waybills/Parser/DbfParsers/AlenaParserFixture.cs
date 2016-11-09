using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	class AlenaParserFixture
	{
		[Test]
		public void Parse()
		{
			/*
			 * http://redmine.analit.net/issues/56357
			 */
			var doc = WaybillParser.Parse("Т-000824.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("БУУТ-000824"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2016, 11, 02)));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00016466"));
			Assert.That(line.Product, Is.EqualTo("Нутрилак 0-6 с преб. м/с 350г /уп. 24шт."));
			Assert.That(line.Quantity, Is.EqualTo(12));
			Assert.That(line.SupplierCost, Is.EqualTo(270.89));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(246.26));
			Assert.That(line.Nds, Is.EqualTo(10));
		}
	}
}
