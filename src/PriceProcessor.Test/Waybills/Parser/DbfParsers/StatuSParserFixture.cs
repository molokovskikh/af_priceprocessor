using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	public class StatuSParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("d0000760.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(20));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("с-00000760"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("05.09.2012"));
			var line = doc.Lines[1];
			Assert.That(line.Product, Is.EqualTo("Пластырь для груди \"Две жемчужины\" ЮКАН,1шт"));
			Assert.That(line.Producer, Is.EqualTo("Китай"));
			Assert.That(line.Country, Is.EqualTo("КНР"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(46.2));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.SerialNumber, Is.EqualTo("0104"));
			Assert.That(line.Certificates, Is.EqualTo("POCC CN.AГ.H01442"));
			Assert.That(line.Period, Is.EqualTo("01.04.2014"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10714040/060212/0004040"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("POCCRU.0001.11АГ37"));
			Assert.That(line.CertificatesDate, Is.EqualTo("29.12.2011"));

			var invoice = doc.Invoice;
			Assert.That(invoice.BuyerName, Is.EqualTo("Таттехмедфарм"));
			Assert.That(invoice.BuyerId, Is.EqualTo(0));
			Assert.That(invoice.SellerName, Is.EqualTo("ООО \"статуС\""));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("ул.Чехова,4"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(SmileParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\d0000760.dbf")));
		}
	}
}
