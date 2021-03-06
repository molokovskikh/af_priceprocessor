﻿using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class SiaParserFixture : DocumentFixture
	{
		[Test]
		public void Parse_with_incorrect_reg_date()
		{
			var document = WaybillParser.Parse(@"8472653.dbf");
			Assert.AreEqual("  .  .", document.Lines[0].CertificatesDate);
		}

		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\1016416.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1016416"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Пентамин 5% Р-р д/ин. 1мл Амп. Х10 Б"));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(171.78));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(156.16));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.06.2013"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ08.Д95450"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("05.06.2009"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("70508"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("12/02/2010")));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(15.62));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(8));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(1374.21));
		}

		[Test]
		public void Parse_with_character_type()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\1016416_char.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(3));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Г000006147"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Андипал таб. № 10"));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(5.18));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.07.2012"));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ10.Д50325"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("18.01.2010"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(4.71));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("24.03.10")));
		}

		[Test]
		public void Parse_with_vitally_important()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\8916.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("R7036"));
			Assert.That(doc.Lines[4].VitallyImportant, Is.True);
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("01/02/2008")));
			Assert.That(doc.Lines[5].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo(null));
			Assert.That(doc.Lines[4].CertificatesDate, Is.EqualTo("29.08.2007"));
		}

		[Test]
		public void Parse_with_registry_cost_in_reestr_field()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\8916_REESTR.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.Lines[4].RegistryCost, Is.EqualTo(82.0615));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[2].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ЗАМЕНИТЕЛЬ САХАРА\"РИО ГОЛД\" N1200 ТАБЛ"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(SiaParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsTrue(SiaParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(SiaParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsTrue(SiaParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsTrue(SiaParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
		}

		[Test]
		public void Parse_without_document_date()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\without_date.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.DocumentDate.HasValue, Is.False);
			Assert.That(document.DocumentDate, Is.Null);
		}

		[Test]
		public void Parse_without_registry_cost()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3655268_Катрен(K_59329).dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.Lines[0].RegistryCost, Is.Null);
		}

		[Test]
		public void Parse_with_null_period()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3677177_0_3677175_0_3676850_Сиа Интернейшнл(1064837).DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(37));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.10.2011"));
			Assert.That(document.Lines[28].Period, Is.Null);
		}

		[Test]
		public void Parse_with_column_jnvls()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3681901_УФК_3681896_УФК_3681714_УФК_t120410___.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(68));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[2].VitallyImportant, Is.True);
		}

		[Test]
		public void Parse_with_column_gzwl()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3683304_УФК_u4004036_.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(4));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[1].VitallyImportant, Is.True);
			Assert.That(document.Lines[2].VitallyImportant, Is.False);
			Assert.That(document.Lines[3].VitallyImportant, Is.False);
		}

		[Test]
		public void Parse_with_registry_cost()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3683304_УФК_u4004036_.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(4));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[1].RegistryCost, Is.EqualTo(72.55));
			Assert.That(document.Lines[2].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[3].RegistryCost, Is.EqualTo(0));
		}

		[Test]
		public void Parse_without_producer_cost()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3692407_Сиа_Интернейшнл_1068481_.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(20));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1068481"));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(121.28));
			Assert.That(document.Lines[2].ProducerCostWithoutNDS, Is.Null);
		}

		[Test]
		public void Parse_ForaFarmLogic_Moscow()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3700278_ФораФарм_лоджик-Москва_37607_.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(66));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("37607"));
			Assert.That(document.Lines[0].Code, Is.EqualTo("9452"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("QS Юниор cалфетка влажная д/девочек (земляника) 10шт"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Моск. ф-ка влажных салфеток"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("РОССИЯ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(8.85));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(7.5));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].RegistryCost, Is.Null);
			Assert.That(document.Lines[0].Period, Is.EqualTo("02.12.2011"));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.АЕ51.В13746"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("1209"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("08/04/2010")));
		}

		[Test]
		public void Parse_ForaFarmLogic_Moscow1()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\5189569_ФораФарм_лоджик-Москва_506462_.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(11));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("506462"));
			Assert.That(document.Lines[0].Code, Is.EqualTo("18024"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Гематоген Народный детский 40г (БАД)"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Сибирское здоровье 2000"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("РОССИЯ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(50));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(4.61));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(3.91));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].RegistryCost, Is.Null);
			Assert.That(document.Lines[0].Period, Is.EqualTo("04.05.2011"));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.АИ42.Д01672"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("0910"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("09.11.2010")));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(35.16));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(230.5));
		}

		[Test]
		public void Parse_Ufk()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3731509_УФК_u4004584_.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(9));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("М000004584"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("22.04.10")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("10002297"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Боро Фреш крем 25,0 (роза)"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Аюшакти Аюрвед ПВТЛТ"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(16.56));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(19.93));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("2009"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС IN АЕ45 В50935"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.05.2014"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(16.89));
		}

		[Test]
		public void Parse_Sia_Orel()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\Р-1043585.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(20));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-1043585"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("23.04.2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("17709"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Аква Марис спрей назальный /морская вода/ 30мл Б"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Jadran Co."));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Хорватия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.Null);
			Assert.That(document.Lines[2].ProducerCostWithoutNDS, Is.EqualTo(19.0900));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(123.7700));
			Assert.That(document.Lines[0].SerialNumber, Is.Null);
			Assert.That(document.Lines[2].SerialNumber, Is.EqualTo("81109"));
			Assert.That(document.Lines[0].Certificates, Is.Null);
			Assert.That(document.Lines[2].Certificates, Is.EqualTo("РОСС RU.ФМ05.Д16560"));
			Assert.That(document.Lines[0].Period, Is.Null);
			Assert.That(document.Lines[2].Period, Is.EqualTo("01.12.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(112.52));
			Assert.That(document.Lines[2].SupplierCostWithoutNDS, Is.EqualTo(19.9));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
			Assert.That(document.Lines[2].SupplierPriceMarkup, Is.EqualTo(14.6674));
		}

		[Test]
		public void Parse_Sia_Int_Kasan()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\5305689_СИА_Интернейшнл-Казань_Р-567197_.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(13));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-567197"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("17.11.2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("26143"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Амигренин 100мг таб.п/о Х2"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Верофарм ЗАО (sc)"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(221.36));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(243.5));
			Assert.That(document.Lines[1].SupplierCost, Is.EqualTo(134.39));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("10210"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д79451"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.03.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(false));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(221.36));
			Assert.That(document.Lines[1].SupplierCostWithoutNDS, Is.EqualTo(122.17));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
		}

		[Test]
		public void Parse_Katren()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\5586693_Катрен(232417).dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(11));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("232417"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АМБРОКСОЛ-ВЕРТЕ 0,03 N20 ТАБЛ"));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(9.46));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(8.6));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.05.2012"));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ03.Д04467"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("030410"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("10/12/2010")));
		}

		[Test]
		public void Parse_FixFile()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\78934_0.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(16));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("78934/0"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Аевит капс 0.2г N10"));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(11.22));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(10.2));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.10.2012"));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ03.Д20073"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("170910"));
			Assert.That(document.Lines[0].Code, Is.EqualTo("151"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(50));
			Assert.That(document.Lines[9].Nds, Is.EqualTo(0));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("13.12.2010")));
		}

		[Test]
		public void Parse_LekRus()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\00033418.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(8));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00033418"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("21.12.2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("76561"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Натрия хлорида раствор для инъекций 0,9%, р-р д/ин., 0,9 %, амп. 10 мл, №10, 0"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Сишуи Ксирканг Фармасьютикал Ко.Лтд/Китай"));
			Assert.IsNull(document.Lines[0].Country);
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(7));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(18.78));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(23.70));
			Assert.That(document.Lines[1].SupplierCost, Is.EqualTo(10.60));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("100703"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС CN.ФМ08 Д27703"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.07.2015"));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(true));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(18.78));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(21.55));
			Assert.That(document.Lines[1].SupplierCostWithoutNDS, Is.EqualTo(8.98));
		}

		[Test]
		public void Parse_suplier_cost_without_nds()
		{
			var doc = WaybillParser.Parse("6868203_СИА Интернейшнл-Казань(Р-646580).DBF");
			var line = doc.Lines[6];
			Assert.That(line.SupplierCost, Is.EqualTo(371.21));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(337.46));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(362.50));
		}

		[Test]
		public void Parse_Pulse_Ekaterinburg()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\n216864.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(8));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00216864"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("09.10.2012 17:52:03")));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("19636"));
			Assert.That(line.Product, Is.EqualTo("Ацикловир табл. 200 мг х20"));
			Assert.That(line.Producer, Is.EqualTo("Белмедпрепараты"));
			Assert.That(line.Country, Is.EqualTo("Беларусь"));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Amount, Is.EqualTo(115.5));
			Assert.That(line.RegistryCost, Is.EqualTo(27.4));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(9.9));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(10.5));
			Assert.That(line.NdsAmount, Is.EqualTo(10.5));
			Assert.That(line.SerialNumber, Is.EqualTo("650712"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BY.ФМ05.Д05769"));
			Assert.That(line.CertificatesDate, Is.EqualTo("08.08.2012"));
			Assert.That(line.Period, Is.EqualTo("01.08.2014"));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.IsNull(line.BillOfEntryNumber);
		}

		[Test]
		public void Parse_sia_tula()
		{
			var doc = WaybillParser.Parse("Р-1766562.DBF");
			Assert.AreEqual(13, doc.Lines.Count);
			var line = doc.Lines[0];
			Assert.AreEqual("Алька-прим шип.таб. Х10", line.Product);
			Assert.AreEqual("Польфарма Фармацевтический завод", line.Producer);
			Assert.AreEqual(2, line.Quantity);
			Assert.AreEqual(107.78, line.ProducerCostWithoutNDS);
			Assert.AreEqual(107.78, line.SupplierCost);
		}

		/// <summary>
		/// Новые данные от задачи
		/// http://redmine.analit.net/issues/28233
		/// </summary>
		[Test]
		public void Parse_lekrus()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\00013666.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(4));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00013666"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("09.10.2014")));

			var line = document.Lines[2];
			Assert.That(line.Code, Is.EqualTo("155955"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130130/070711/0013819/1"));
			Assert.That(line.EAN13, Is.EqualTo(4013054003923));
		}

		/// <summary>
		/// http://redmine.analit.net/issues/50187
		/// </summary>
		[Test]
		public void Parse_SIAInternationalKazan()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\Р-1900577.DBF");

			var line = document.Lines[0];
			Assert.That(line.CertificatesDate, Is.EqualTo("08.12.2015"));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(Convert.ToDateTime("01.08.2018")));
			Assert.That(line.CertificateAuthority, Is.EqualTo("РОСС.RU.0001.11ФМ08 ООО \"Окружной центр контроля качества\", г. Москва"));
		}

		/// <summary>
		/// К задаче http://redmine.analit.net/issues/37665
		/// </summary>
		[Test]
      public void Parse2()
      {
          var document = WaybillParser.Parse(@"..\..\Data\Waybills\Р-2606051.dbf");
          var line = document.Lines[0];
          Assert.That(line.Amount, Is.EqualTo(182.4900));
      }

      /// <summary>
      /// К задаче http://redmine.analit.net/issues/38625
      /// </summary>
      [Test]
      public void Parse3()
      {
          var document = WaybillParser.Parse(@"..\..\Data\Waybills\Р-2620497.DBF");
          Assert.That(document.Invoice.Amount, Is.EqualTo(55593.5600));
      }

		/// <summary>
		/// К задаче http://redmine.analit.net/issues/50445
		/// </summary>
		[Test]
		public void Parse_by_code()
		{
			var document = WaybillParser.Parse("269636.dbf");
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("76791885"));
			Assert.That(line.CodeCr, Is.EqualTo("13488742"));
		}

		[Test]
		public void Parse_Roton_settings()
		{
			var parser = new Inforoom.PriceProcessor.Models.Parser("PulsRyazanParser", appSupplier);
			parser.Add("NUM_DOC", "Header_ProviderDocumentId");
			parser.Add("DATE_DOC", "Header_DocumentDate");
			parser.Add("NUM_SF", "Invoice_InvoiceNumber");
			parser.Add("DATE_SF", "Invoice_InvoiceDate");
			parser.Add("ORG", "Invoice_ShipperInfo");
			parser.Add("POLUCH", "Invoice_BuyerName");
			parser.Add("CODE_TOVAR", "Code");
			parser.Add("NAME_TOVAR", "Product");
			parser.Add("PROIZ", "Producer");
			parser.Add("COUNTRY", "Country");
			parser.Add("PR_PROIZ", "ProducerCostWithoutNDS");
			parser.Add("PRICE_NDS", "SupplierCost");
			parser.Add("PRICE", "SupplierCostWithoutNDS");
			parser.Add("VOLUME", "Quantity");
			parser.Add("SUMMA", "Amount");
			parser.Add("SROK", "Period");
			parser.Add("GTD", "BillOfEntryNumber");
			parser.Add("PCT_NDS", "Nds");
			parser.Add("SUMMA_NDS", "NdsAmount");
			parser.Add("SERIA", "SerialNumber");
			parser.Add("PRICE_RR", "RegistryCost");
			parser.Add("JNVLS", "VitallyImportant");
			parser.Add("CER_NUMBER", "Certificates");
			parser.Add("SERT_ORG", "CertificateAuthority");
			parser.Add("EAN13", "EAN13");
			session.Save(parser);

			var ids = new WaybillService().ParseWaybill(new[] { CreateTestLog("40308608_Ротон(9687).DBF").Id });
			var document = session.Load<Document>(ids[0]);
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("986"));
			Assert.That(line.Amount, Is.EqualTo(801));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АЯ46Д70664"));
			Assert.That(line.Product, Is.EqualTo("Т-1442  Бандаж противогрыжевый при вентральных грыжах р. 7-ХXL (Тривес)"));
		}
	}
}