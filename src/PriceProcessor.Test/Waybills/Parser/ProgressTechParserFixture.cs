using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class ProgressTechParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("PT001870.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(3));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("ЭПТ0001870"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("13.05.2010")));
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("Э0001549"));
			Assert.That(line.Product, Is.EqualTo("Аскорутин табл №50"));
			Assert.That(line.Producer, Is.EqualTo("Вифитех"));
			Assert.That(line.Country, Is.EqualTo("         1"));
			Assert.That(line.SerialNumber, Is.EqualTo("011209"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ08.Д66127"));
			Assert.That(line.Period, Is.EqualTo("01.01.2013"));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.SupplierCost, Is.EqualTo(27.4));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(24.91));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.VitallyImportant, Is.False);
		}
	}
}