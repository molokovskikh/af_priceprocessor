using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
    [TestFixture]
    public class TredifarmParserFixture
    {
        [Test]
        public void Parse()
        {
            var doc = WaybillParser.Parse("00112517.txt");
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("РНТ-000000112517"));
            Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("14.06.2011")));
            Assert.That(doc.Lines.Count, Is.EqualTo(13));
            Assert.That(doc.Lines[0].Code, Is.EqualTo("00000667"));
            Assert.That(doc.Lines[0].Product, Is.EqualTo("Галазолин 0,1% 10мл фл капли в нос - POLFA"));
            Assert.That(doc.Lines[0].Producer, Is.EqualTo("POLFA"));
            Assert.That(doc.Lines[0].Country, Is.EqualTo("Польша"));
            Assert.That(doc.Lines[0].Quantity, Is.EqualTo(5));
            Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(22.65));
            Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(26.14));
            Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(28.75));            
            Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
            Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(13.07));
            Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("13UK1010"));
            Assert.That(doc.Lines[0].Period, Is.EqualTo("01.10.2014"));
            Assert.That(doc.Lines[0].Certificates, Is.Null);
            Assert.That(doc.Lines[0].CertificatesDate, Is.Null);
            Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(25.21));
            Assert.That(doc.Lines[0].VitallyImportant, Is.True);

            doc = WaybillParser.Parse("8618961.txt");
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("РНТ-000000113691"));
            Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.06.2011")));
            Assert.That(doc.Lines.Count, Is.EqualTo(7));
            Assert.That(doc.Lines[0].Code, Is.EqualTo("00009618"));
            Assert.That(doc.Lines[0].Product, Is.EqualTo("Бромгексин  4мг/5мл 100мл  сироп - Фармстандарт-Лексредства ОАО"));
            Assert.That(doc.Lines[0].Producer, Is.EqualTo("Фармстандарт-Лексредства ОАО"));
            Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
            Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
            Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(24.21));
            Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(24.21));
            Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(26.63));
            Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
            Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(7.26));
            Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("1022011"));
            Assert.That(doc.Lines[0].Period, Is.EqualTo("01.03.2014"));
            Assert.That(doc.Lines[0].Certificates, Is.Null);
            Assert.That(doc.Lines[0].CertificatesDate, Is.Null);
            Assert.That(doc.Lines[0].RegistryCost, Is.Null);
            Assert.That(doc.Lines[0].VitallyImportant, Is.False);
            Assert.That(doc.Lines[0].Amount, Is.EqualTo(79.89));
        }
    }
}
