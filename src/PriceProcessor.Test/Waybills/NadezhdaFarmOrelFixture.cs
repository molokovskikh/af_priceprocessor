using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using PriceProcessor.Test.Waybills.Parser;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class NadezhdaFarmOrelFixture
	{
		[Test]
		public void Parse()
		{
			var now = DateTime.Now;
			var doc = WaybillParser.Parse("14356_4.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("14356_4"));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(now.ToString()));
			Assert.That(doc.Lines.Count, Is.EqualTo(1));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("3345"));
			Assert.That(line.Product, Is.EqualTo("Коринфар таб п/о 10мг № 50"));
			Assert.That(line.Producer, Is.EqualTo("Плива Хрватска д.о.о./АВД фарма ГмбХ и Ко КГ"));
			Assert.That(line.Country, Is.EqualTo("Хорватия/Германия"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("17.02.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ08.Д05192"));
			Assert.That(line.SupplierCost, Is.EqualTo(45.05000));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(40.95330));
			Assert.That(line.ProducerCost, Is.EqualTo(39.18000));
			Assert.That(line.SerialNumber, Is.EqualTo("9B018A"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(15.00000));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.RegistryCost, Is.EqualTo(50.08000));
		}

		[Test]
		public void Parse2()
		{
			var doc = WaybillParser.Parse("14326_4.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("14326_4"));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
			Assert.That(doc.Lines.Count, Is.EqualTo(4));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("24235"));
			Assert.That(line.Product, Is.EqualTo("Лозап таб. п/о 12.5 №30"));
			Assert.That(line.Producer, Is.EqualTo("Зентива а.с."));
			Assert.That(line.Country, Is.EqualTo("Чешская Республика"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("31.12.2011"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС SK.ФМ11.Д04901"));
			Assert.That(line.SupplierCost, Is.EqualTo(193.14000));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(175.58000));
			Assert.That(line.ProducerCost, Is.EqualTo(170.93000));
			Assert.That(line.SerialNumber, Is.EqualTo("2010110"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(13.00000));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.RegistryCost, Is.EqualTo(155.39000));
		}

		[Test]
		public void Parse3()
		{
			var doc = WaybillParser.Parse("14326_0.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("14326_0"));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
			Assert.That(doc.Lines.Count, Is.EqualTo(6));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("2404"));
			Assert.That(line.Product, Is.EqualTo("З/щетка Колгейт Классическая чистота (мягкая)"));
			Assert.That(line.Producer, Is.EqualTo("Колгейт Санкшао"));
			Assert.That(line.Country, Is.EqualTo("Китай"));
			Assert.That(line.Quantity, Is.EqualTo(6));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Period, Is.EqualTo("01.08.2018"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС CN ПК04 В00054"));
			Assert.That(line.SupplierCost, Is.EqualTo(12.19000));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(10.33000));
			Assert.That(line.ProducerCost, Is.EqualTo(11.61000));
			Assert.That(line.SerialNumber, Is.EqualTo("Б/С"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(5.00000));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.EqualTo(0));
		}
	}
}
