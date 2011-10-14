using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
    [TestFixture]
    public class RostaVoronezhParserFixture
    {
        [Test]
        public void Parse()
        {
            var doc = WaybillParser.Parse(@"10410382.txt");
            Assert.That(doc.Lines.Count, Is.EqualTo(23));
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("10410382"));
            Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("01.07.2011")));

            Assert.That(doc.Lines[0].Code, Is.Null);
            Assert.That(doc.Lines[0].Product, Is.EqualTo("АЗАФЕН ТБ 0,025 №50"));
            Assert.That(doc.Lines[0].Unit, Is.EqualTo("УП."));
            Assert.That(doc.Lines[0].Quantity, Is.EqualTo(30));
            Assert.That(doc.Lines[0].Producer, Is.EqualTo("Макиз Фарма - Россия"));
            Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(140.50));
            Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(127.73));
            Assert.That(doc.Lines[0].ExciseTax, Is.Null);
            Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0.00));
            Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(137.87));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(137.87));
            Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
            Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(383.19));
            Assert.That(doc.Lines[0].Amount, Is.EqualTo(4215.09));
            Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("010211"));
            Assert.That(doc.Lines[0].Period, Is.EqualTo("01.02.16"));
            Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU ФМ01 Д93409"));
            Assert.That(doc.Lines[0].Country, Is.EqualTo("РОССИЯ"));
            Assert.That(doc.Lines[0].BillOfEntryNumber, Is.EqualTo("---"));
            Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("01.02.16"));
            Assert.That(doc.Lines[0].VitallyImportant, Is.True);
            Assert.That(doc.Lines[0].EAN13, Is.EqualTo("4607018261599 "));

            var invoice = doc.Invoice;
            Assert.That(invoice, Is.Not.Null);
            Assert.That(invoice.InvoiceNumber, Is.EqualTo("10410382"));
            Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("01.07.2011"));
            Assert.That(invoice.SellerName, Is.EqualTo("Закрытое акционерное общество 'РОСТА' (ЗАО 'РОСТА')"));
            Assert.That(invoice.SellerAddress, Is.EqualTo("142100, Московская обл., г.Подольск, пр.Ленина, д.1"));
            Assert.That(invoice.SellerINN, Is.EqualTo("7726320638"));
            Assert.That(invoice.SellerKPP, Is.EqualTo("366243001"));
            Assert.That(invoice.ShipperInfo, Is.EqualTo("ЗАО 'РОСТА' Воронежский филиал, 394016,г.Воронеж, ул.45-й Стрелковой дивизии,224,4эт"));
            Assert.That(invoice.ConsigneeInfo, Is.EqualTo("ОГУП \"Липецкфармация\" , Липецк,ул.Константиновой,д.1"));
            
            Assert.That(invoice.BuyerName, Is.EqualTo("ОГУП \"Липецкфармация\""));
            Assert.That(invoice.BuyerAddress, Is.EqualTo("398043 г.Липецк,ул.Гагарина, дом 113 Тел: (4742)77-74-76"));
            Assert.That(invoice.BuyerINN, Is.EqualTo("4826022196"));
            Assert.That(invoice.BuyerKPP, Is.EqualTo("462601001"));
            Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
            Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(13651.69));
            Assert.That(invoice.NDSAmount10, Is.EqualTo(1365.16));
            Assert.That(invoice.Amount10, Is.EqualTo(15016.85));
            Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(328.68));
            Assert.That(invoice.NDSAmount18, Is.EqualTo(59.17));
            Assert.That(invoice.Amount18, Is.EqualTo(387.85));
            Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(13980.37));
            Assert.That(invoice.NDSAmount, Is.EqualTo(1424.33));
            Assert.That(invoice.Amount, Is.EqualTo(15404.70));
        }
    }
}
