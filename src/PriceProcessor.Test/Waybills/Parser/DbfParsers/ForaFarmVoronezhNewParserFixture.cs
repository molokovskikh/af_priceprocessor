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
	public class ForaFarmVoronezhNewParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("86778.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(9));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("86778"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("08.06.2011"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("86778"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("07.06.2011"));
			Assert.That(invoice.SellerName, Is.EqualTo("ООО  Фора-Фарм Воронеж"));
			Assert.That(invoice.SellerAddress, Is.EqualTo("394040 г.Воронеж р.п.Придонской ул.Мазлумова д.25 А литер Г, офис Г1"));
			Assert.That(invoice.SellerINN, Is.EqualTo("3661045013"));
			Assert.That(invoice.SellerKPP, Is.EqualTo("366501001"));
			Assert.That(invoice.ShipperInfo, Is.EqualTo("ООО  Фора-Фарм Воронеж   394040 г.Воронеж р.п.Придонской ул.Мазлумова д.25 А литер Г, офис Г1"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("Липецкфармация ОГУП (Зегеля, 30) Липецкая обл. г.Липецк ул.Зегеля д.30А"));
			Assert.That(invoice.PaymentDocumentInfo, Is.Null);
			Assert.That(invoice.BuyerName, Is.EqualTo("ОГУП \"Липецкфармация\""));
			Assert.That(invoice.BuyerAddress, Is.EqualTo("Липецкая обл. г.Липецк ул.Гагарина д.113"));
			Assert.That(invoice.BuyerINN, Is.EqualTo("4826022196"));
			Assert.That(invoice.BuyerKPP, Is.EqualTo("482601001"));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(383.55));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(38.35));
			Assert.That(invoice.Amount10, Is.EqualTo(421.90));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(1219.25));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(219.47));
			Assert.That(invoice.Amount18, Is.EqualTo(1438.72));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(1602.80));
			Assert.That(invoice.NDSAmount, Is.EqualTo(257.82));
			Assert.That(invoice.Amount, Is.EqualTo(1860.62));

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Ангелина прокладки с антибак.слоем меди пакетик №8 артSAPNN1"));
			Assert.That(line.Unit, Is.EqualTo("шт"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.Producer, Is.EqualTo("ТД \"Невис\""));
			Assert.That(line.SupplierCost, Is.EqualTo(20.03));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(0.00));
			Assert.That(line.ExciseTax, Is.EqualTo(0.00));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0.00));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(9.10));
			Assert.That(line.Amount, Is.EqualTo(100.15));
			Assert.That(line.SerialNumber, Is.EqualTo("0111"));
			Assert.That(line.Period, Is.EqualTo("01.01.2013"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АЕ45.В55793"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.CertificatesDate, Is.EqualTo("19.08.2013"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.EqualTo("4607015591071"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(ForaFarmVoronezhNewParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\86778.dbf")));
		}
	}
}