﻿using System;
using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
    [TestFixture]
    public class SiaInternationalOmskParserFixture
    {
        [Test]
        public void Parse() {
            var doc = WaybillParser.Parse("SIAPdbf.dbf");
            //Assert.That(doc.ProviderDocumentId, Is.EqualTo(Document.GenerateProviderDocumentId()));
            //Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
            //Assert.That(doc.ProviderDocumentId, Is.EqualTo("NN"));
            //Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.09.2010"));
            var line = doc.Lines[0];
            Assert.That(line.Product, Is.EqualTo("Микардис 40мг Таб. Х14"));
            Assert.That(line.Quantity, Is.EqualTo(1));
            Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ01.Д00848 (ФГУ \"ЦС МЗ РФ\" г. Москва)"));
            Assert.That(line.Country, Is.EqualTo("Германия"));
            Assert.That(line.Producer, Is.EqualTo("Boehringer Ingelheim"));
            Assert.That(line.Period, Is.EqualTo("28.02.2013"));
            Assert.That(line.SerialNumber, Is.EqualTo("902878"));
            Assert.That(line.SupplierCost, Is.EqualTo(460.7));
            Assert.That(line.ProducerCost, Is.EqualTo(418.82));
        }
    }
}