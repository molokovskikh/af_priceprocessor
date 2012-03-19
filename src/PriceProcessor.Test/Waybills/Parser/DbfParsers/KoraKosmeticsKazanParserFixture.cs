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
	public class KoraKosmeticsKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(KrasotaIZdorovieKazanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\160312.DBF")));
			var document = WaybillParser.Parse("160312.DBF");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("00000057"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("16.03.2012"));
			
			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.ConsigneeInfo, Is.EqualTo("ГУП А-370"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("000000531"));
			Assert.That(line.Product, Is.EqualTo("Тоник для жирной и комбинированной кожи 100мл"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(116.1));
			Assert.That(line.Amount, Is.EqualTo(137));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.NdsAmount, Is.EqualTo(20.9));
		}
	}
}
