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
		public DocumentReceiveLog CreateLogEntry(uint supplierId, string fileName)
		{
			DocumentReceiveLog documentLog = null;
			using (new SessionScope()) {
				var supplier = Supplier.Find(supplierId);
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			Assert.IsTrue(WaybillParser.GetParserType(fileName, documentLog) is Moron_338_SpecialParser);
			return documentLog;
		}


		[Test]
		public void Parse()
		{
			DocumentReceiveLog documentLog = null;
			using (new SessionScope()) {
				var supplier = Supplier.Find(338u);
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
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
			DocumentReceiveLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(338u);
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
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
			DocumentReceiveLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(4001u);
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
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
			DocumentReceiveLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(7146u);
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
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

		[Test]
		public void Parse_ForaFarm_Chelyabinsk()
		{
			DocumentReceiveLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(5802u);
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\44027.dbf", documentLog) is Moron_338_SpecialParser);

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\44027.dbf", documentLog);
			Assert.That(document.Lines.Count, Is.EqualTo(6));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("45027"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("31/05/2010")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("394"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Боро Плюс (розовый) 25мл крем"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Emami limited"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Индия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(12));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(26.8));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(22.71));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(0.00));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.09.2014"));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18.00));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС IN.ПК08.В02670"));
			Assert.That(document.Lines[0].RegistryCost, Is.Null);
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("AT0023"));
		}

		[Test]
		public void Parse_Katren_Ufa_with_column_vital()
		{
			DocumentReceiveLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(7146u);
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\K_12345.dbf", documentLog) is Moron_338_SpecialParser);

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\K_12345.dbf", documentLog);
			Assert.That(document.Lines.Count, Is.EqualTo(22));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("12345"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("22.01.2010")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("1126300"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АКРИДЕРМ СК 15,0 МАЗЬ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(68.5000));
			Assert.That(document.Lines[0].Country, Is.EqualTo("россия"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Акрихин ХФК ОАО"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.10.2012"));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(68.5000));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д03430"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("501009"));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(75.35));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[1].VitallyImportant, Is.True);
			Assert.That(document.Lines[2].VitallyImportant, Is.False);
		}

		[Test]
		public void Parse_Sia_Orel_with_zhnvls()
		{
			var documentLog = CreateLogEntry(21u, @"..\..\Data\Waybills\Р-1081732.DBF");
			var doc = WaybillParser.Parse("Р-1081732.DBF", documentLog);

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-1081732"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("30.06.2010")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("3716"));
			Assert.That(line.Product, Is.EqualTo("Амбробене 30мг Таб. Х20"));
			Assert.That(line.Producer, Is.EqualTo("Ratiopharm/Merckle"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.ProducerCost, Is.EqualTo(42.3700));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.03.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ08.Д01507"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(39.7100));
			Assert.That(line.SupplierCost, Is.EqualTo(43.68));
			Assert.That(line.SerialNumber, Is.EqualTo("J12090"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-6.2780));
			Assert.That(line.VitallyImportant, Is.False);

			Assert.That(doc.Lines[1].VitallyImportant, Is.True);
		}

		[Test]
		public void Parse_Moron_zhnvls()
		{
			DocumentReceiveLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(338u);
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3716168_Морон_482025_.dbf", documentLog);
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\3716168_Морон_482025_.dbf", documentLog) is Moron_338_SpecialParser);

			Assert.That(document.Lines.Count, Is.EqualTo(9));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("482025"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(38.40));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[3].VitallyImportant, Is.True);
			Assert.That(document.Lines[7].VitallyImportant, Is.True);
		}
	}
}
