using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SiaInternationalKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Р-1179448.dbf");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-1179448"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("26.09.2012")));
			var invoice = doc.Invoice;
			Assert.That(invoice.BuyerName, Is.EqualTo("апт1 НЧ Электротехников 32/01"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("Наб-челны, ООО \"Здоровые Люди Набережные Челны\", 423822, РТ, г. Набережные Челны, мкр-н 32,"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("10495"));
			Assert.That(line.Product, Is.EqualTo("Амоксициллин 500мг Капс. Х16 (R)"));
			Assert.That(line.Producer, Is.EqualTo("Хемофарм А.Д."));
			Assert.That(line.Country, Is.EqualTo("СЕРБИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(4));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.05.2015"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RS.ФМ09.Д40426 ()"));
			Assert.That(line.CertificatesDate, Is.Null);
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(72.55));
			Assert.That(line.SupplierCost, Is.EqualTo(79.81));
			Assert.That(line.SerialNumber, Is.EqualTo("I200578"));
			Assert.That(line.RegistryCost, Is.EqualTo(73.65));
			Assert.That(line.NdsAmount, Is.EqualTo(29.02));

			Assert.That(line.EAN13, Is.EqualTo("3004101009"));
			Assert.That(line.Amount, Is.EqualTo(319.22));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10106050/270612/0011151/2"));
		}
	}
}
