using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BikovCheboksary12649ParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(BikovCheboksary12649Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\Б0001213.DBF")));
			var document = WaybillParser.Parse("Б0001213.DBF");

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("Б0001213"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("24.04.2012"));
			Assert.That(invoice.BuyerName, Is.EqualTo("ИП Шахвердиева Валентина Александровна"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("3244"));
			Assert.That(line.Product, Is.EqualTo("911 Непотин гель д/ног 100 мл антиперспирант"));
			Assert.That(line.ProducerCost, Is.EqualTo(45.35));
			Assert.That(line.SupplierCost, Is.EqualTo(49.88));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.NdsAmount, Is.EqualTo(0));
			Assert.That(line.Producer, Is.EqualTo("Твинс Тэк Россия"));
			Assert.That(line.Period, Is.EqualTo("01.08.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("0212"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АИ86.Д00149"));
			Assert.That(line.CertificatesDate, Is.EqualTo("16.04.2011"));
			Assert.IsNull(line.BillOfEntryNumber);
		}
	}
}
