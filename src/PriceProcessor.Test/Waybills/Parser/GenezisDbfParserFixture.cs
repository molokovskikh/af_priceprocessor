using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class GenezisDbfParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\890579.dbf");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("890579"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("23/03/2010")));
			Assert.That(document.Lines.Count, Is.EqualTo(9));
			Assert.That(document.Lines[0].Code, Is.EqualTo("51408"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АЦИПОЛ КАПС 10МЛН.КОЕ N30"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Российская Федерация"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("ЛЕККО ФФ ЗАО"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(120.80));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(141.79));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(128.90000));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.12.2011"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("002794"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("56"));			
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(GenezisDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsTrue(GenezisDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\890579.dbf")));
		}

		[Test]
		public void Parse_without_country()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3687858_Генезис УФА(3686469_Генезис Екатеринбург(463344)).dbf");

			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("463344"));
			Assert.IsNull(doc.Lines[0].Country);
			Assert.IsNull(doc.Lines[1].Country);

			Assert.IsNull(doc.Lines[0].VitallyImportant);
			Assert.IsNull(doc.Lines[1].VitallyImportant);
		}

		[Test]
		public void Parse_Genezis_Ekaterinburg()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3703756_Генезис_Екатеринбург_905875_.dbf");

			Assert.That(doc.Lines.Count, Is.EqualTo(3));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("905875"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("12/04/2010")));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("175"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АСКОРБИНОВ. К-ТА ТАБ. 0.1Г N10 С ГЛЮК."));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("ИРБИТСКИЙ ХИМФАРМЗАВОД ОАО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Российская Федерация"));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(2.37));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(100));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("250210"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОССRUФМ05Д67000"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.03.2011"));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(2.59000));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(2.85));
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
		}

		[Test]
		public void Parse_Genezis_Perm()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\898091.DBF");

			Assert.That(doc.Lines.Count, Is.EqualTo(1));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("898091"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("14/04/2010")));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("46338"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ФЛЕМОКЛАВ СОЛЮТАБ ТАБ. 500МГ+125МГ N20"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("АСТЕЛЛАС ФАРМА ЮРОП Б.В."));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Нидерланды"));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(452.70));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(503.00));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0.00));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("09G10/57"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОССNLФМ09Д00878"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.07.2012"));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(273.27000));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(300.60));
			Assert.That(doc.Lines[0].VitallyImportant, Is.True);
		}
	}
}
