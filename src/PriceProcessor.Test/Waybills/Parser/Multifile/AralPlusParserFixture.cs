using System;
using System.Linq;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Multifile
{
	[TestFixture]
	public class AralPlusParserFixture
	{
		[Test]
		public void Parse()
		{
			var files = WaybillParser.GetFilesForParsing("b2921.dbf", "h2921.dbf");

			var mergedFiles = MultifileDocument.Merge(files);
			Assert.That(mergedFiles.Count, Is.EqualTo(1));
			var doc = WaybillParser.Parse(mergedFiles.Single().FileName);
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("3088745"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2010, 7, 24)));
			var line = doc.Lines[0];
			
			Assert.That(line.Code, Is.EqualTo("12947"));
			Assert.That(line.Product, Is.EqualTo("Метеоспазмил, капс.№20"));
			Assert.That(line.Producer, Is.EqualTo("Майоли Спиндлер"));
			Assert.That(line.Country, Is.EqualTo("Франция"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(179.272));
			Assert.That(line.SupplierCost, Is.EqualTo(197.2));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.EqualTo("vn4415"));
			Assert.That(line.Period, Is.EqualTo("01.12.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.FR.ФМ08.Д98850"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(179.27));

			Assert.That(doc.Lines[1].VitallyImportant, Is.True);
			Assert.That(doc.Lines[1].RegistryCost, Is.EqualTo(27.88));
			Assert.That(doc.Lines[1].SupplierPriceMarkup, Is.EqualTo(16.15));
		}
	}
}