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
	public class Vazakor_144_Fixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(Vazakor_144_Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\00387_1.DBF")));
			var document = WaybillParser.Parse("00387_1.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("0000000387"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.02.2012"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.BuyerName, Is.EqualTo("Акватик ООО"));
			Assert.That(invoice.SellerName, Is.EqualTo("ООО \"ВАЗАКОР\""));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("132"));
			Assert.That(line.Product, Is.EqualTo("Шприц 1 мл инсулиновый HELMJECT U-40/100"));
			Assert.That(line.Unit, Is.EqualTo("шт"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(2.94));
			Assert.That(line.SupplierCost, Is.EqualTo(3.23));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(200));
			Assert.That(line.Amount, Is.EqualTo(646.8));
			Assert.That(line.NdsAmount, Is.EqualTo(58.8));
			Assert.That(line.Producer, Is.EqualTo("HELM Medical GmbH"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.DE.ИМ08.Д00165"));
			Assert.That(line.CertificatesDate, Is.EqualTo("09.12.2014"));
		}
	}
}