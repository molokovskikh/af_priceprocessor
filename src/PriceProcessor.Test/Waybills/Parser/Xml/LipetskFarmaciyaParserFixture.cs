using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Xml
{
	[TestFixture]
	public class LipetskFarmaciyaParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\00УT-057181.xml");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00УT-057181"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2016,10,12)));

			var line0 = doc.Lines[0];
			Assert.That(line0.Product, Is.EqualTo("L-Лизина эсцинат, конц. д/пригот. р-ра для в/в введ. 1 мг/мл 5 мл №10"));
			Assert.That(line0.Producer, Is.EqualTo("ГАЛИЧФАРМ ПАО"));
			Assert.That(line0.Country, Is.EqualTo("УКРАИНА"));
			Assert.That(line0.SerialNumber,  Is.EqualTo("810815"));
			Assert.That(line0.SupplierCost, Is.EqualTo(1590));
			Assert.That(line0.ProducerCostWithoutNDS, Is.EqualTo(1155.6));
			Assert.That(line0.Quantity, Is.EqualTo(2));
			Assert.That(line0.VitallyImportant, Is.EqualTo(false));
			Assert.That(line0.Nds, Is.EqualTo(10));

		}
	}
}
	