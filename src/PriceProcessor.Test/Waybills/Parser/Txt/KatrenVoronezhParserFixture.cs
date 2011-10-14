using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
    [TestFixture]
    public class KatrenVoronezhParserFixture
    {
        [Test]
        public void Parse()
        {
            var doc = WaybillParser.Parse(@"137803.txt");
            Assert.That(doc.Lines.Count, Is.EqualTo(1));
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("137803"));
            Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("20.06.2011")));

            Assert.That(doc.Lines[0].Code, Is.Null);
            Assert.That(doc.Lines[0].Product, Is.EqualTo("МАМОКЛАМ 0,1 N40 ТАБЛ П/О"));
            Assert.That(doc.Lines[0].Unit, Is.EqualTo("уп."));
            Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
            Assert.That(doc.Lines[0].Producer, Is.EqualTo("Мега Фарм ЗАО"));
            Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(496.54));
            Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(451.40));
            Assert.That(doc.Lines[0].ExciseTax, Is.Null);
            Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0.00));
            Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0.00));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(451.40));
            Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
            Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(45.14));
            Assert.That(doc.Lines[0].Amount, Is.EqualTo(496.54));
            Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("020211"));
            Assert.That(doc.Lines[0].Period, Is.EqualTo("01.02.2013"));
            Assert.That(doc.Lines[0].Certificates, Is.EqualTo("POCC RU.ФМ08.Д98760"));
            Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
            Assert.That(doc.Lines[0].BillOfEntryNumber, Is.Null);
            Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("11.03.2011"));
            Assert.That(doc.Lines[0].VitallyImportant, Is.False);
            Assert.That(doc.Lines[0].EAN13, Is.EqualTo("4607061790596"));

            var invoice = doc.Invoice;
            Assert.That(invoice, Is.Not.Null);
            Assert.That(invoice.InvoiceNumber, Is.EqualTo("137803"));
            Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("20.06.2011"));
            Assert.That(invoice.SellerName, Is.EqualTo("ЗАО НПК \"Катрен\""));
            Assert.That(invoice.SellerAddress, Is.EqualTo("Россия, 630117, г. Новосибирск, ул. Тимакова, д. 4."));
            Assert.That(invoice.SellerINN, Is.EqualTo("5408130693"));
            Assert.That(invoice.SellerKPP, Is.EqualTo("366502001"));
            Assert.That(invoice.ShipperInfo, Is.EqualTo("Филиал ЗАО НПК \"Катрен\" в г.Воронеж,394065, Россия, г. Воронеж, пр-т Патриотов д. 57а"));
            Assert.That(invoice.ConsigneeInfo, Is.EqualTo("Аптека №116, г. Липецк, ул. Неделина, д.31А, нежилое помещение№1"));
            Assert.That(invoice.PaymentDocumentInfo, Is.Null);
            Assert.That(invoice.BuyerName, Is.EqualTo("ЛИПЕЦК, ОГУП* ЛИПЕЦКФАРМАЦИЯ *"));
            Assert.That(invoice.BuyerAddress, Is.EqualTo("г. Липецк, ул. Гагарина,д. 113"));
            Assert.That(invoice.BuyerINN, Is.EqualTo("4826022196"));
            Assert.That(invoice.BuyerKPP, Is.EqualTo("482501001"));
            Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
            Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(451.40));
            Assert.That(invoice.NDSAmount10, Is.EqualTo(45.14));
            Assert.That(invoice.Amount10, Is.EqualTo(496.54));
            Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(0.00));
            Assert.That(invoice.NDSAmount18, Is.EqualTo(0.00));
            Assert.That(invoice.Amount18, Is.EqualTo(0.00));
            Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(451.40));
            Assert.That(invoice.NDSAmount, Is.EqualTo(45.14));
            Assert.That(invoice.Amount, Is.EqualTo(496.54));
        }
    }
}
