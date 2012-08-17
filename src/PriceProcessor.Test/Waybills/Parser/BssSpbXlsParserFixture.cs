using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class BssSpbXlsParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("3902401_Медицина(Документ РН-0110107).xls");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("РН-0110107"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("02.06.10")));

			Assert.That(document.Lines.Count, Is.EqualTo(7));
			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Глицин-форте 100мг таб. №50"));
			Assert.That(line.Code, Is.EqualTo("17572"));
			Assert.That(line.Producer, Is.EqualTo("Фармгрупп"));
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.SupplierCost, Is.EqualTo(10.02));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(8.49));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(line.SerialNumber, Is.EqualTo("п.05"));
			Assert.That(line.Certificates, Is.EqualTo("СОГР7799233У7281808"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Period, Is.EqualTo("01.12.12"));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.VitallyImportant, Is.False);

			Assert.That(document.Lines[4].VitallyImportant, Is.True);
			Assert.That(document.Lines[4].RegistryCost, Is.EqualTo(15.50));
			Assert.That(document.Lines[1].ProducerCostWithoutNDS, Is.EqualTo(11.59));
		}
	}
}