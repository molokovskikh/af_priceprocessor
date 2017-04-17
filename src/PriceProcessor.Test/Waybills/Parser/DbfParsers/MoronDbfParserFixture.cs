using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class MoronDbfParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\0000470553.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(72));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("470553,00"));

			Assert.That(document.Lines[5].Code, Is.EqualTo("26505,00"));
			Assert.That(document.Lines[5].Producer, Is.EqualTo("Дина+"));
			Assert.That(document.Lines[5].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[5].Product, Is.EqualTo("Барсучок бальзам д/детей разогрев 30мл"));
			Assert.That(document.Lines[5].SupplierCost, Is.EqualTo(43.07));
			Assert.That(document.Lines[5].SupplierCostWithoutNDS, Is.EqualTo(36.50));
			Assert.That(document.Lines[5].Period, Is.EqualTo("01.11.2011"));
			Assert.That(document.Lines[5].VitallyImportant, Is.False);
			Assert.That(document.Lines[5].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[5].Certificates, Is.EqualTo("РОСС.RU.АИ11.В00697"));
			Assert.That(document.Lines[5].SerialNumber, Is.EqualTo("1109"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("26/03/2010")));
		}

		[Test]
		public void Parse_moron_tula_dbf()
		{
			var doc = WaybillParser.Parse("3860551_Морон_333114_.dbf");

			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("25.05.2010")));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("333114"));

			Assert.That(doc.Lines.Count, Is.EqualTo(19));
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Анальгин св. д/д 250мг №10"));
			Assert.That(line.Producer, Is.EqualTo("Нижфарм"));
			Assert.That(line.Period, Is.EqualTo("01.01.2011"));
			Assert.That(line.SerialNumber, Is.EqualTo("41208"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SupplierCost, Is.EqualTo(25.96));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(23));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.Certificates, Is.EqualTo("РОСС.RU.ФМ01.Д28661"));
		}

		[Test]
		public void VectorGroup_16777_dbf()
		{
			var doc = WaybillParser.Parse("вар13-A7623.DBF");

			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("03.06.2016")));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("7623"));

			Assert.That(doc.Lines.Count, Is.EqualTo(3));
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("СЕНИ АКТИВ впит.трусики д/взрослых M нор"));
			Assert.That(line.Producer, Is.EqualTo("БЕЛЛА"));
			Assert.That(line.Period, Is.EqualTo("01.05.2019"));
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SupplierCost, Is.EqualTo(329.07));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(299.15));
			Assert.That(line.Country, Is.EqualTo("Польша"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.VitallyImportant, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.PL46.Д65783"));
			Assert.That(line.Code, Is.EqualTo("715808"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(299.15));
			Assert.That(line.EAN13, Is.EqualTo(5900516693046));
			Assert.That(line.CertificateAuthority, Is.EqualTo("\"РОСТЕСТ-МОСКВА\" ЗАО,Москва,Нахимовский проспект,31,тел.(499) 129-26-00"));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(DateTime.Parse("26.02.2017")));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130060/130912/0022841/2"));
			Assert.That(line.Amount, Is.EqualTo(329.07));
			Assert.That(line.Unit, Is.EqualTo("шт."));
			Assert.That(doc.Invoice.SellerName, Is.EqualTo("ООО \"Вектор Групп\""));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsTrue(MoronDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(MoronDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\890579.dbf")));
		}
	}
}