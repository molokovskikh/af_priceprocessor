using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.SstParsers;
using NUnit.Framework;
using System;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class UkonParserFixture
	{
		[Test, Description("Накладная с Ценой поставщика с НДС равной 0.")]
		public void Parse_With_Zero_SupplierCost()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\7455319.sst");

			Assert.That(doc.Lines.Count, Is.EqualTo(3));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("СМ-7455319/00"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("23.03.2011")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("2753"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Супрастин табл. 25мг N20 Венгрия"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Egis Pharmaceuticals Plc"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Венгрия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(30));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(91.10));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(91.10));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(93.73));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(0));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(93.89));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС.HU.ФМ08.Д98806"));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(0.00));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(2733.00));

			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.08.2015"));
			//Assert.That(doc.Lines[0].ProductId, Is.Null);
			Assert.That(doc.Lines[0].ProductEntity, Is.Null);
			Assert.That(doc.Lines[0].ProducerId, Is.Null);
		}

		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\0004076.sst");
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("0000004076"));

			Assert.That(doc.Lines[0].Product, Is.EqualTo("Солодкового корня сироп фл.100 г"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("201109^РОСС RU.ФМ05.Д11132^01.12.11201109^74-2347154^25.11.09 ГУЗ ОЦСККЛ г. Челябинск"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.12.11"));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("201109"));

			Assert.That(doc.Lines[1].Product, Is.EqualTo("Эвкалипта настойка фл.25 мл"));
			Assert.That(doc.Lines[1].Certificates, Is.EqualTo("151209^РОСС ФМ05.Д36360^01.12.14151209^74-2370989^18.01.10 ГУЗ ОЦСККЛ г. Челябинск"));
			Assert.That(doc.Lines[1].Period, Is.EqualTo("01.12.14"));
			Assert.That(doc.Lines[1].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("08.02.10")));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
		}

		[Test]
		public void Parse_without_supplier_cost_without_nds()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\8521183.sst");
		}

		[Test]
		public void Parse_with_zero_supplier_cost_without_nds()
		{
			var doc = WaybillParser.Parse(@"9907125-002.sst");

			Assert.That(doc.Lines.Count, Is.EqualTo(1));

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("9907125-002"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("1660"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ДЕСИТИН МАЗЬ 57Г"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Pfizer"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("США"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(84.78));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(0));
			Assert.That(doc.Lines[0].Nds, Is.Null);
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("2408013"));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(8.01));
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("2408013^74-2197593^20.03.2009 ЦККЛС в г.Челябинск2408013^POCC US.ФM01.Д26788^15.12.2008 ФГУ ЦЭККМП Росздравнадзор"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.12.2010"));
		}

		[Test]
		public void Parse_ForaFarmLogic_Msk()
		{
			var doc = WaybillParser.Parse(@"362677.sst");
			Assert.That(doc.Lines.Count, Is.EqualTo(1));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("362677"));

			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("09.06.2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(1));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("11152"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Эротика Делюкс №3 (60) презервативы"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Guilin Guibiao"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Китай"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(6.56));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(5.1900));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(5.9600));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("ZE62009"));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(14.84));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("2006/2822"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.06.2014"));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
		}

		[Test]
		public void ParseWithMultilineComments()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3645763_ОАС(114504).sst");
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("114504"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("30.03.2010")));

			Assert.That(doc.Lines[0].Product, Is.EqualTo("Трамадол р-р д/и 50мг/мл 2мл амп N5x1 МЭЗ РОС"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("461009^РОСС RU.ФМ01.Д91475^01.03.2010 ФГУ \"ЦЭККМП\" Росздравнадзор^01.11.2012"));
			Assert.That(doc.Lines[0].Period, Is.Null);
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));

			Assert.That(doc.Lines[1].Product, Is.EqualTo("Трамадол р-р д/и 50мг/мл 2мл амп N5x1 МЭЗ РОС"));
			Assert.That(doc.Lines[1].Certificates, Is.EqualTo("461009^РОСС RU.ФМ01.Д91475^01.03.2010 ФГУ \"ЦЭККМП\" Росздравнадзор^01.11.2012"));
			Assert.That(doc.Lines[1].Period, Is.Null);
			Assert.That(doc.Lines[1].Nds.Value, Is.EqualTo(10));
		}


		[Test, Description("В заголовке отсутствует строка '- В следующей строке перечислены:'")]
		public void Parse_without_headerLine()
		{
			var parser = new UkonParser();
			var doc = new Document();

			//не парсится, так как в заголовке отсутствует строка "- В следующей строке перечислены:"
			var resultDoc = parser.Parse(@"..\..\Data\Waybills\00019418.sst", doc);
			Assert.That(resultDoc, Is.Null);
		}

		[Test]
		public void Parse_if_supplier_cost_null()
		{
			var parser = new UkonParser();
			var doc = new Document();

			var resultDoc = parser.Parse(@"..\..\Data\Waybills\593053.sst", doc);
		}

		[Test]
		public void Parse_without_header()
		{
			var parser = new UkonParser();
			var doc = new Document();

			try {
				parser.Parse(@"..\..\Data\Waybills\without_header.sst", doc);
				Assert.Fail("Должны были выбросить исключение");
			}
			catch (Exception e) {
				Assert.That(e.Message, Is.StringContaining("Не найден заголовок накладной"));
			}
		}

		[Test]
		public void Parse_only_comments()
		{
			var parser = new UkonParser();
			var doc = new Document();

			try {
				parser.Parse(@"..\..\Data\Waybills\only_comments.sst", doc);
				Assert.Fail("Должны были выбросить исключение");
			}
			catch (Exception e) {
				Assert.That(e.Message, Is.StringContaining("Не найден заголовок накладной"));
			}
		}

		[Test]
		public void Parse_without_body()
		{
			var parser = new UkonParser();
			var doc = new Document();
			try {
				parser.Parse(@"..\..\Data\Waybills\without_body.sst", doc);
				Assert.Fail("Должны были выбросить исключение");
			}
			catch (Exception e) {
				Assert.That(e.Message, Is.StringContaining("Не найдено тело накладной"));
			}
		}

		[Test]
		public void Parse_with_zhnvls_column()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\8825045-001.sst");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("8825045-001"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("01.04.2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(20));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("15918"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АДЖИСЕПТ ПАСТИЛКИ №24 ЭВКАЛИПТ И МЕНТОЛ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("AGIO"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Индия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(22.22));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(16.82));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(20.20));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("10/14/9024"));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(20.10));

			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[8].VitallyImportant, Is.True);

			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[8].RegistryCost, Is.EqualTo(164.7));
		}

		[Test]
		public void Parse_without_SupplierPriceMarkup()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3687861_БИОТЭК (Екатеринбург-Фарм)(3687558_БИОТЭК (Екатеринбург-Фарм)(064197)).sst");

			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("12.04.10")));
			Assert.That(doc.Lines.Count, Is.EqualTo(1));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Лейкопластырь 5см х 500см Унипласт фиксирующий эластичный шт №1"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Верофарм"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("РОССИЯ"));
		}

		[Test]
		public void Parse_Protek()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3700197_Протек-21(9041050-001).sst");

			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("9041050-001"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.04.2010")));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("13881"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ГЛИБОМЕТ ТАБ. П/О 2,5МГ/400МГ №40"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Berlin-Chemie"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Германия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(191.29));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(166.86));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(173.90));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(4.22));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("92588^74-2391651^09.02.2010 ЦККЛС в г.Челябинск92588^POCC DE.ФM01.Д52733^27.01.2010 ФГУ ЦЭККМП Росздравнадзор"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("92588"));
		}

		[Test]
		public void Parse_with_empty_strings_at_the_end()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3699446_Катрен(046726).sst");

			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("46726"));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("3015207"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АСПИКОР 0,1 N30 ТАБЛ П/О"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Вертекс ЗАО."));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.03.2012"));
		}

		[Test]
		public void Parse_with_commas()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\13111-М - 16.04.2010.sst");

			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(22.64));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(18.14));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(20.58));
		}

		[Test]
		public void Parse_AlianceHelskeaRus()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\6456625.sst");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("СМ-6456625/00"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("01.06.2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(4));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("30093"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Али капс капс. 0.49г N8 Россия"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("ВИС ООО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(456.19));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(379.02));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(386.60));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(18));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ09.Д03270"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("0310"));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));

			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0.00));
		}

		[Test]
		public void Parse_with_float_quantity()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\103923.sst");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("103923"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("31.05.2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(4));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("13480840"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ТЕРМОМЕТР ЭЛЕКТРОННЫЙ AMDT-11 С МЯГ НАКОНЕЧНИКОМ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Амрус Энтерпрайзис ЛТД"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("сша"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС US.ИМ04.В06948"));

			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(93.94));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(86));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(93.94));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(0));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("032010"));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));

			Assert.That(doc.Lines[2].SupplierCost, Is.EqualTo(30.14));
			Assert.That(doc.Lines[2].SupplierCostWithoutNDS, Is.EqualTo(27.40));
			Assert.That(doc.Lines[2].Nds, Is.EqualTo(10));

			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0.00));
		}

		[Test]
		public void Parse_with_uncommented_rows_after_header()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\163909.sst");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Рн-КЛ00000163909"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("31.05.2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(3));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("44925"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Подгузники.д/взр  СУПЕР СЕНИ под.д/взр  М сред 30шт"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Торунский з-д Польша"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Польша"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(537.87));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(443.19));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(488.97));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(45.78));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("094-МЕ30-А01"));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0.00));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("РОСС PL ИМ09 В 02246"));
		}

		//странный файл приводил к зацыкливанию
		[Test]
		public void Read_broken_file()
		{
			try {
				var doc = WaybillParser.Parse("122447_11215092-001.sst");
			}
			catch (Exception) {
			}
		}

		[Test]
		public void Parse_with_one_body_comment_line()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\_9591.sst");
			var doc1 = WaybillParser.Parse(@"..\..\Data\Waybills\_997.sst");

			//накладная без заголовка. не парсится.
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\00019418.sst");
		}

		// http://redmine.analit.net/issues/59617
		[Test]
		public void Parse_59617()
		{
			var doc = WaybillParser.Parse("72490703_001.sst");
			Assert.That(doc.Lines.Count, Is.EqualTo(43));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("72490703-001"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("27.01.2017")));
			Assert.That(doc.Invoice.Amount, Is.EqualTo(67812.02m));
			//Тип поставки
			Assert.That(doc.Invoice.NDSAmount10, Is.EqualTo(5446.25m));
			Assert.That(doc.Invoice.NDSAmount18, Is.EqualTo(1205.62m));
			//Тип валюты
			//Курс валюты
			Assert.That(doc.Invoice.RecipientId, Is.EqualTo(167039));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("33566"));
			Assert.That(line.Product, Is.EqualTo("А-ЦЕРУМЕН СР-ВО МНОГОФУНКЦ. ОТОЛАРИНГОЛОГ. Д/ПРОМЫВ. УШНОГО ПРОХОДА ФЛ-КАП. 2МЛ №5"));
			Assert.That(line.Producer, Is.EqualTo("Laboratoires Gilbert"));
			Assert.That(line.Country, Is.EqualTo("ФРАНЦИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(2.00m));
			Assert.That(line.SupplierCost, Is.EqualTo(269.19m));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(239.92m));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(244.72m));
			//Цена поставщика с НДС
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(2.00m));
			Assert.That(line.ExpireInMonths, Is.Null);
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130110/130716/0001976/2"));
			Assert.That(line.Certificates, Is.EqualTo("182735^POCC FR.AГ58.Д00025^25.10.2011,ФГУ \"НИИ ФХМБА\" России  182735^POCC FR.AГ58.H00184^13.12.2013,РОСС RU.0001.11АГ58  182735^№ ФCЗ 2011/10222^19.07.2011,ФСН в СЗиСР"));
			Assert.That(line.SerialNumber, Is.EqualTo("182735"));
			Assert.That(line.DateOfManufacture, Is.Null);
			Assert.That(line.Period, Is.EqualTo("01.05.2019"));
			Assert.That(line.EAN13, Is.EqualTo("3518646057342"));
			Assert.That(line.RegistryDate, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
			//Торговая наценка организации-импортера
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
		}

		[Test]
		public void Parse_60333()
		{
			var doc = WaybillParser.Parse("96599.sst");
			var line = doc.Lines[0];
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.EqualTo("4603933004785"));
		}
	}
}