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
	public class UralBioFarmKurskParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(UralBioFarmKurskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\152307.DBF")));
			var document = WaybillParser.Parse("152307.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(3));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("152307"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("25.11.2011"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("152307"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("25.11.2011"));
			Assert.That(invoice.BuyerName, Is.EqualTo("ИП Нескородова Л.А."));
			Assert.That(invoice.BuyerAddress, Is.EqualTo("305025, г.Курск  Магистральный пр. 16 б"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("2029"));
			Assert.That(line.Product, Is.EqualTo("Асептолин р-р д/наруж. прим. 90% 100мл"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(15.32));
			Assert.That(line.ProducerCost, Is.EqualTo(16.85));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(15.47));
			Assert.That(line.SupplierCost, Is.EqualTo(17.02));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(210));
			Assert.That(line.Amount, Is.EqualTo(3573.57));
			Assert.That(line.NdsAmount, Is.EqualTo(324.87));
			Assert.That(line.Producer, Is.EqualTo("Фармацевтический комбинат (Россия)"));
			Assert.That(line.Period, Is.EqualTo("01.10.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("071011"));
			Assert.That(line.Certificates, Is.EqualTo("POCCRU.ФМ01.Д38608, 01.11.11, ФГБУ\"ЦЭККМП\"Росздравнадзора, 01.10.13"));
			Assert.IsNull(line.BillOfEntryNumber);
		}
	}
}