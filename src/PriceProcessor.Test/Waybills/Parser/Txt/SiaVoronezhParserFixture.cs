using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
    [TestFixture]
    public class SiaVoronezhParserFixture
    {
        [Test]
        public void Parse()
        {
            var doc = WaybillParser.Parse(@"SiaVoronezh.txt");
            Assert.That(doc.Lines.Count, Is.EqualTo(4));
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-2653170/1"));
            Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("21.07.2011")));

            Assert.That(doc.Lines[0].Code, Is.Null);
            Assert.That(doc.Lines[0].Product, Is.EqualTo("Утрожестан 200мг Капс. Х14 (R)"));
            Assert.That(doc.Lines[0].Unit, Is.EqualTo("шт."));
            Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
            Assert.That(doc.Lines[0].Producer, Is.EqualTo("Besins"));
            Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(317.99));
            Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(289.08));
            Assert.That(doc.Lines[0].ExciseTax, Is.Null);
            Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
            Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(311.90));
            Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(297.34));
            Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
            Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(57.82));
            Assert.That(doc.Lines[0].Amount, Is.EqualTo(635.98));
            Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("1303"));
            Assert.That(doc.Lines[0].Period, Is.EqualTo("24.10.2013"));
            Assert.That(doc.Lines[0].Certificates, Is.EqualTo("POCC BE.ФМ01.Д30436"));
            Assert.That(doc.Lines[0].Country, Is.EqualTo("Франция"));
            Assert.That(doc.Lines[0].BillOfEntryNumber, Is.EqualTo("10130030/270411/0001571/2"));
            Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("24.10.2013"));
            Assert.That(doc.Lines[0].VitallyImportant, Is.True);
            Assert.That(doc.Lines[0].EAN13, Is.EqualTo("3700039500430"));

            var invoice = doc.Invoice;
            Assert.That(invoice, Is.Not.Null);
            Assert.That(invoice.InvoiceNumber, Is.EqualTo("Р-2653170/1"));
            Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("21.07.2011"));
            Assert.That(invoice.SellerName, Is.EqualTo("Закрытое акционерное общество \"СИА Интернейшнл - Воронеж\""));
            Assert.That(invoice.SellerAddress, Is.EqualTo("394026, г.Воронеж, Проспект Труда, д.65"));
            Assert.That(invoice.SellerINN, Is.EqualTo("3662047599"));
            Assert.That(invoice.SellerKPP, Is.EqualTo("366201001"));
            Assert.That(invoice.ShipperInfo, Is.EqualTo("ЗАО \"СИА Интернейшнл - Воронеж\", 394026, г.Воронеж, Проспект Труда, д.65"));
            Assert.That(invoice.ConsigneeInfo, Is.EqualTo("ОГУП \"Липецкфармация\", 398059, г.Липецк, ул.Неделина, д.31\"а\", нежилое помещение №1"));
            Assert.That(invoice.PaymentDocumentInfo, Is.Null);

            Assert.That(invoice.BuyerName, Is.EqualTo("ОГУП \"Липецкфармация\""));
            Assert.That(invoice.BuyerAddress, Is.EqualTo("398043 г. Липецк, ул.Гагарина, д.113"));
            Assert.That(invoice.BuyerINN, Is.EqualTo("4826022196"));
            Assert.That(invoice.BuyerKPP, Is.EqualTo("482601001"));
            Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
            Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(1507.44));
            Assert.That(invoice.NDSAmount10, Is.EqualTo(150.75));
            Assert.That(invoice.Amount10, Is.EqualTo(1658.19));
            Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(0.00));
            Assert.That(invoice.NDSAmount18, Is.EqualTo(0.00));
            Assert.That(invoice.Amount18, Is.EqualTo(0.00));
            Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(1507.44));
            Assert.That(invoice.NDSAmount, Is.EqualTo(150.75));
            Assert.That(invoice.Amount, Is.EqualTo(1658.19));            
        }
    }
}
