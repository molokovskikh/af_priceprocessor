using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
    [TestFixture]
    public class PulsBryanskParserFixture
    {
        /// <summary>
        /// К задаче http://redmine.analit.net/issues/38265
        /// </summary>
        [Test]
        public void Parse()
        {
            var doc = WaybillParser.Parse("00248858.sst");
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("00248858"));
            Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("28.08.2015")));

            var line = doc.Lines[0];
            Assert.That(line.Code, Is.EqualTo("07698"));
            Assert.That(line.Product, Is.EqualTo("Антифлу Кидс пор. д/приг. р-ра д/пероральн. прим. х5"));
            Assert.That(line.Producer, Is.EqualTo("Contract Pharmacal Corp"));
            Assert.That(line.Country, Is.EqualTo("США"));
            Assert.That(line.Quantity, Is.EqualTo(1));
            Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(114.45));
            Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(114.45));
            Assert.That(line.Nds, Is.EqualTo(10));
            Assert.That(line.NdsAmount, Is.EqualTo(11m));
            Assert.That(line.Period, Is.EqualTo("01.10.2017"));
            Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130032/050515/0003256/1"));
            Assert.That(line.Certificates, Is.EqualTo("031519^01.10.2017^РОСС US.ФM08.Д61049, РОСС US.ФM08.Д53926^22.04.2015^01.10.2017^^^ООО \"Окружной центр контроля качества\" г. Москва"));
            Assert.That(line.EAN13, Is.EqualTo(4250369500871));
            Assert.That(line.VitallyImportant, Is.EqualTo(false));
            Assert.That(line.ProducerCost, Is.EqualTo(125.9m));
        }
    }
}
