using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KatrenVrnMusatovParserFixture
	{
		[Test]
		public void OldParse()
		{
			var doc = WaybillParser.Parse("203176.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(6));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("203176"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("16881"));
			Assert.That(line.Product, Is.EqualTo("ДИОКСИДИНА 5% 30,0 МАЗЬ"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ01.Д51084"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("Биосинтез ОАО"));
			Assert.That(line.Period, Is.EqualTo("01.08.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("30710"));
			Assert.That(line.SupplierCost, Is.EqualTo(83.38));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(75.8));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(75.80));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			var line1 = doc.Lines[1];
			Assert.That(line1.ProducerCostWithoutNDS, Is.EqualTo(116.83));
			Assert.That(line1.SupplierCostWithoutNDS, Is.EqualTo(108.7));
			Assert.That(line1.Nds, Is.EqualTo(10));
		}

		[Test]
		public void Parse_produser_cost_without_nds()
		{
			var doc = WaybillParser.Parse("6098189_Катрен_1209_.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(95));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("6098189_Катрен_1209_"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("6439408"));
			Assert.That(line.Product, Is.EqualTo("ТЕРМОМЕТР ЭЛЕКТРОННЫЙ AMDT-10 (УДАРОСТОЙК КОРПУС)"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo("РОСС US.ИМ04.В06948"));
			Assert.That(line.Country, Is.EqualTo("США"));
			Assert.That(line.Producer, Is.EqualTo("Амрус Энтерпрайзис ЛТД"));
			Assert.That(line.Period, Is.EqualTo("01.11.2015"));
			Assert.That(line.SerialNumber, Is.EqualTo("112010"));
			Assert.That(line.SupplierCost, Is.EqualTo(71.51));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(71.51));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(71.51));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			var line1 = doc.Lines[1];
			Assert.That(line1.ProducerCostWithoutNDS, Is.EqualTo(33.90));
			Assert.That(line1.SupplierCostWithoutNDS, Is.EqualTo(33.90));
			Assert.That(line1.Nds, Is.EqualTo(10));
		}

		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("50232.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(14));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("02.03.2011"));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("50232"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("474432"));
			Assert.That(line.Product, Is.EqualTo("АЦИКЛОВИР 5% 5,0 МАЗЬ"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ03.Д91772"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("Вертекс ЗАО"));
			Assert.That(line.Period, Is.EqualTo("01.09.2012"));
			Assert.That(line.SerialNumber, Is.EqualTo("210810"));
			Assert.That(line.SupplierCost, Is.EqualTo(14.19));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(12.90));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(14.04));
			Assert.That(line.RegistryCost, Is.EqualTo(15.16));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(line.Amount, Is.EqualTo(70.95));
			Assert.That(line.NdsAmount, Is.EqualTo(6.45));
			var line1 = doc.Lines[1];
			Assert.That(line1.ProducerCostWithoutNDS, Is.EqualTo(157.90));
			Assert.That(line1.SupplierCostWithoutNDS, Is.EqualTo(157.90));
			Assert.That(line1.Nds, Is.EqualTo(10));
			Assert.That(line1.Amount, Is.EqualTo(868.45));
			Assert.That(line1.NdsAmount, Is.EqualTo(78.95));

			//Дополнительная проверка к задаче
			//http://redmine.analit.net/issues/28639
			var document = WaybillParser.Parse("407734.dbf");
		}

		//http://redmine.analit.net/issues/47625
		[Test]
		public void ParseCertificatesDate()
		{
			var doc = WaybillParser.Parse("111044-06.dbf");
			var line = doc.Lines[0];
			Assert.That(line.CertificatesDate, Is.EqualTo("11.12.2015"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(KatrenVrnMusatovParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\50232.dbf")));
		}

		[Test]
		public void ParseWithEan13()
		{
			/*
			 * http://redmine.analit.net/issues/55907
			 */
			var doc = WaybillParser.Parse("589339-06.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("589339-06"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("3372697"));
			Assert.That(line.Product, Is.EqualTo("АБАКТАЛ 0,4 N10 ТАБЛ П/О"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo("РОСС SI.ФМ08.Д19098"));
			Assert.That(line.Country, Is.EqualTo("Словения"));
			Assert.That(line.Producer, Is.EqualTo("Лек Д.Д."));
			Assert.That(line.Period, Is.EqualTo("01.12.2018"));
			Assert.That(line.SerialNumber, Is.EqualTo("GA5633"));
			Assert.That(line.SupplierCost, Is.EqualTo(209.99));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(196.65));
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-2.92));
			Assert.That(line.EAN13, Is.EqualTo(3838957492800));
		}

		[Test]
		public void ParseWithInvoiceSum()
		{
			/*
			 * http://redmine.analit.net/issues/56429
			 */
			var doc = WaybillParser.Parse("620980-06.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("620980-06"));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("4857"));
			Assert.That(line.Product, Is.EqualTo("АНАФЕРОН N20 ТАБЛ Д/РАССАС"));
			Assert.That(line.Quantity, Is.EqualTo(8));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д37754"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("Материа Медика Холдинг НПФ ООО"));
			Assert.That(line.Period, Is.EqualTo("01.05.2019"));
			Assert.That(line.SerialNumber, Is.EqualTo("4930516"));
			Assert.That(line.SupplierCost, Is.EqualTo(171.93));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(161.6));
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-3.28));
			Assert.That(line.EAN13, Is.EqualTo(4607009582245));

			var invoice = doc.Invoice;
			Assert.That(invoice.Amount, Is.EqualTo(28545.82));



		}

	}
}