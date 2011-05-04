using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Xls
{
	[TestFixture]
	public class VectorXlsParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("Аптека93.xls");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("8345-С"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("07.02.2011")));

			Assert.That(document.Lines.Count, Is.EqualTo(8));
			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("ДИВА прокл.Comfort Soft 9"));
			Assert.That(line.Code, Is.EqualTo("37739"));
			Assert.That(line.Producer, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.SupplierCost, Is.EqualTo(20.26));
			Assert.That(line.SupplierCostWithoutNDS, Is.Null);
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.ProducerCost, Is.Null);
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.Null);
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Period, Is.Null);
			Assert.That(line.VitallyImportant, Is.Null);

			Assert.That(document.Lines[1].SupplierCost, Is.EqualTo(25.48));
		}		
	}
}
