using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class BiofarmCheboksarySpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00031412.dbf");

			Assert.That(doc.Lines.Count, Is.EqualTo(31));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("2Б00031412"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("08.11.2011"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("4473"));
			Assert.That(line.Product, Is.EqualTo("Бинт стерильный  7х14 \"Развитие\" инд. упаковка 1/300"));
			Assert.That(line.Producer, Is.EqualTo("ООО ПКФ \"Развитие\""));
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(100));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(9.38));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(9.85));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(98.45));
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.SerialNumber, Is.EqualTo("0711"));
			Assert.That(line.Period, Is.EqualTo("01.01.2016"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ИМ09.В02378"));
			Assert.That(line.Amount, Is.EqualTo(1084));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(5.01));
		}
	}
}