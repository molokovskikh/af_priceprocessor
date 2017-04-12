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
	public class Medkom_Mp_Spb_Fixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(Medkom_Mp_Spb.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\6792.dbf")));
			var document = WaybillParser.Parse("6792.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(28));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("СКЛ-006792"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("30.01.2012"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("СКЛ-006792"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("30.01.2012"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("ООО \"УК Здоровые Люди\",196105, Санкт-Петербург г, Московский пр-кт,  дом 143 лит.А 12Н"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("ММ-1016160"));
			Assert.That(line.Product, Is.EqualTo("DUREX (lubr)  Play O 15ml"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(423.73));
			Assert.That(line.SupplierCost, Is.EqualTo(500));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.EAN13, Is.EqualTo(0));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Amount, Is.EqualTo(1000));
			Assert.That(line.NdsAmount, Is.EqualTo(152.54));
			Assert.That(line.Producer, Is.Null);
			Assert.That(line.Country, Is.EqualTo("ВЕЛИКОБРИТАНИЯ"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130070/210611/0013791/1"));
			Assert.That(line.Period, Is.EqualTo("31.01.2015"));
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.Certificates, Is.EqualTo("РОСС GB.АЯ46.Д39193"));
		}
	}
}