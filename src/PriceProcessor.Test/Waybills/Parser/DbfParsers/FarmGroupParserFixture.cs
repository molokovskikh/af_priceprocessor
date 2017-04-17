using System;
using System.Globalization;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class FarmGroupParserFixture
	{
		[Test]
		public void CertificateTest()
		{
			var doc = WaybillParser.Parse("00180725.dbf");
			var line = doc.Lines[0];
			Assert.That(line.CertificateFilename, Is.EqualTo("120508-60413-O-1.JPG"));
		}

		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00013602.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00013602"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("17.05.10")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("С00027133"));
			Assert.That(line.Product, Is.EqualTo("Валериана (эк-т таб. п/о 0,02г №50)"));
			Assert.That(line.Producer, Is.EqualTo("Озон, ООО"));
			Assert.That(line.SerialNumber, Is.EqualTo("161009"));
			Assert.That(line.Period, Is.EqualTo("01.11.2012"));
			Assert.That(line.SupplierCost, Is.EqualTo(6.96));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.д08755"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(3.9d));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(6.3273));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(6.09));
		}

		[Test]
		public void Parse_Avesta_Farmatsevtika()
		{
			var doc = WaybillParser.Parse("106836_10.dbf");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("106836"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("29.06.2010")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("43423"));
			Assert.That(line.Product, Is.EqualTo("ВИАГРА таб п/о 100мг N1"));
			Assert.That(line.Producer, Is.EqualTo("Pfizer"));
			Assert.That(line.SerialNumber, Is.EqualTo("8312804"));
			Assert.That(line.Period, Is.EqualTo("01.12.2013"));
			Assert.That(line.SupplierCost, Is.EqualTo(472.6600));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС FR.ФМ08.Д94373"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(10));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Country, Is.EqualTo("Франция"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(429.6900));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(390.6300));
		}

		[Test]
		public void Parse_PulsBryansk()
		{
			var doc = WaybillParser.Parse("7997577_PulsBryansk(N_11121_11126_00031791).dbf");
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00015"));
			Assert.That(line.Product, Is.EqualTo("Аджисепт /ментол-эвкалипт/ табл. д/рассас. х24"));
			Assert.That(line.Producer, Is.EqualTo("AGIO"));
			Assert.That(line.SerialNumber, Is.EqualTo("10/14/0022"));
			Assert.That(line.Period, Is.EqualTo("01.09.2013"));
			Assert.That(line.SupplierCost, Is.EqualTo(25.76));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Certificates, Is.EqualTo("РОСС IN.ФM08.Д58735"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Country, Is.EqualTo("Индия"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(23.42));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(23.42));
		}

		[Test]
		public void Parse_BiolainAMPFarmpreparaty()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\17913.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(3));

			var line = document.Lines[1];

			Assert.That(line.Code, Is.EqualTo("726481"));
			Assert.That(line.Product, Is.EqualTo("Медицинский антисептический раствор~р-р наружн.~70%~фл.100мл N1~Фармацевтический"));
			Assert.That(line.SerialNumber, Is.EqualTo("031211"));
			Assert.That(line.Period, Is.EqualTo("01.12.2016"));
			Assert.That(line.SupplierCost, Is.EqualTo(13.11));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU ФМ01 Д70630"));
			Assert.That(line.CertificatesDate, Is.EqualTo("30.03.2012"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(20.52));
			Assert.That(line.Producer, Is.EqualTo("Фармацевтический комбинат"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(11.92));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
		}

		/// <summary>
		/// К задаче
		/// http://redmine.analit.net/issues/28233
		/// </summary>
		[Test]
		public void Parse_CIA()
		{
			var file = @"..\..\Data\Waybills\Р5921938.DBF";
			var document = WaybillParser.Parse(file);

			Assert.That(document.Lines.Count, Is.EqualTo(13));
			var line = document.Lines[0];

			Assert.That(line.Code, Is.EqualTo("4056"));
			Assert.That(line.EAN13, Is.EqualTo("4030685240329"));
			Assert.That(line.UnitCode, Is.EqualTo("778"));
			Assert.That(line.CountryCode, Is.EqualTo("380"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10113080/250214/0003145/4"));
		}
	}
}