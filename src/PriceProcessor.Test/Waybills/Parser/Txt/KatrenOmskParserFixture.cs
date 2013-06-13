using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class KatrenOmskParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"3907429_Катрен(38880).txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(4));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("38880"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("04.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("7592290"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("CAREFREE ПРОКЛАДКИ ЕЖЕДН LARGE PLUS N20"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Johnson and Johnson S.p.A."));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Италия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(80.25));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(93.72));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(85.20));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("082009"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.08.2012"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС.IT.АЯ02.B38951"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);

			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[1].VitallyImportant, Is.False);
			Assert.That(doc.Lines[2].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(6.17));
		}

		[Test]
		public void Parse_MyBaby_3783()
		{
			var doc = WaybillParser.Parse("0000005260_22.06.10.txt");

			Assert.That(doc.Lines.Count, Is.EqualTo(24));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("0000005260"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("22.06.10")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("00003800"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("\"БАБУШКИНО ЛУКОШКО\" детская питьевая вода, 1500 мл"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("АКВА-ПАК"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(15.59));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(20.24));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(17.15));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(doc.Lines[0].SerialNumber, Is.Null);
			Assert.That(doc.Lines[0].Period, Is.EqualTo("02.04.12"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("*РОСС RU.АЮ97.В39177"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);

			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[1].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(10.01));
		}
	}
}