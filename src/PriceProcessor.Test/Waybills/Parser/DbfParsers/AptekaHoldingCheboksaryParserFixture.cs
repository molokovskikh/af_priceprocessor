using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
    [TestFixture]
    public class AptekaHoldingCheboksaryParserFixture
    {
        [Test]
        public void Parse()
        {
            var document = WaybillParser.Parse(@"..\..\Data\Waybills\725951.dbf");
            Assert.That(document.Lines.Count, Is.EqualTo(7));
            Assert.That(document.ProviderDocumentId, Is.EqualTo("00000725951/0"));
            Assert.That(document.DocumentDate, !Is.Null);
            Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("06.07.2011"));

            var line = document.Lines[0];
            Assert.That(line.Code, Is.EqualTo("7975"));
            Assert.That(line.Product, Is.EqualTo("Бинт эласт медиц ВР  3м х 8см Латвия"));
            Assert.That(line.Producer, Is.EqualTo("Tonus Elast ООО"));
            Assert.That(line.Country, Is.EqualTo("Латвия"));
            Assert.That(line.BillOfEntryNumber, Is.EqualTo("10225040/140311/0000478/1"));
            Assert.That(line.Quantity, Is.EqualTo(1));
            Assert.That(line.ProducerCost, Is.EqualTo(31.30));
            Assert.That(line.SupplierPriceMarkup, Is.EqualTo(13.10));
            Assert.That(line.Nds, Is.EqualTo(10.00));
            Assert.That(line.NdsAmount, Is.EqualTo(3.54));
            Assert.That(line.SupplierCost, Is.EqualTo(38.94));
            Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(35.40));
            Assert.That(line.Amount, Is.EqualTo(38.94));
            Assert.That(line.SerialNumber, Is.EqualTo("-**11.04.11"));
            Assert.That(line.Certificates, Is.EqualTo("РОСС LV.ИМ25.В02516"));
            Assert.That(line.CertificatesDate, Is.EqualTo("13.08.2012"));
            Assert.That(line.Period, Is.EqualTo("01.03.2016"));
            Assert.That(line.RegistryCost, Is.EqualTo(0.00));            
            Assert.That(line.VitallyImportant, Is.False);            
        }

        [Test]
        public void Check_file_format()
        {
            Assert.IsTrue(AptekaHoldingCheboksaryParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\725951.dbf")));
        }
    }
}
