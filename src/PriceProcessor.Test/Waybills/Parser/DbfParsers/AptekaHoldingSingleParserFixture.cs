﻿using System;
using System.Data;
using System.IO;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NHibernate.Linq;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class AptekaHoldingSingleParserFixture
	{
		[Test, Description("Просто проверяем, что при отсутствии колонки TZENA парсинг происходит")]
		public void No_Tzena()
		{
			WaybillParser.Parse("30893754_001.dbf");
		}

		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\А0973748.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(34));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-0973748"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("09/12/2009")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("27618"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Sanosan крем защитный от опрелостей с оливк. масл. и молочн. протеином"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Mann&Schroder GmbH"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Германия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(83.92));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(85.60));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("-"));
			Assert.That(document.Lines[1].SerialNumber, Is.EqualTo("AE39L8D"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС.DE.ПК08.В01732 д"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.10.2011"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[3].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00000));
			Assert.That(document.Lines[3].RegistryCost, Is.EqualTo(238.69));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(101.01));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(2));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(15.41));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(101.01));
		}

		[Test]
		public void WithEan13Test()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\1875_00000032569.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(9));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00000032569/0"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("05.12.2012")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("26401"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Акридерм мазь 0.064% туба 15г Россия"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Акрихин  ХФК ОАО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(55.32m));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(53.1m));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("1060512"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д63755"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.05.2014"));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[1].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(55.32));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(58.41));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(10.62m));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(116.82m));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo(4601969005486));
		}

		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\R1363495.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(40));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-1363495"));
		}

		[Test]
		public void Parse3()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\1281_1087568.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(8));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-1087568"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("29/04/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("3"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("L-тироксин табл. 100мкг N100  (ЖВЛС)"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Berlin-Chemie AG/Menarini Group"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Германия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(107.12));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(104.90));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("93052"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС.DE.ФМ01.Д27143 д"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.08.2011"));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(107.75));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(115.39));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(20.98));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(230.78));
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\R1362131.DBF")));
			Assert.IsTrue(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\А0973748.DBF")));
			Assert.IsTrue(AptekaHoldingSingleParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\7883837_Rosta_10213326_.DBF")));
		}

		[Test]
		public void Parse4()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\21796_1272844.dbf");
		}

		[Test]
		public void Parse5()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\7883837_Rosta_10213326_.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(14));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("10213326"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("19.04.2011"));

			Assert.That(document.Lines[0].Code, Is.EqualTo("34387"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("911 ОКОПНИК Г-БАЛЬЗ ПРИ БОЛИ10"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Твинс Тэк ЗАО - Россия"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("-"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(35.23));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(37.88));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("022011"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU АИ86 Д00149 Д"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.08.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18.00));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(44.70));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(7.52));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(34.09));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(223.49));
		}

		[Test]
		public void Parse6()
		{
			var document = WaybillParser.Parse(@"9764791.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(149));
			Assert.That(document.Lines[1].Product, Is.EqualTo("Аллохол табл. п/о N24"));
		}

		[Test]
		public void Parse7WithCertificateFiles()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\9832937_Аптека-Холдинг(3334_1459366).dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(26));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-1459366"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("15.09.2011")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("28455"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Артро-актив капс. 300мг N36 Россия"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Диод ОАО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(52.94));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(54.00));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("080711"));
			Assert.That(document.Lines[1].SerialNumber, Is.EqualTo("24212051"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ11.Д35118 д"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.07.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[1].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00000));
			Assert.That(document.Lines[1].RegistryCost, Is.EqualTo(100.41));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[1].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(63.72));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(2));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(9.72));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(63.72));
			Assert.That(document.Lines[0].CertificateFilename, Is.EqualTo("sLA9S"));
			Assert.That(document.Lines[1].CertificateFilename, Is.EqualTo("sM9TM"));
			Assert.That(document.Lines[8].CertificateFilename, Is.EqualTo("sFR06"));
			Assert.That(document.Lines[0].ProtocolFilemame, Is.Null.Or.Empty);
			Assert.That(document.Lines[1].ProtocolFilemame, Is.Null.Or.Empty);
			Assert.That(document.Lines[8].ProtocolFilemame, Is.EqualTo("rFR06"));
			Assert.That(document.Lines[0].PassportFilename, Is.EqualTo("pMAJO"));
			Assert.That(document.Lines[1].PassportFilename, Is.EqualTo("pM9TM"));
			Assert.That(document.Lines[8].PassportFilename, Is.Null.Or.Empty);
		}

		/// <summary>
		/// Новые данные от задачи
		/// http://redmine.analit.net/issues/28233
		/// </summary>
		[Test]
		public void Parse8()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\6562_2488183.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(7));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-2488183"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("13.10.2014")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("37164"));
			Assert.That(document.Lines[0].UnitCode, Is.EqualTo("778"));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo(4605964003874));
			Assert.That(document.Lines[0].CountryCode, Is.EqualTo("643"));
			Assert.That(document.Lines[5].BillOfEntryNumber, Is.EqualTo("10103090/190214/0000966/1"));
		}

		[Test]
		public void Universal_format_test()
		{
			var document = WaybillParser.Parse("3960_00000030842 (3).dbf");
			var fileName = Path.Combine(@"..\..\Data\Waybills\", "universalDbf.dbf");
			try {
				var log = new DocumentReceiveLog { Supplier = new Supplier() };
				document.Log = log;
				document.Address = new Address();
				DbfExporter.SaveUniversalV2(document, fileName);
				var table = Dbf.Load(fileName);
				var amnt = table.Rows[0]["AMNT"];
				Assert.AreEqual(amnt.ToString(), "5182,45");
				var amntNds = table.Rows[0]["amnt_n_all"];
				Assert.AreEqual(amntNds.ToString(), "507,25");
			}
			finally {
				File.Delete(fileName);
			}
		}

        /// <summary>
        /// Для задачи http://redmine.analit.net/issues/37439
        /// </summary>
        [Test]
        public void Parse9()
        {
            var document = WaybillParser.Parse(@"..\..\Data\Waybills\4193_2803785.dbf");

            Assert.That(document.Lines[0].Product, Is.EqualTo("Амбробене табл. 30мг N20 Германия"));
            Assert.That(document.Lines[0].OrderId, Is.EqualTo(74101766));
        }

        /// <summary>
        /// К задаче http://redmine.analit.net/issues/37655
        /// </summary>
        [Test]
        public void Parse10()
        {
            var document = WaybillParser.Parse(@"..\..\Data\Waybills\6626_2811983.dbf");
            Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-2811983/35"));
            Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("20.08.2015")));
            Assert.That(document.Lines[0].Code, Is.EqualTo("31853"));
            Assert.That(document.Lines[0].Product, Is.EqualTo("Аллапинин табл. 0.025г N30 Россия"));
            Assert.That(document.Lines[0].Producer, Is.EqualTo("Фармцентр Вилар ЗАО"));
            Assert.That(document.Lines[0].OrderId, Is.Null);
        }

		[Test]
		public void Parse11()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\16695644.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(14));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.Null);
		}
	}
}