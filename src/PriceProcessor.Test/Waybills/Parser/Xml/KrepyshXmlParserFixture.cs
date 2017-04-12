using System;
using Common.Tools;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	internal class KrepyshXmlParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\20101119_8055_250829.xml");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("8055"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("19.11.2010")));
			Assert.That(document.Lines.Count, Is.EqualTo(14));
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("5443"));
			Assert.That(line.Product, Is.EqualTo("Прокладки \"Котекс\" Део ежедневные 1 пач/20шт"));
			Assert.That(line.Producer, Is.EqualTo(" \"Yuhan-Kimberly Ltd\", Республика Корея"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SupplierCost, Is.EqualTo(28.57));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(25.97));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС CN.АВ57.В05475"));
			Assert.AreEqual("09.08.2010", line.CertificatesEndDate.Value.ToString("d"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(null));
			Assert.That(line.Period, Is.EqualTo(null));
			Assert.That(line.CertificatesDate, Is.EqualTo("10.08.2009"));
		}

		[Test]
		public void ParseFix()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\125968.xml");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-Иж0000125968"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("08.12.2010")));
			Assert.That(document.Lines.Count, Is.EqualTo(4));
			Assert.That(document.Lines[0].Code, Is.EqualTo(null));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Альфарона 50тысМЕ пор д/пр наз р-ра №1"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Фармаклон НПП ООО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("РОССИЯ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(106.91818));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(6.92));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(100));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.11.2011"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ08.Д96848 "));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("03.12.2009"));
		}

		[Test]
		public void Parse_without_producer_cost_and_nds_with_symbol_percent()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\8817928.xml");
			document = WaybillParser.Parse(@"..\..\Data\Waybills\8817930.xml");
			document = WaybillParser.Parse(@"..\..\Data\Waybills\8817942.xml");
		}

		[Test]
		public void Parse_with_invalid_data()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\20101119_8055_250829_1.xml");
			Assert.That(document.Lines[0].Nds, Is.Null);
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void ParseSiaInternational()
		{
			var document = WaybillParser.Parse(@"64_10413501_Р-2047471.xml");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-2047471"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("18.07.2013")));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Алмагель А Сусп. 170мл Фл. Б М"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Балканфарма-Троян АД, БОЛГАРИЯ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(12));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(88.3500));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10.0000));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(106.02));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(1166.2200));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("3800009121020"));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo("10130032/120413/0002530/1"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("020313"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС BG.ФМ08.Д61596"));
			Assert.That(document.Lines[0].CertificateAuthority, Is.EqualTo(@"РОСС.RU.0001.11ФМ08 ООО " + '\u0022' + "Окружной центр контроля качества" + '\u0022' + ", г. Москва"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("07.05.2013"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.03.2015"));
		}

		/// <summary>
		/// Для задачи http://redmine.analit.net/issues/36853
		/// </summary>
		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\00003044799_36_1.xml");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00003044799/36"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("23.07.2015")));
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("2341"));
			Assert.That(line.Product, Is.EqualTo("Пояс послеоперационн  разм 2 Латвия"));
			Assert.That(line.Producer, Is.EqualTo("Tonus Elast ООО"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo("РОСС LV.ИМ32.Д00281"));
			Assert.That(line.Period, Is.EqualTo("01.02.2019"));
			Assert.That(line.CertificatesDate, Is.EqualTo("31.07.2012"));
			Assert.That(line.EAN13, Is.EqualTo(4750283035089));
		}
	}
}