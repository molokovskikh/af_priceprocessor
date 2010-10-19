﻿using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class ForaFarmVoronezhParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("39276.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("39276"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("01.10.2010"));
			var line = doc.Lines[0];
			//Assert.That(.S, Is.EqualTo("815575"));
			//Assert.That(line.Code, Is.EqualTo("05244"));
			Assert.That(line.Product, Is.EqualTo("911 Окопник гель-бальзам 100мл"));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Quantity, Is.EqualTo(10));
			//Assert.That(line.ProducerCost, Is.EqualTo(40.18));
			Assert.That(line.SupplierCost, Is.EqualTo(40.78));
			//Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(42.89));
			Assert.That(line.RegistryCost, Is.EqualTo(null));
			Assert.That(line.Period, Is.EqualTo("01.02.2012"));
			Assert.That(line.SerialNumber, Is.EqualTo("0810"));
			//Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("Твинс Тэк"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АИ86.Д00149"));
			//Assert.That(line.VitallyImportant, Is.True);
		}
	}
}
