using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

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
			Assert.That(invoice.RecipientAddress, Is.EqualTo("ГУП А-2"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("000000534"));
			Assert.That(line.Product, Is.EqualTo("Тоник для чувствительной кожи 100мл"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(174.00));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Amount, Is.EqualTo(200.54));
			Assert.That(line.NdsAmount, Is.EqualTo(26.54));

			var document2 = WaybillParser.Parse("200312.DBF");

			Assert.That(document2.ProviderDocumentId, Is.EqualTo("00000060"));
			Assert.That(document2.DocumentDate.Value.ToShortDateString(), Is.EqualTo("19.03.2012"));

			invoice = document2.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.RecipientAddress, Is.EqualTo("ГУП А-371"));

			line = document2.Lines[0];
			Assert.That(line.Code, Is.EqualTo("000000458"));
			Assert.That(line.Product, Is.EqualTo("Бальзам после загара и тела 150мл"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("ООО \"Фитопром\""));
			Assert.That(line.SerialNumber, Is.EqualTo("Р 52343-2005"));
			Assert.That(line.Period, Is.EqualTo("01.07.2013"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(144.07));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Amount, Is.EqualTo(170));
			Assert.That(line.NdsAmount, Is.EqualTo(25.93m));
		}
	}
}