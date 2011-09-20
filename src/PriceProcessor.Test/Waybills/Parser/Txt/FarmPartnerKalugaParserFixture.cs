using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
    [TestFixture]
    public class FarmPartnerKalugaParserFixture
    {
        [Test]
        public void Parse()
        {
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 4910u } }; // код поставщика Фармпартнер (Калуга)            
            Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\06532.sst", documentLog) is FarmPartnerKalugaParser);

            var doc = WaybillParser.Parse("06532.sst", documentLog);
            Assert.That(doc.Lines.Count, Is.EqualTo(1));
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("РНк-006532"));
            Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("12.07.2011"));

            Assert.That(doc.Lines[0].Code, Is.EqualTo("125065"));
            Assert.That(doc.Lines[0].Product, Is.EqualTo("L-ТИРОКСИН ТАБ 50МКГ N50"));
            Assert.That(doc.Lines[0].Producer, Is.EqualTo("БЕРЛИН-ХЕМИ АГ"));
            Assert.That(doc.Lines[0].Country, Is.EqualTo("ГЕРМАНИЯ"));
            Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));

            Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(68.25));
            Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(75.08));
            Assert.That(doc.Lines[0].Period, Is.EqualTo("01.10.12"));
            Assert.That(doc.Lines[0].BillOfEntryNumber, Is.EqualTo("10110110/091210/0014229/1"));
            Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС DE.ФМ01.Д36775"));
            Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("04031"));
            Assert.That(doc.Lines[0].EAN13, Is.Null);
            Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(69.33));
            Assert.That(doc.Lines[0].VitallyImportant, Is.True);
            Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(6.83));
            Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
            Assert.That(doc.Lines[0].Amount, Is.EqualTo(75.08));
            Assert.That(doc.Lines[0].ProducerCost, Is.Null);
        }
    }
}
