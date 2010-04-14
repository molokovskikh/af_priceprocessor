using System;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class SiaParserFixture
	{
		[Test]
		public void Parse()
		{
			var parser = new SiaParser();
			var doc = new Document();
			var document = parser.Parse(@"..\..\Data\Waybills\1016416.dbf", doc);
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1016416"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Пентамин 5% Р-р д/ин. 1мл Амп. Х10 Б"));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(171.78));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(156.16));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.06.2013"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ08.Д95450"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("70508"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("12/02/2010")));
		}

		[Test]
		public void Parse_with_character_type()
		{
			var parser = new SiaParser();
			var doc = new Document();
			var document = parser.Parse(@"..\..\Data\Waybills\1016416_char.dbf", doc);
			Assert.That(document.Lines.Count, Is.EqualTo(3));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Г000006147"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Андипал таб. № 10"));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(5.18));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.07.2012"));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ10.Д50325"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(4.71));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("24.03.10")));
		}

		[Test]
		public void Parse_with_vitally_important()
		{
			var parser = new SiaParser();
			var doc = new Document();
			parser.Parse(@"..\..\Data\Waybills\8916.dbf", doc);
			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("R7036"));
			Assert.That(doc.Lines[4].VitallyImportant, Is.True);
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("01/02/2008")));
			Assert.That(doc.Lines[5].VitallyImportant, Is.False);
		}

		[Test]
		public void Parse_with_registry_cost_in_reestr_field()
		{
			var parser = new SiaParser();
			var doc = new Document();
			parser.Parse(@"..\..\Data\Waybills\8916_REESTR.dbf", doc);
			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.Lines[4].RegistryCost, Is.EqualTo(82.0615));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[2].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ЗАМЕНИТЕЛЬ САХАРА\"РИО ГОЛД\" N1200 ТАБЛ"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(SiaParser.CheckFileFormat(@"..\..\Data\Waybills\1016416.dbf"));
			Assert.IsTrue(SiaParser.CheckFileFormat(@"..\..\Data\Waybills\1016416_char.DBF"));
			Assert.IsFalse(SiaParser.CheckFileFormat(@"..\..\Data\Waybills\0000470553.dbf"));
			Assert.IsTrue(SiaParser.CheckFileFormat(@"..\..\Data\Waybills\1040150.DBF"));
			Assert.IsTrue(SiaParser.CheckFileFormat(@"..\..\Data\Waybills\8916.dbf"));
		}

		[Test]
		public void Parse_without_document_date()
		{
			var parser = new SiaParser();
			var document = new Document();
			parser.Parse(@"..\..\Data\Waybills\without_date.dbf", document);
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.DocumentDate.HasValue, Is.False);
			Assert.That(document.DocumentDate, Is.Null);
		}

		[Test]
		public void Parse_without_registry_cost()
		{
			var parser = new SiaParser();
			var document = new Document();
			parser.Parse(@"..\..\Data\Waybills\3655268_Катрен(K_59329).dbf", document);
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.Lines[0].RegistryCost, Is.Null);
		}

		[Test]
		public void Parse_with_null_period()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3677177_0_3677175_0_3676850_Сиа Интернейшнл(1064837).DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(37));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.10.2011"));
			Assert.That(document.Lines[28].Period, Is.Null);
		}

		[Test]
		public void Parse_with_column_jnvls()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3681901_УФК_3681896_УФК_3681714_УФК_t120410___.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(68));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[2].VitallyImportant, Is.True);
		}

		[Test]
		public void Parse_with_column_gzwl()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3683304_УФК_u4004036_.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(4));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[1].VitallyImportant, Is.True);
			Assert.That(document.Lines[2].VitallyImportant, Is.False);
			Assert.That(document.Lines[3].VitallyImportant, Is.False);
		}

		[Test]
		public void Parse_without_producer_cost()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3692407_Сиа_Интернейшнл_1068481_.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(20));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1068481"));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(121.28));
			Assert.That(document.Lines[2].ProducerCost, Is.Null);
		}
	}
}
