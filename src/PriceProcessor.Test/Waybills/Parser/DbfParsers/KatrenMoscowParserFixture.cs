using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KatrenMoscowParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("15674587_Катрен(828475).DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("828475"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("23.09.2012")));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("33998734"));
			Assert.That(line.Product, Is.EqualTo("АМПИЦИЛЛИН 0,25 N20 ТАБЛ"));
			Assert.That(line.Quantity, Is.EqualTo(15));
			Assert.That(line.SupplierCost, Is.EqualTo(17.16));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(23.4));
			Assert.That(line.Amount, Is.EqualTo(257.4));
			Assert.That(line.SerialNumber, Is.EqualTo("050812"));
			Assert.That(line.Period, Is.EqualTo("01.09.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ10.Д09517"));
			Assert.That(line.Producer, Is.EqualTo("Барнаульский завод медпрепаратов,ООО"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
		}
	}
}
