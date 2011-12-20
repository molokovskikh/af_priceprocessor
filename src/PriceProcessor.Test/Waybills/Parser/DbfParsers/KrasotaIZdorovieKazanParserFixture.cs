using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KrasotaIZdorovieKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(KrasotaIZdorovieKazanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\011111.DBF")));
			var document = WaybillParser.Parse("011111.DBF");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("00000146"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("01.11.2011"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.ConsigneeInfo, Is.EqualTo("ГУП А-2"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("000000534"));
			Assert.That(line.Product, Is.EqualTo("Тоник для чувствительной кожи 100мл"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(174.00));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Amount, Is.EqualTo(200.54));
			Assert.That(line.NdsAmount, Is.EqualTo(26.54));
		}
	}
}
