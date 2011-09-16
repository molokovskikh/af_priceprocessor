using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
    [TestFixture]
    public class ZdravServiceSpecialParserFixture
    {
        [Test]
        public void Parse()
        {
            DocumentReceiveLog documentLog = null;
            using (new SessionScope())
            {
                var supplier = Supplier.Find(1581u); // код поставщика Здравсервис
                documentLog = new DocumentReceiveLog { Supplier = supplier, };
                documentLog.CreateAndFlush();
            }
            Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\2094247.dbf", documentLog) is ZdravServiceSpecialParser);

            var document = WaybillParser.Parse("2094247.dbf", documentLog);

            Assert.That(document.Lines.Count, Is.EqualTo(22));
            Assert.That(document.ProviderDocumentId, Is.EqualTo("2094247"));
            Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("01.07.2011"));

            var invoice = document.Invoice;
            Assert.That(invoice, Is.Not.Null);
            Assert.That(invoice.InvoiceNumber, Is.EqualTo("2094247"));
            Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("01.07.2011"));
            Assert.That(invoice.SellerName, Is.EqualTo("ЗДРАВСЕРВИС"));
            Assert.That(invoice.SellerAddress, Is.EqualTo("300026, г.Тула, ул.Скуратовская, д.107"));
            Assert.That(invoice.SellerINN, Is.EqualTo("4826022196"));
            Assert.That(invoice.SellerKPP, Is.EqualTo("482601001"));
            Assert.That(invoice.ShipperInfo, Is.EqualTo("ООО Здравсервис, 300026, г.Тула, ул.Скуратовская, д.107"));
            Assert.That(invoice.ConsigneeInfo, Is.EqualTo("ОГУП \"Липецкфармация\" аптека №1, г. Липецк, ул.Ворошилова, д.1"));
            Assert.That(invoice.PaymentDocumentInfo, Is.Null);
            Assert.That(invoice.BuyerName, Is.EqualTo("ОГУП \"Липецкфармация\""));
            Assert.That(invoice.BuyerAddress, Is.EqualTo("398025, г. Липецк, ул. Гагарина, 113"));
            Assert.That(invoice.BuyerINN, Is.Null);
            Assert.That(invoice.BuyerKPP, Is.Null);
            Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
            Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(10365.77));
            Assert.That(invoice.NDSAmount10, Is.EqualTo(1036.59));
            Assert.That(invoice.Amount10, Is.EqualTo(11402.36));
            Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(0.00));
            Assert.That(invoice.NDSAmount18, Is.EqualTo(0.00));
            Assert.That(invoice.Amount18, Is.EqualTo(0.00));
            Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(0)); // ?
            Assert.That(invoice.NDSAmount, Is.EqualTo(11402.36)); // ? 
            Assert.That(invoice.Amount, Is.EqualTo(11402.36));

            var line = document.Lines[0];
            Assert.That(line.Code, Is.EqualTo("252740"));
            Assert.That(line.Product, Is.EqualTo("Бактериофаг клебсиеллезный поливалентный очищенный жидкий фл 20мл N4"));
            Assert.That(line.Unit, Is.EqualTo("шт."));
            Assert.That(line.Quantity, Is.EqualTo(3));
            Assert.That(line.Producer, Is.EqualTo("Микроген-Иммун"));
            Assert.That(line.SupplierCost, Is.EqualTo(632.53));
            Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(575.03));
            Assert.That(line.ExciseTax, Is.Null);
            Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));
            Assert.That(line.RegistryCost, Is.Null);
            Assert.That(line.ProducerCost, Is.EqualTo(485.76));
            Assert.That(line.Nds, Is.EqualTo(10));
            Assert.That(line.NdsAmount, Is.EqualTo(172.51));
            Assert.That(line.Amount, Is.EqualTo(1897.59));
            Assert.That(line.SerialNumber, Is.EqualTo("Y125/0211"));
            Assert.That(line.Period, Is.EqualTo("01.03.2013"));
            Assert.That(line.Certificates, Is.EqualTo("002840"));
            Assert.That(line.Country, Is.EqualTo("РОС"));
            Assert.That(line.BillOfEntryNumber, Is.Null);            
            Assert.That(line.VitallyImportant, Is.False);
            Assert.That(line.EAN13, Is.EqualTo("4600488003393"));
        }
    }
}
