using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class FarmakonPlusParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("FP89356.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(7));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("RFP89356"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("18/06/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("570"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Бромгексин 0,008г №25 таб.п/о"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Берлин-Хеми АГ / Менарини Групп"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Германия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(27.26));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(29.99));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(27.26));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("92516"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС.DE.ФМ01.Д78928"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.06.2014"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[1].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
		}
	}
}
