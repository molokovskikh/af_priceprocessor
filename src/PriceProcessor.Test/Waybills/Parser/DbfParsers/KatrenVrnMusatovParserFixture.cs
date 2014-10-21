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

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(KatrenVrnMusatovParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\50232.dbf")));
		}
	}
}