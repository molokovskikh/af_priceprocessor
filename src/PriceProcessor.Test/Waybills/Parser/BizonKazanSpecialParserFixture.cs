using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class BizonKazanSpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			// код поставщика "Бизон" (Казань)
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier{Id = 8063}, };
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\N000011720.txt", documentLog) is BizonKazanSpecialParser);

			var doc = WaybillParser.Parse("N000011720.txt", documentLog);          
			Assert.That(doc.Lines.Count, Is.EqualTo(15));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-000011720"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("30.05.2011"));

			Assert.That(doc.Lines[0].Code, Is.Null);
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Аптечка автомобильная (нового образца   )"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("*"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(115.91));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(11.59));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(127.5));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Фарм-Глобал"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.09.13"));

			Assert.That(doc.Lines[0].Country, Is.Null);
			Assert.That(doc.Lines[0].ProducerCost, Is.Null);
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(127.5));                                           
			Assert.That(doc.Lines[0].Certificates, Is.Null);
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);

			doc = WaybillParser.Parse("801_000011708.txt", documentLog);
			Assert.That(doc.Lines.Count, Is.EqualTo(9));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-000011708"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("30.05.2011"));

			Assert.That(doc.Lines[0].Code, Is.Null);
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Алмагель А (сусп.  170 мл )"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("250610"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(76.5));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(7.65));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(84.15));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Balkanpharma-Troyan AD"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.06.12"));

			Assert.That(doc.Lines[0].Country, Is.Null);
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(87.66));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(84.15));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("ФМ09.Д09423"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0.00));
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void Parse_without_period()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier{Id = 8063}, };
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\13755.txt", documentLog) is BizonKazanSpecialParser);
			var doc = WaybillParser.Parse("13755.txt", documentLog);
			Assert.That(doc.Lines.Count, Is.EqualTo(11));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-000013755"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("28.06.2011"));

			Assert.That(doc.Lines[10].Code, Is.Null);
			Assert.That(doc.Lines[10].Product, Is.EqualTo("Фурадонин (табл. 0,05 г №10 )"));
			Assert.That(doc.Lines[10].SerialNumber, Is.EqualTo("360311"));
			Assert.That(doc.Lines[10].Quantity, Is.EqualTo(20));
			Assert.That(doc.Lines[10].SupplierCostWithoutNDS, Is.EqualTo(2.72));
			Assert.That(doc.Lines[10].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[10].NdsAmount, Is.EqualTo(5.44));
			Assert.That(doc.Lines[10].Amount, Is.EqualTo(59.84));
			Assert.That(doc.Lines[10].Producer, Is.EqualTo("Борисовский "));
			Assert.That(doc.Lines[10].Period, Is.Null);

			Assert.That(doc.Lines[10].Country, Is.Null);
			Assert.That(doc.Lines[10].ProducerCost, Is.Null);            
			Assert.That(doc.Lines[10].Certificates, Is.Null);
			Assert.That(doc.Lines[10].RegistryCost, Is.Null);
			Assert.That(doc.Lines[10].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[10].SupplierPriceMarkup, Is.Null);
		}
	}
}
