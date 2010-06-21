using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class DetstvoOmskParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("3905490_Детство(11038-04.06.10).txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(5));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("11038"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("04.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("141455"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("MAMASENSE НАБОР ОБУЧ ЛОЖКА И ВИЛКА 250"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo(" Мамасенс - Великобритания"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Великобритания"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(63.22));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(63.22));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(53.58));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("469121"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.01.1980"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("^^^"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
		}
	}
}
