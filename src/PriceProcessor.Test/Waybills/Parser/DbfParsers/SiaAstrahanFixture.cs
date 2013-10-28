using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SiaAstrahanFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(SiaAstrahanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\Р-786953.DBF")));
			var document = WaybillParser.Parse("Р-786953.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(36));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-786953"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("12.03.2012"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("24378"));
			Assert.That(line.Product, Is.EqualTo("Аква Марис капли назальные д/детей 10мл Фл-капельница Б"));
			Assert.That(line.Producer, Is.EqualTo("Ядран Галенский Лабораторий АО"));
			Assert.That(line.Country, Is.EqualTo("ХОРВАТИЯ"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.IsNull(line.ProducerCost);
			Assert.IsNull(line.ProducerCostWithoutNDS);
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(73.22));
			Assert.That(line.SupplierCost, Is.EqualTo(80.54));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Amount, Is.EqualTo(161.08));
			Assert.That(line.NdsAmount, Is.EqualTo(14.64));
			Assert.That(line.Period, Is.EqualTo("01.08.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("2041"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС HR.ФМ01.Д23988"));
			Assert.That(line.CertificatesDate, Is.EqualTo("25.10.2011"));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.EAN13, Is.EqualTo("3858881054738"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130030/251011/0004515/1"));
		}

		[Test]
		public void Parse_supplier_12423_variant()
		{
			var doc = WaybillParser.Parse("P-856773.DBF");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-856773"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("17.11.2012")));

			var l = doc.Lines[3];
			Assert.That(l.Code, Is.EqualTo("1496"));
			Assert.That(l.Product, Is.EqualTo("Бифидумбактерин Сух.пор. д/пр внутрь и мест. прим. Пак. Х30 М (R)"));
			Assert.That(l.Producer, Is.EqualTo("Партнер Ао, РОССИЯ"));
			Assert.That(l.Country, Is.Null);
			Assert.That(l.ProducerCostWithoutNDS, Is.EqualTo(174.04));
			Assert.That(l.ProducerCost, Is.EqualTo(191.444));
			Assert.That(l.SupplierCostWithoutNDS, Is.EqualTo(28.8));
			Assert.That(l.SupplierCost, Is.EqualTo(31.68));
			Assert.That(l.RegistryCost, Is.EqualTo(174.04));
			Assert.That(l.SupplierPriceMarkup, Is.EqualTo(-83.4521));
			Assert.That(l.Amount, Is.EqualTo(31.68));
			Assert.That(l.NdsAmount, Is.EqualTo(2.88));
			Assert.That(l.Quantity, Is.EqualTo(1));
			Assert.That(l.Period, Is.EqualTo("01.12.2012"));
			Assert.That(l.Certificates, Is.EqualTo("РОСС RU.ФМ13.В04323"));
			Assert.That(l.CertificatesDate, Is.Null);
			Assert.That(l.SerialNumber, Is.EqualTo("273-21111"));
			Assert.That(l.BillOfEntryNumber, Is.Null);
			Assert.That(l.EAN13, Is.EqualTo("4600561020019"));
			Assert.That(l.Nds, Is.EqualTo(10));
			Assert.That(l.VitallyImportant, Is.True);
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
		}

		[Test]
		public void Parse_sia_volgograd()
		{
			var doc = WaybillParser.Parse(@"Р-1300016.DBF");
			Assert.AreEqual("Р-1300016", doc.ProviderDocumentId);
			Assert.AreEqual(new DateTime(2013, 10, 28), doc.DocumentDate);
			Assert.AreEqual(28, doc.Lines.Count);
		}
	}
}