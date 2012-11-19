using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class FarmSet_VoronezhParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Та534098.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("ФК001534098"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("13.11.2012")));
			var line = doc.Lines[2];
			Assert.That(line.Code, Is.EqualTo("114"));
			Assert.That(line.Product, Is.EqualTo("Бронхолитин 125мл сироп"));
			Assert.That(line.Producer, Is.EqualTo("Sopharma"));
			Assert.That(line.Country, Is.EqualTo("БОЛГАРИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(52.39));
			Assert.That(line.SupplierCost, Is.EqualTo(57.63));
			Assert.That(line.SerialNumber, Is.EqualTo("4010512"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BG.ФМ03.Д79568"));
			Assert.That(line.Period, Is.EqualTo("01.05.2016"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(43.73));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.EqualTo("3800010650175"));
			Assert.That(line.CertificateFilename, Is.EqualTo(@"Б\БРОНХОЛИТИН_4010512.TIF"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10210190/030712/0011654"));
			Assert.That(line.OrderId, Is.EqualTo(35302648));
		}
	}
}
