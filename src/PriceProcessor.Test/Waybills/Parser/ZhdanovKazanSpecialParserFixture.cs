using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills;


namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class ZhdanovKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 7957u } }; // код поставщика ИП Жданов, Казань
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\02000489.dbf", documentLog) is ZhdanovKazanSpecialParser);
			
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\02000489.dbf", documentLog);
			Assert.That(document.Lines.Count, Is.EqualTo(48));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("000489"));			
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("24.03.2011"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("1200000757"));
			Assert.That(line.Product, Is.EqualTo("Johnsons baby Масло 100 мл"));
			Assert.That(line.Producer, Is.EqualTo("Джонсон"));
			Assert.That(line.ProducerCost, Is.EqualTo(66.84));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Period, Is.Null);
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("РОСС IT.ПК05.В27822"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(66.84));
			Assert.That(line.SupplierCost, Is.EqualTo(66.84));
			Assert.That(line.Amount, Is.EqualTo(133.68));
			Assert.That(line.NdsAmount, Is.EqualTo(0));

			line = document.Lines[1];
			Assert.That(line.Code, Is.EqualTo("1200000968"));
			Assert.That(line.Product, Is.EqualTo("Johnsons baby Масло 200 мл"));
			Assert.That(line.Producer, Is.EqualTo("Джонсон"));
			Assert.That(line.ProducerCost, Is.EqualTo(127.89));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Period, Is.EqualTo("01.09.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("0913,"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС IT.ПК05.В27822"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(127.89));
			Assert.That(line.SupplierCost, Is.EqualTo(127.89));
			Assert.That(line.Amount, Is.EqualTo(127.89));
			Assert.That(line.NdsAmount, Is.EqualTo(0));
		}

		[Test]
		public void Parse2()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 8063u } }; // код поставщика ООО "Бизон", Казань
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\7833642.dbf", documentLog) is ZhdanovKazanSpecialParser);

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\7833642.dbf", documentLog);
			Assert.That(document.Lines.Count, Is.EqualTo(48));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("000489"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("24.03.2011"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("1200000757"));
			Assert.That(line.Product, Is.EqualTo("Johnsons baby Масло 100 мл"));
			Assert.That(line.Producer, Is.EqualTo("Джонсон"));
			Assert.That(line.ProducerCost, Is.EqualTo(66.84));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Period, Is.Null);
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("РОСС IT.ПК05.В27822"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(66.84));
			Assert.That(line.SupplierCost, Is.EqualTo(66.84));
			Assert.That(line.Amount, Is.EqualTo(133.68));
			Assert.That(line.NdsAmount, Is.EqualTo(0));

			line = document.Lines[1];
			Assert.That(line.Code, Is.EqualTo("1200000968"));
			Assert.That(line.Product, Is.EqualTo("Johnsons baby Масло 200 мл"));
			Assert.That(line.Producer, Is.EqualTo("Джонсон"));
			Assert.That(line.ProducerCost, Is.EqualTo(127.89));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Period, Is.EqualTo("01.09.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("0913,"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС IT.ПК05.В27822"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(127.89));
			Assert.That(line.SupplierCost, Is.EqualTo(127.89));
			Assert.That(line.Amount, Is.EqualTo(127.89));
			Assert.That(line.NdsAmount, Is.EqualTo(0));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(ZhdanovKazanSpecialParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\02000489.dbf")));
			Assert.IsTrue(ZhdanovKazanSpecialParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\7833642.dbf")));
		}
	}
}
