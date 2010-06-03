﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	public class SiaKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("3879289_СИА_Интернейшнл_Р-424409_.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-424409"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("28.05.2010")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("79460"));
			Assert.That(line.Product, Is.EqualTo("Вильпрафен Солютаб дисперг. 1000мг Таб. Х10"));
			Assert.That(line.Producer, Is.EqualTo("Yamanouchi"));
			Assert.That(line.Country, Is.EqualTo("Италия"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.10.2011"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС IT.ФМ09.Д03420 ()"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(552.0300));
			Assert.That(line.SupplierCost, Is.EqualTo(607.2300));
			Assert.That(line.SerialNumber, Is.EqualTo("09J01/87"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
		}

		[Test]
		public void Parse_SiaInternationalSpb()
		{
			var doc = WaybillParser.Parse("3889638_Сиа Интернейшнл(Р-2616032).DBF");
			
			Assert.That(doc.ProviderDocumentId, Is.EqualTo(Document.GenerateProviderDocumentId()));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("86446"));
			Assert.That(line.Product, Is.EqualTo("Адаптол таб. 500мг №20"));
			Assert.That(line.Producer, Is.EqualTo("Олайнский Хфз"));
			Assert.That(line.Country, Is.EqualTo("Латвия"));
			Assert.That(line.Quantity, Is.EqualTo(1.0000));
			Assert.That(line.ProducerCost, Is.EqualTo(199.1100));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.03.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС LV.ФМ01.Д98043 ( ФГУ центр сертификации минестерства здравоохранения РФ)"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(210.7400));
			Assert.That(line.SupplierCost, Is.EqualTo(231.8100));
			Assert.That(line.SerialNumber, Is.EqualTo("120310"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.SupplierPriceMarkup, Is.Null);
			Assert.That(line.VitallyImportant, Is.Null);
		}
	}
}
