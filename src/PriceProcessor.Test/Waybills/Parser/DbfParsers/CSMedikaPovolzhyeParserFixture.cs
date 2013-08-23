using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class CSMedikaPovolzhyeParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("н-023623.dbf");
			var invoice = document.Invoice;
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("15.05.2013")));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("РБн-023623"));
			Assert.That(invoice.BuyerName, Is.EqualTo("<>"));
			Assert.That(invoice.RecipientAddress, Is.Null);
			Assert.That(document.Lines.Count, Is.EqualTo(1));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("18597"));
			Assert.That(line.Product, Is.EqualTo("Тонометр механический СS Medica CS-105 (со встроен"));
			Assert.That(line.Producer, Is.EqualTo("ООО " + '\u0022' + "СиЭс Медика" + '\u0022'));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.SupplierCost, Is.EqualTo(10000));
			Assert.That(line.NdsAmount, Is.EqualTo(0));
			Assert.That(line.Amount, Is.EqualTo(30000));
			Assert.That(line.Certificates, Is.EqualTo("РОСС СN.ME20.B07108"));
			Assert.That(line.CertificatesDate, Is.EqualTo("28.10.2013"));
		}
	}
}
