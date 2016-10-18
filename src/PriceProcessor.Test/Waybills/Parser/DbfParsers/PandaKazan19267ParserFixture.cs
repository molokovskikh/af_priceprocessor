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
	public class PandaKazan19267ParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(PandaKazan19267Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\39.dbf")));
			var document = WaybillParser.Parse("39.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("39"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("17.10.2016")));

			var invoice = document.Invoice;
			Assert.That(invoice.BuyerName, Is.EqualTo("\"Иволга\" (Бирюзовая)"));
			Assert.That(invoice.BuyerId, Is.EqualTo(157));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("892"));
			Assert.That(line.Product, Is.EqualTo("Сеалекс фоpте ПЛЮС капс 0,4г ь4"));
			Assert.That(line.Producer, Is.EqualTo("ООО ВИС"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(451.19));
			Assert.That(line.SupplierCost, Is.EqualTo(532.40));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.ProducerCost, Is.EqualTo(527.13));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.NdsAmount, Is.EqualTo(162.43));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Amount, Is.EqualTo(1064.80));
			Assert.That(line.Period, Is.EqualTo("22.03.2018"));
			Assert.That(line.SerialNumber, Is.EqualTo("040316"));
			Assert.That(line.Certificates, Is.EqualTo("СГР ьRU.77.99.32.003.Е.009173."));
			Assert.That(line.CertificatesDate, Is.EqualTo("24.09.2015"));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(Convert.ToDateTime("24.09.2025")));
			Assert.That(line.CertificateAuthority, Is.EqualTo("Таможенный союз"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
		}
	}
}
