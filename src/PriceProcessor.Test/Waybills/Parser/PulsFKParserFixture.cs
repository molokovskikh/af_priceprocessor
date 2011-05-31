using System;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

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
			Assert.That(line.ProducerCost, Is.EqualTo(111.16));
			Assert.That(line.SerialNumber, Is.EqualTo("010310"));
			Assert.That(line.RegistryCost, Is.EqualTo(122.11));
			Assert.That(line.SupplierPriceMarkup, Is.Null);
			Assert.That(line.VitallyImportant, Is.True);
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
			Assert.That(line.ProducerCost, Is.EqualTo(266.09));
			Assert.That(line.SerialNumber, Is.EqualTo("N68061"));
			Assert.That(line.RegistryCost, Is.EqualTo(311.22));
			Assert.That(line.SupplierPriceMarkup, Is.Null);
			Assert.That(line.VitallyImportant, Is.True);
		}

		[Test]
		public void Parse_zdrav_service()
		{
			var doc = WaybillParser.Parse("1689520.DBF", new DocumentReceiveLog {Supplier = new Supplier {Id = 1581}});
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Бумага туалетная Зева Плюс 2-х сл. Ромашка (144065) N4"));

			doc = WaybillParser.Parse("1843615.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(10));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("1843615"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.12.2010"));
			line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("120190"));
			Assert.That(line.ProducerCost, Is.EqualTo(93.90));
			Assert.That(line.SupplierCost, Is.EqualTo(103.83));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(94.39));
			//Assert.That(line.NdsAmount, Is.EqualTo(9.44));
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
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1843615.dbf")));
			Assert.IsTrue(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1689520.dbf")));
			Assert.IsTrue(PulsFKParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\3901847_Роста(300882R).dbf")));
		}
	}
}
