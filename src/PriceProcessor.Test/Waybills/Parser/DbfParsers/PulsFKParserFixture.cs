using System;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class PulsFKParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("3905255_ПУЛЬС ФК(00204995).dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00204995"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("03/06/2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(6));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("11638"));
			Assert.That(line.Product, Is.EqualTo("Бетагистин табл. 8 мг х30"));
			Assert.That(line.Producer, Is.EqualTo("Канонфарма продакшн"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.04.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФM08.Д84457"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(102.00));
			Assert.That(line.SupplierCost, Is.EqualTo(112.20));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(111.16));
			Assert.That(line.SerialNumber, Is.EqualTo("010310"));
			Assert.That(line.RegistryCost, Is.EqualTo(122.11));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-8.24));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.EAN13, Is.Null);
		}

		[Test]
		public void Parse_Rosta_Msk()
		{
			var doc = WaybillParser.Parse("3901847_Роста(300882R).DBF");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("300882"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("03/06/2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("80293"));
			Assert.That(line.Product, Is.EqualTo("Аторис таб. п/о 10 мг х 30"));
			Assert.That(line.Producer, Is.EqualTo("KRKA -Словения"));
			Assert.That(line.Country, Is.EqualTo("Словения"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.11.2011"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС SI ФМ08 Д65108"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(244.65));
			Assert.That(line.SupplierCost, Is.EqualTo(269.12));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(266.09));
			Assert.That(line.SerialNumber, Is.EqualTo("N68061"));
			Assert.That(line.RegistryCost, Is.EqualTo(311.22));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-8.06));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.EAN13, Is.EqualTo("3838989596446"));
		}

		[Test]
		public void Parse_zdrav_service()
		{
			var doc = WaybillParser.Parse("1689520.DBF", new DocumentReceiveLog { Supplier = new Supplier { Id = 1581 } });
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Бумага туалетная Зева Плюс 2-х сл. Ромашка (144065) N4"));

			doc = WaybillParser.Parse("1843615.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(10));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("1843615"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.12.2010"));
			line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("120190"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(93.90));
			Assert.That(line.SupplierCost, Is.EqualTo(103.83));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(94.39));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SerialNumber, Is.EqualTo("G0A043A"));
			Assert.That(line.Period, Is.EqualTo("01.10.2012"));
			Assert.That(line.Product, Is.EqualTo("Ауробин мазь 20г"));
			Assert.That(line.Country, Is.EqualTo("ВЕН"));
			Assert.That(line.Producer, Is.EqualTo("Гедеон Рихтер"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.EqualTo(null));
			Assert.That(line.Certificates, Is.EqualTo("POCC HU.ФM01.Д45323"));
			Assert.That(line.Amount, Is.EqualTo(207.66));
			Assert.That(line.NdsAmount, Is.EqualTo(18.88));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(14.00));
			Assert.That(line.EAN13, Is.EqualTo("5997001393871"));
		}

		[Test]
		public void Parse_A_and_D_rus()
		{
			var doc = WaybillParser.Parse("Apteka2000_invoice.DBF");
			Assert.That(doc.Lines.Count, Is.EqualTo(14));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("РНАА061-00001"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("01.10.2006"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("Т00332"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(77.00000));
			Assert.That(line.SupplierCost, Is.EqualTo(77.00000));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(77.00000));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Period, Is.Null);
			Assert.That(line.Product, Is.EqualTo("Термометр DT-501 A&D электронный"));
			Assert.That(line.Country, Is.EqualTo("ЯПО"));
			Assert.That(line.Producer, Is.EqualTo("A&D"));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("POCC JP.ИM04.B05579"));
			Assert.That(line.Amount, Is.EqualTo(154.00000));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(line.EAN13, Is.EqualTo("4606339000818"));
		}

		[Test]
		public void Parse_FarmPartnerKaluga()
		{
			var doc = WaybillParser.Parse("13093.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(8));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("013093"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("14.07.2011"));
			var line = doc.Lines[2];
			Assert.That(line.Code, Is.EqualTo("127984"));
			Assert.That(line.Product, Is.EqualTo("ПРОКЛАД УРОЛОГИЧ СЕНИ-ЛЕДИ ПЛЮС AIR N15"));
			Assert.That(line.Producer, Is.EqualTo("ТЗМО"));
			Assert.That(line.Country, Is.EqualTo("ПОЛЬША"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(136.94));
			Assert.That(line.SupplierCost, Is.EqualTo(150.63));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(136.94));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.04.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС PL ИМ09 В02637"));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.SerialNumber, Is.EqualTo(".."));
			Assert.That(line.Amount, Is.EqualTo(301.26));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(line.EAN13, Is.EqualTo("0"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10124030/270905/0009093/2"));
		}

		[Test]
		public void Parse_GrandCapital_7752()
		{
			var doc = WaybillParser.Parse("002339_02989.dbf");
			var line = doc.Lines[1];
			Assert.That(line.Code, Is.EqualTo("2-000163"));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("16-1-02989"));
		}

		[Test]
		public void Uralbiofarm_dop_fields()
		{
			var doc = WaybillParser.Parse("16810 (1).dbf");
			var line = doc.Lines[0];
			var invoice = doc.Invoice;
			Assert.That(line.ProducerCost, Is.EqualTo(12.25));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(11.14));
			Assert.That(line.Amount, Is.EqualTo(1237.81));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("429330, Чувашская Республика - Чувашия, Канаш г, Кооперативн"));
		}

		[Test(Description = "Проверка перечисленных накладных на соответствие парсеру PulsFKParser")]
		public void Check_file_format()
		{
			Assert.IsTrue(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1843615.dbf")));
			Assert.IsTrue(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1689520.dbf")));
			Assert.IsTrue(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\3901847_Роста(300882R).dbf")));
			Assert.IsTrue(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\13093.dbf")));
			Assert.IsTrue(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\16810 (1).dbf")));
			Assert.IsFalse(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\5066_5014C.dbf")),
				"Накладная 'Волгофарм' по-прежнему удовлетворяет парсеру PulsFKParser");
		}

		/// <summary>
		/// Для задачи http://redmine.analit.net/issues/38523
		/// </summary>
		[Test]
		public void Parse2()
		{
			var doc = WaybillParser.Parse("633304.dbf");
			var invoice = doc.Invoice;
			Assert.That(invoice.Amount, Is.EqualTo(58279.01));
		}
	}
}