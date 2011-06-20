using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            Assert.That(doc.Lines[0].Code, Is.EqualTo("1"));
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
    }
}
