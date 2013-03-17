using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BTKParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("BI13915.DBF");
			Assert.That(doc.ProviderDocumentId.Trim(), Is.EqualTo("13915"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("14.03.2013"));
			Assert.That(doc.Invoice.RecipientId, Is.EqualTo(77008));
			Assert.That(doc.Invoice.RecipientAddress, Is.EqualTo("Аптека Биос Калмыкия. г.Элиста"));
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Х.Х.подг.SOFT/DRY JUNIOR 10ш.839/15"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.NdsAmount, Is.EqualTo(17.65));
			Assert.That(line.Country, Is.EqualTo("Чехия"));
			Assert.That(line.Amount, Is.EqualTo(194.12));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.EqualTo(null));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(88.24));
			Assert.That(line.SupplierCost, Is.EqualTo(97.06));
			Assert.That(line.BillOfEntryNumber.Trim(), Is.EqualTo("10130060/300512/0012565/1"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BE АЮ18 В17230 №0072158"));
			Assert.That(line.CertificatesDate, Is.EqualTo("29.01.2014"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО \"Онтекс РУ\""));
		}
	}
}
