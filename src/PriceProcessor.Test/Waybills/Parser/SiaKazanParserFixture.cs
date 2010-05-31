using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	public class SiaKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("3879289_СИА_Интернейшнл_Р-424409_.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-424409"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("28.05.2010")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("79460"));
			Assert.That(line.Product, Is.EqualTo("Вильпрафен Солютаб дисперг. 1000мг Таб. Х10"));
			Assert.That(line.Producer, Is.EqualTo("Yamanouchi"));
			Assert.That(line.Country, Is.EqualTo("Италия"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.10.2011"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС IT.ФМ09.Д03420 ()"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(552.0300));
			Assert.That(line.SupplierCost, Is.EqualTo(607.2300));
			Assert.That(line.SerialNumber, Is.EqualTo("09J01/87"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
		}
	}
}
