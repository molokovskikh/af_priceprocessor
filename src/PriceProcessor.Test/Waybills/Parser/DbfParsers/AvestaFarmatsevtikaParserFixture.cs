using System;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class AvestaFarmatsevtikaParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("78930_10.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("78930"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("28.05.2010")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("40570"));
			Assert.That(line.Product, Is.EqualTo("L-ТИРОКСИН таб 100мкг N100 Berlin"));
			Assert.That(line.Producer, Is.EqualTo("Berlin-Chemie"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.11.2011"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ01.Д49539"));
			Assert.That(line.SupplierCost, Is.EqualTo(122.24));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(111.1300));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(102.2900));
			Assert.That(line.SerialNumber, Is.EqualTo("94075"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(8.64));
			Assert.That(line.VitallyImportant, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
		}

		[Test]
		public void Parse2()
		{
			var doc = WaybillParser.Parse("78930_18.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("78930"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("28.05.2010")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("1644427"));
			Assert.That(line.Product, Is.EqualTo("ВЕРБЕНА-чистые сосуды капли 50мл КоролевФарм"));
			Assert.That(line.Producer, Is.EqualTo("КоролевФарм ООО"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Period, Is.EqualTo("01.03.2012"));
			Assert.That(line.Certificates, Is.EqualTo("RU.002.П0168"));
			Assert.That(line.SupplierCost, Is.EqualTo(137.43));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(116.4700));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(103.0700));
			Assert.That(line.SerialNumber, Is.EqualTo("110."));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(13.00));
			Assert.That(line.VitallyImportant, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
		}

		[Test]
		public void Parse_FarmGroupFormat()
		{
			var doc = WaybillParser.Parse("79011_10.dbf");
			var providerDocId = Document.GenerateProviderDocumentId();
			providerDocId = providerDocId.Remove(providerDocId.Length - 1);

			Assert.IsFalse(doc.ProviderDocumentId.StartsWith(providerDocId));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("79011_10"));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("48834"));
			Assert.That(line.Product, Is.EqualTo("ЛИНКОМИЦИНА Г/Х амп 30% 1мл N10 БорисЗМП"));
			Assert.That(line.Producer, Is.EqualTo("Борисовский ЗМП"));
			Assert.That(line.Country, Is.EqualTo("Беларусь"));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.03.2013"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BY.ФМ08.Д55707"));
			Assert.That(line.SupplierCost, Is.EqualTo(20.7350));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(18.8500));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(17.7600));
			Assert.That(line.SerialNumber, Is.EqualTo("110210"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(6.1374));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.EqualTo(0));

			Assert.That(doc.Lines[2].VitallyImportant, Is.True);
			Assert.That(doc.Lines[2].RegistryCost, Is.EqualTo(2));
		}

		private DocumentReceiveLog CreateDocumentLog(uint supplierId, string fileName)
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = supplierId } };
			Assert.IsTrue(WaybillParser.GetParserType(fileName, documentLog) is Avesta_6256_SpecialParser);
			return documentLog;
		}

		[Test, Description("Тест для файла, в котором тип одной из колонок указан как NUMBER, но там встречаются строковые значения")]
		public void Parse_unsafe_with_wrong_column_type()
		{
			var documentLog = CreateDocumentLog(6256u, @"..\..\Data\Waybills\4006238_Авеста-Фармацевтика_106949_1_.dbf");
			var doc = WaybillParser.Parse("4006238_Авеста-Фармацевтика_106949_1_.dbf", documentLog);

			var providerDocId = Document.GenerateProviderDocumentId();
			providerDocId = providerDocId.Remove(providerDocId.Length - 1);

			Assert.IsFalse(doc.ProviderDocumentId.StartsWith(providerDocId));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("41215"));
			Assert.That(line.Product, Is.EqualTo("АМПИЦИЛЛИН Т/Г таб 250мг N20 Барнаул"));
			Assert.That(line.Producer, Is.EqualTo("Барнаульский ЗМП"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Quantity, Is.EqualTo(8));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.05.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ10.Д05887"));
			Assert.That(line.SupplierCost, Is.EqualTo(18.9200));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(17.2000));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(15.7300));
			Assert.That(line.SerialNumber, Is.EqualTo("100410"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(9.3452));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.EqualTo(0));
		}

		[Test]
		public void Parse_vitally_important()
		{
			var doc = WaybillParser.Parse("147242.dbf");
			var line = doc.Lines[5];
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.RegistryCost, Is.EqualTo(62.58));
		}

		[Test]
		public void Parse3()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 6256u } };
			var fileName = @"..\..\Data\Waybills\4049766_Авеста-Фармацевтика(118955).dbf";
			Assert.IsFalse(WaybillParser.GetParserType(fileName, documentLog) is Avesta_6256_SpecialParser);

			var doc = WaybillParser.Parse("4049766_Авеста-Фармацевтика(118955).dbf", documentLog);
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("118955"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("09.07.2010")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("40764"));
			Assert.That(line.Product, Is.EqualTo("АДЖИСЕПТ МЕНТОЛ+ЭВКАЛИПТ пастилки N24"));
			Assert.That(line.Producer, Is.EqualTo("Agio Pharmaceuticals"));
			Assert.That(line.Country, Is.EqualTo("Индия"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("31.12.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС IN.ФМ08.Д95800"));
			Assert.That(line.SupplierCost, Is.EqualTo(22.90));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(20.8200));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(20.4200));
			Assert.That(line.SerialNumber, Is.EqualTo("10/14/0004"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(1.96));
			Assert.That(line.VitallyImportant, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
		}

		[Test]
		public void Read_vitaly_important_flag_from_sst()
		{
			var doc = WaybillParser.Parse("4090225_Авеста-Фармацевтика(130122).sst");
			Assert.That(doc.Lines.Count, Is.EqualTo(24));
			var line = doc.Lines[2];
			Assert.That(line.Code, Is.EqualTo("1411866"));
			Assert.That(line.Product, Is.EqualTo("ВОДА д/инъекций амп 5мл N10 Эллара"));
			Assert.That(line.Producer, Is.EqualTo("Эллара МЦ ООО"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.VitallyImportant, Is.True);
		}
	}
}