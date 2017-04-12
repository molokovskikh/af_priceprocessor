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
	public class UralBioFarmCheboksaryFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(UralBioFarmCheboksaryParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\134189.DBF")));
			var document = WaybillParser.Parse("134189.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(8));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("134189"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("13.03.2012"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.RecipientAddress, Is.EqualTo("г.Чебоксары, ул.Л.Комсомола, д.34/8, пом.1"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("9840"));
			Assert.That(line.Product, Is.EqualTo("Бипрол 10мг таб.п/о №30"));
			Assert.That(line.Producer, Is.EqualTo("Макиз-Фарма ЗАО,  Россия"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.RegistryCost, Is.EqualTo(111.99));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(111.99));
			Assert.That(line.ProducerCost, Is.EqualTo(123.189));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(104.15));
			Assert.That(line.SupplierCost, Is.EqualTo(114.565));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Amount, Is.EqualTo(229.13));
			Assert.That(line.NdsAmount, Is.EqualTo(20.83));
			Assert.That(line.Period, Is.EqualTo("01.03.2014"));
			Assert.That(line.SerialNumber, Is.EqualTo("020311"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ01.Д07489"));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
			Assert.That(line.EAN13, Is.EqualTo(4607018262930));
			Assert.IsNull(line.BillOfEntryNumber);
			Assert.IsNull(line.CertificatesDate);
		}
	}
}