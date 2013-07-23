using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	class SiaSamaraParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Р-1017256.DBF");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-1017256"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("23.07.2013")));
			var l = doc.Lines[0];
			Assert.That(l.Code, Is.EqualTo("167"));
			Assert.That(l.Quantity, Is.EqualTo(10));
			Assert.That(l.SupplierCost, Is.EqualTo(78.03));
			Assert.That(l.Nds, Is.EqualTo(10));
			Assert.That(l.NdsAmount, Is.EqualTo(70.94));
			Assert.That(l.Amount, Is.EqualTo(780.34));
			Assert.That(l.SupplierCostWithoutNDS, Is.EqualTo(70.94));
			Assert.That(l.ProducerCost, Is.EqualTo(null));
			Assert.That(l.ProducerCostWithoutNDS, Is.EqualTo(null));
			Assert.That(l.SupplierPriceMarkup, Is.EqualTo(null));
			Assert.That(l.RegistryCost, Is.EqualTo(null));
			Assert.That(l.RegistryDate, Is.Null);
			Assert.That(l.SerialNumber, Is.EqualTo("DC4294"));
			Assert.That(l.BillOfEntryNumber, Is.EqualTo("10130060/120313/0005747/04"));
			Assert.That(l.Certificates, Is.EqualTo("POCC SI.ФМ08.Д39137"));
			Assert.That(l.Product, Is.EqualTo("5-нок 50мг Таб. П/о Х50 Б"));
			Assert.That(l.Producer, Is.EqualTo("Лек д.д., СЛОВЕНИЯ"));
			Assert.That(l.Period, Is.EqualTo("01.01.2018"));
			Assert.That(l.EAN13, Is.EqualTo("3838957090976"));
			Assert.That(l.VitallyImportant, Is.EqualTo(false));
		}
	}
}
