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
		public void Parse_Tredifarm()
		{
			var doc = WaybillParser.Parse("00086534.txt");

			Assert.That(doc.Lines.Count, Is.EqualTo(14));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("РНТ-000000086534"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("10.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("00000315"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Ацилакт свечи №10 - Ланафарм ООО"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Ланафарм ООО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].ProducerCost, Is.Null);
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(24.06));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(21.87));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("260310"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.04.2011"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС 002683"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(4.81));
		}
	}
}
