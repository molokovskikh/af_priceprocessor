using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
    [TestFixture]
    public class VectorGroupKalugaParserFixture
    {
        [Test]
        public void Parse()
        {
            var doc = WaybillParser.Parse("a2182.txt");
            
            Assert.That(doc.Lines.Count, Is.EqualTo(4));
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("2182"));
            Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("10.06.2011"));

            Assert.That(doc.Lines[0].Code, Is.Null);
            Assert.That(doc.Lines[0].Product, Is.EqualTo("Клинекс плат.Велти.персик"));
            Assert.That(doc.Lines[0].Quantity, Is.EqualTo(5));
            Assert.That(doc.Lines[0].Producer, Is.EqualTo("КИМБЕРЛИ-КЛАРК гигиена"));
            Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(4.47));
            Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(5.28));
            Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
            Assert.That(doc.Lines[0].RegistryCost, Is.Null);
            Assert.That(doc.Lines[0].ProducerCost, Is.Null);
            Assert.That(doc.Lines[0].Nds, Is.EqualTo(18));
            Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(4.03));
            Assert.That(doc.Lines[0].Amount, Is.EqualTo(26.40));
            Assert.That(doc.Lines[0].SerialNumber, Is.Null);            
            Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС IT АЕ95 В 00535"));
            Assert.That(doc.Lines[0].Country, Is.EqualTo("Польша"));
            Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("17.01.2009"));
            Assert.That(doc.Lines[0].VitallyImportant, Is.False);
        }

        [Test]
        public void Parse2()
        {
            var doc = WaybillParser.Parse("a34202.txt");
            Assert.That(doc.Lines.Count, Is.EqualTo(6));
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("34202"));
            Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("22.06.2011"));

            Assert.That(doc.Lines[0].Code, Is.Null);
            Assert.That(doc.Lines[0].Product, Is.EqualTo("Крем ВИНОГРАД увлажняющий 40мл НК нор/комб/к*36"));
            Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
            Assert.That(doc.Lines[0].Producer, Is.EqualTo("НЕВСКАЯ КОСМЕТИКА"));
            Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(18.53));
            Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(21.86));
            Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
            Assert.That(doc.Lines[0].RegistryCost, Is.Null);
            Assert.That(doc.Lines[0].ProducerCost, Is.Null);
            Assert.That(doc.Lines[0].Nds, Is.EqualTo(18));
            Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(10.00));
            Assert.That(doc.Lines[0].Amount, Is.EqualTo(65.58));
            Assert.That(doc.Lines[0].SerialNumber, Is.Null);
            Assert.That(doc.Lines[0].Period, Is.EqualTo("28.10.2012"));
            Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU АЕ45 В 27744"));
            Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
            Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("14.12.2012"));
            Assert.That(doc.Lines[0].VitallyImportant, Is.False);
        }

        [Test]
        public void Check_file_format()
        {
            Assert.IsTrue(VectorGroupKalugaParser.CheckFileFormat(@"..\..\Data\Waybills\a2182.txt"));
        }
    }
}
