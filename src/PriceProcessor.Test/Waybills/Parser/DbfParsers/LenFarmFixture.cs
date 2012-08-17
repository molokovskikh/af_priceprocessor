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
	public class LenFarmFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(LenFarmParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\00012685.dbf")));
			var document = WaybillParser.Parse("00012685.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(3));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("М000012685"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("14.02.2012"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.BuyerName, Is.EqualTo("ЦРА №23 ОАО1 этаж"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("6342"));
			Assert.That(line.Product, Is.EqualTo("Альбадент (грейпфрут) освеж/полости рта 10 мл"));

			Assert.That(line.Producer, Is.EqualTo("Фирма Вита ООО"));
			Assert.IsNull(line.Country);

			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(22.46));
			Assert.That(line.SupplierCost, Is.EqualTo(26.5));
			Assert.That(line.RegistryCost, Is.EqualTo(0));

			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Amount, Is.EqualTo(53));

			Assert.That(line.Period, Is.EqualTo("01.06.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("0878/011011"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АИ35.Д14019"));
			Assert.That(line.CertificatesDate, Is.EqualTo("01.12.2013"));
			Assert.IsNull(line.BillOfEntryNumber);
		}
	}
}