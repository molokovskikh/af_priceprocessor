using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class Rafel2758TxtParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("02387.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(17));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("ПР-ЧЛН02387"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("21.05.2012")));
			Assert.That(doc.Invoice.BuyerName, Is.EqualTo("ООО РАФЭЛ"));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("52185"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Ацетилсалициловая кислота 0,5гN10"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("РУП \"Борисовский завод медпрепаратов\", Беларусь"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Беларусь"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(20));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(1.75));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(3.49));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("470212"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.03.2016"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС ВY.ФМ05.Д09920"));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(1.67));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(1.52));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(5));
			Assert.That(doc.Lines[0].VitallyImportant, Is.EqualTo(true));
		}
	}
}