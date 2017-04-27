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
	public class BioMaster2394ParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(BioMaster2394Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\r_03-068.DBF")));
			var document = WaybillParser.Parse("r_03-068.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(2));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Г-1603-068"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("16.03.2012"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("Г-1603-068"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("16.03.2012"));
			Assert.That(invoice.Amount, Is.EqualTo(156229.9));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00437"));
			Assert.That(line.Product, Is.EqualTo("Веро-метотрексат р-р д/ин 10мг/мл 5мл №10"));
			Assert.That(line.Producer, Is.EqualTo("Лэнс-Фарм"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.RegistryCost, Is.EqualTo(661.64));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(650.69));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(707.95));
			Assert.That(line.SupplierCost, Is.EqualTo(778.75));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Amount, Is.EqualTo(778.75));
			Assert.That(line.NdsAmount, Is.EqualTo(70.8));
			Assert.That(line.Period, Is.EqualTo("01.08.2014"));
			Assert.That(line.SerialNumber, Is.EqualTo("100711"));
			Assert.That(line.CertificatesDate, Is.EqualTo("10.08.2011"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ01.Д87884"));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
			Assert.That(line.EAN13, Is.EqualTo(4605095002678));
			Assert.IsNull(line.BillOfEntryNumber);
		}
	}
}