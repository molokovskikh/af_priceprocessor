﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class KatrenOrelTxtParserFixture
	{
		[Test]
		public void Parse_Katren_Orel()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\82936.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(38));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("82936"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("30.04.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("13509328"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АКРИДИЛОЛ 0,0125 N30 ТАБЛ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Акрихин ХФК ОАО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(203.74));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(204.40));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(185.82));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("10210"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.11.2013"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д68638"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(203.74));
			
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(20.44));
		}

		[Test]
		public void Parse_Katren_Voronezh()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3767013_Катрен(91136).txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(5));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("91136"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("03.05.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("24648935"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ТЕСТ-ПОЛОСКА САТЕЛЛИТ ПЛЮС ПКГЭ-02.4 N50"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("КОМПАНИЯ ЭЛТА, ООО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(290.00));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(295.47));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(295.47));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(0));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("083"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.05.2011"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("022.а2006/5582-06"));
			Assert.That(doc.Lines[1].Certificates, Is.EqualTo("РОСС HU.ПК12.В06358"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[3].VitallyImportant, Is.True);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
		}

		[Test]
		public void Parse_Katren_Voronezh2()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3919268_Катрен_118340_.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("118340"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("08.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("24187465"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ДОЛФИН СР-ВО ГИГИЕНИЧ Д/ПРОМЫВ 1,0 N30 /ДЕТ/"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Динамика, ООО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(162.00));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(167.46));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(167.46));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(0));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("112009"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.11.2011"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU.АЯ79.В11173"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));

			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[3].VitallyImportant, Is.True);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(doc.Lines[2].SupplierPriceMarkup, Is.EqualTo(205.64));
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\3633567_0_17202011.xml"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\3700197_Протек-21(9041050-001).sst"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\0000470553.dbf"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\3725138_Риа-Панда(07054623).TXT"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\8916.dbf"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\890579.dbf"));
			Assert.IsTrue(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\82936.txt"));
			Assert.IsTrue(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\3767013_Катрен(91136).txt"));
		}

		[Test]
		public void Parse_Biolain_Voronezh()
		{
			var doc = WaybillParser.Parse("3944680_Биолайн(5741).txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(4));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("5741"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("15.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("533973"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Атенолол~табл. 0,05 г уп.контурн.яч. 10 пач.картон. 3~Пранафарм Россия"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Пранафарм"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(20));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(5.89));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(6.21));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(5.65));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("20210"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.03.2012"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU ФМ05 Д87832"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(5.89));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(12.42));
		}

		[Test]
		public void Parse_Rosta_Tumen()
		{
			var doc = WaybillParser.Parse("3911727_Роста(78381_9).txt");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("78381/9"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("04.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("81790"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Пирацетам р-р д/ин. 20% 5мл амп. х 10"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Фармстандарт-Уфимский витам.з-д"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(17.08));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(19.11));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(17.37));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("1530410"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.05.2015"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU ФМ05 Д17069"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(17.08));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(9.56));
		}

		[Test]
		public void Parse_NadezhdaFarm_Tambov()
		{
			var doc = WaybillParser.Parse("3954577_Надежда-Фарм(196336_0).txt");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("196336/0"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("52263"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Бинт трубчатый N3 Апполо (длина 20см)"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Апполо ООО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(5.0571));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(5.84));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(5.31));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("0410"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.04.2015"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОССRUИМО9,В02062"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(2.65));
		}

		[Test]
		public void Parse_NormanPlus_Voronezh()
		{
			var doc = WaybillParser.Parse("3961023_Норман Плюс(00177860).txt");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00177860"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("17.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("105761"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Аджисепт ментол/эвкалипт пастилки №24"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Agio Pharmaceuticals (ИНДИЯ)"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("ИНДИЯ "));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(20.562));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(22.8500));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(20.77));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("10/14/0003"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.12.2012"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС IN.ФМ08 Д81562"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void Parse_Katren_Orel2()
		{
			var doc = WaybillParser.Parse("3956552_Катрен(115820).txt");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("115820"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("17.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("18199289"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ИНГАЛЯТОР WN-116 U УЛЬТР 3Л/МИН ПОРТАТИВНЫЙ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("B. Well Limited"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("аргентина"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(1445.00));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(1468.56));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(1468.56));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(0));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("022010"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.02.2015"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС GB.ИМ04.В07518"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
		}

		[Test]
		public void Parse_Katren_Kazan()
		{
			var doc = WaybillParser.Parse("3952576_Катрен(120165).txt");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("120165"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("2311"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ТЕРМОМЕТР DT-501 ЦИФРОВОЙ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("A&D Company Ltd"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("китай"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(114.00));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(127.50));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(127.50));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(0));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("122009"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.12.2011"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС JP.ИМ04.В07230"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
		}

		[Test]
		public void Parse_Katren_Voronezh3()
		{
			var doc = WaybillParser.Parse("3955563_Катрен(124244).txt");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("124244"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("14853"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ГАЛИДОР 0,025/МЛ 2МЛ N10 АМП Р-Р В/В В/М"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Эгис Фармацевтический завод ОАО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("венгрия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(243.49));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(236.10));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(214.64));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("T131A0909"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.09.2012"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС HU.ФМ08.Д91304"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(47.22));
		}

		[Test]
		public void Parse_Rosta_Msk()
		{
			var doc = WaybillParser.Parse("3951059_Роста(326355).TXT");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("326355.0"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.06.10")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("94193"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Адвантан мазь 0,1% туба 50г"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Intendis Manufacturing"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Италия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(656.26));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(746.81));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(678.92));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("92205B"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("03.06.12"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС IT ФМ01 Д39735 ОС ФГУ \"ЦЭККМП\" Росздравнадзор  г.Москва"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void Parse_Tredifarm_Belgorod()
		{
			var doc = WaybillParser.Parse("3956532_Трэдифарм(00090384).txt");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("РНТ-000000090384"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("17.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("00004238"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Анальгин 0,5г табл №10 - Асфарма"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Асфарма ООО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(50));
			Assert.That(doc.Lines[0].ProducerCost, Is.Null);
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(3.31));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(3.01));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("410410"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.12.2013"));
			Assert.That(doc.Lines[0].Certificates, Is.Null);
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(15.05));
		}

		[Test]
		public void Parse_Sia_Orel()
		{
			var doc = WaybillParser.Parse("3950126_Сиа Интернейшнл(Р-1073256).TXT");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-1073256"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("15.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("6548"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АЛФЛУТОП 1МЛ Р-Р Д/ИН. АМП. Х10 Б"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Биотехнос С.А."));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Румыния"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(990.1));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(1045.58));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(950.53));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("3230310"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.02.2013"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("3230310^РОСС RO.ФМ08.Д06148"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
		}
	}
}
