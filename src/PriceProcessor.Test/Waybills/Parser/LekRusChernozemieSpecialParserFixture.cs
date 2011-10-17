using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	class LekRusChernozemieSpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 182u } }; // Лекрус Центральное Черноземье
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\00039401.dbf", documentLog) is LekRusChernozemieSpecialParser);

			var document = WaybillParser.Parse("00039401.dbf", documentLog);

			Assert.That(document.Lines.Count, Is.EqualTo(4));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00039401"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("13.09.2011"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("00039401"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("13.09.2011"));
			Assert.That(invoice.ConsigneeInfo, Is.EqualTo("Аптека № 59, Липецк, п.Дачный,ул.Писарева,10а"));
			Assert.That(invoice.ShipperInfo, Is.EqualTo("394040, Воронеж, пгт Придонской, Мазлумова, дом № 25А, корпус а"));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(865.53));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(0.00));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(86.55));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(0.00));
			Assert.That(invoice.Amount10, Is.EqualTo(952.08));
			Assert.That(invoice.Amount18, Is.EqualTo(0.00));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(865.53));
			Assert.That(invoice.Amount, Is.EqualTo(952.08));
			Assert.That(invoice.NDSAmount, Is.EqualTo(86.55));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("182644"));
			Assert.That(line.Product, Is.EqualTo("Бромгексин, табл., 8 мг, №10, , 1 200"));
			Assert.That(line.Producer, Is.EqualTo("Ирбитский ХФЗ"));
			Assert.That(line.Country, Is.Null);
			Assert.That(document.Lines[2].Country, Is.EqualTo("РОССИЯ"));
			Assert.That(document.Lines[1].BillOfEntryNumber, Is.EqualTo("10125020/280710/0007"));
			Assert.That(line.Quantity, Is.EqualTo(50));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(1.73));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(2.00));
			Assert.That(line.SupplierCost, Is.EqualTo(2.20));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.NdsAmount, Is.EqualTo(10.00));
			Assert.That(line.Amount, Is.EqualTo(110.00));
			Assert.That(line.SerialNumber, Is.EqualTo("50211"));
			Assert.That(line.Period, Is.EqualTo("01.03.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д97380"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.Null);			
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(15.61));
		}
	}
}
