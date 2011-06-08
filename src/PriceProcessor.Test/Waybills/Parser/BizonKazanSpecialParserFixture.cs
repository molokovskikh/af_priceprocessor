using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
    [TestFixture]
    public class BizonKazanSpecialParserFixture
    {
        [Test]
        public void Parse()
        {
            DocumentReceiveLog documentLog = null;
            using (new SessionScope())
            {
                var supplier = Supplier.Find(8063u); // код поставщика "Бизон" (Казань)
                documentLog = new DocumentReceiveLog { Supplier = supplier, };
                documentLog.CreateAndFlush();
            }
            Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\N000011720.txt", documentLog) is BizonKazanSpecialParser);

            var doc = WaybillParser.Parse("N000011720.txt", documentLog);
            Assert.That(doc.Lines.Count, Is.EqualTo(15));
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-000011720"));
            Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("30.05.2011"));

            Assert.That(doc.Lines[0].Code, Is.Null);
            Assert.That(doc.Lines[0].Product, Is.EqualTo("Аптечка автомобильная (нового образца   )"));
            Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("*"));
            Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
            Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(115.91));
            Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
            Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(11.59));
            Assert.That(doc.Lines[0].Amount, Is.EqualTo(127.5));
            Assert.That(doc.Lines[0].Producer, Is.EqualTo("Фарм-Глобал"));
            Assert.That(doc.Lines[0].Period, Is.EqualTo("01.09.13"));

            Assert.That(doc.Lines[0].Country, Is.Null);
            Assert.That(doc.Lines[0].ProducerCost, Is.Null);
            Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(127.5));                                           
            Assert.That(doc.Lines[0].Certificates, Is.Null);
            Assert.That(doc.Lines[0].RegistryCost, Is.Null);
            Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
            Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
        }
    }
}
