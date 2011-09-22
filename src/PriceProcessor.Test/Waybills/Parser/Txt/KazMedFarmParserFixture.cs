using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class KazMedFarmParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"18296308_001.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(18));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("18296308-001"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("21.09.2011")));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("202867"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ВАЛИДОЛ КАПС. 50МГ №40"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(16.41));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(14.92));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(2.98));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(32.82));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("POCC RU.ФM05.Д61964"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("050511"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.06.2013"));
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.Null);
			Assert.That(doc.Lines[2].BillOfEntryNumber, Is.EqualTo("10130032/100211/0000358"));
			Assert.That(doc.Lines[0].EAN13, Is.EqualTo("04607004431395"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Люми ООО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
		}
	}
}
