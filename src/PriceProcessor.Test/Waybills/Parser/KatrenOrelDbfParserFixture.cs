using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class KatrenOrelDbfParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\83504.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(15));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("83504"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("30.04.2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("31897628"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АМЕЛОТЕКС 0,015 N20 ТАБЛ"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Реплекфарм АО/ФармФирма Сотекс (Россия)"));
			Assert.That(document.Lines[0].Country, Is.Null);
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(250.0000));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(246.7000));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("010110"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д82590"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.09.2011"));
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[4].RegistryCost, Is.EqualTo(122.3400));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(271.3700));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
		}

		[Test]
		public void Parse_with_vitally_important()
		{
			var doc = WaybillParser.Parse("97303.dbf");
			Assert.That(doc.Lines[2].VitallyImportant, Is.True);
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\fm21554.dbf")));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(KatrenOrelDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\95472.dbf")));
			Assert.IsTrue(KatrenOrelDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\83504.dbf")));
		}

		[Test]
		public void Parse_SiaInternationalVrn()
		{
			var document = WaybillParser.Parse(@"Р1903070.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(6));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-1903070"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("28.05.2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("14823"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Аргосульфан 2% Крем 40г"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Polfa/PF Jelfa SA, Польша"));
			Assert.That(document.Lines[0].Country, Is.Null);
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(171.6500));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(163.0600));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("912061"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС PL.ФМ01.Д06613"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("31.12.2011"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[1].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(179.3700));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(-5.0044));
		}

		[Test]
		public void Parse_BellaVolga()
		{
			var document = WaybillParser.Parse(@"00011560.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(27));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("к0000011560"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("20.09.2011")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("26"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Прокладки женские гигиенические впитывающие\"bella\" \"Classic Nova Maxi\" drainette air по 10 шт"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("TZMO З.А."));
			Assert.That(document.Lines[0].Country, Is.Null);
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(29.6550));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(32.6200));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(29.6550));
			Assert.That(document.Lines[0].Period, Is.Null);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ИМ09.В02709"));
			Assert.That(document.Lines[0].SerialNumber, Is.Null);						
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);			
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[19].Nds, Is.EqualTo(18));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(5.93));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(65.2400));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.Null);
			Assert.That(document.Lines[1].BillOfEntryNumber, Is.EqualTo("10130060/120911/0025285/1"));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("5900516300920"));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
		}

		[Test]
		public void Parse_BellaVolgaKazan()
		{
			var document = WaybillParser.Parse(@"00011560_.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(27));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("к0000011560"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("20.09.2011")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("26"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Прокладки женские гигиенические впитывающие\"bella\" \"Classic Nova Maxi\" drainette air по 10 шт"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("TZMO З.А."));
			Assert.That(document.Lines[0].Country, Is.Null);
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(29.6550));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(32.6200));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(29.6550));
			Assert.That(document.Lines[0].Period, Is.Null);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ИМ09.В02709"));
			Assert.That(document.Lines[0].SerialNumber, Is.Null);						
			Assert.That(document.Lines[0].VitallyImportant, Is.Null);			
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[19].Nds, Is.EqualTo(18));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(5.93));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(65.2400));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.Null);
			Assert.That(document.Lines[1].BillOfEntryNumber, Is.EqualTo("10130060/120911/0025285/1"));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("5900516300920"));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
		}
	}
}
