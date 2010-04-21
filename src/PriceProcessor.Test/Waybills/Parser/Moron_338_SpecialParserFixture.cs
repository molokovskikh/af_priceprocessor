using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using Castle.ActiveRecord;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class Moron_338_SpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			DocumentLog documentLog = null;
			using (new SessionScope()) {
				var supplier = Supplier.Find(338);
				documentLog = new DocumentLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\3668585_5_00475628.dbf", documentLog) is Moron_338_SpecialParser);

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3668585_5_00475628.dbf", documentLog);

			Assert.That(document.ProviderDocumentId, Is.EqualTo("475628"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("07/04/2010")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("2057,00"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Атенолол таб. 50мг №30"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Дания"));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(29.02));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(30.12));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(33.13));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(1.10));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10.00));
		}

		[Test]
		public void Parse_with_null_period()
		{
			DocumentLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(338);
				documentLog = new DocumentLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3676275_Морон(476832).dbf", documentLog);

			Assert.That(document.ProviderDocumentId, Is.EqualTo("476832"));
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.Lines[0].Period, Is.Null);			
		}

		[Test]
		public void Parse_Ekaterinburg_farm()
		{
			DocumentLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(4001);
				documentLog = new DocumentLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\bi055540.DBF", documentLog) is Moron_338_SpecialParser);
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\bi055540.DBF", documentLog);

			Assert.That(document.ProviderDocumentId, Is.EqualTo("55540"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("19/02/2010")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("252839416"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Азитромицин 250мг капс №6"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Вертекс ЗАО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("РОССИЯ"));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(67.17));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(70.59));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.11.2011"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ03.Д01939"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("231009"));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(80.70));
		}

		[Test]
		public void Parse_Katren_Ufa()
		{
			DocumentLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(7146);
				documentLog = new DocumentLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\K_69960.dbf", documentLog) is Moron_338_SpecialParser);

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\K_69960.dbf", documentLog);
			Assert.That(document.Lines.Count, Is.EqualTo(21));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("69960"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("19.04.2010")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("410726"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АМОКСИКЛАВ 0,5+0,125 N15 ТАБЛ П/О"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Лек Д.Д."));
			Assert.That(document.Lines[0].Country, Is.EqualTo("словения"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(326.81));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(297.1));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(284.2500));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(4.5200));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.01.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС SI.ФМ08.Д55748"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(284.2500));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("AR5148"));
		}
	}
}
