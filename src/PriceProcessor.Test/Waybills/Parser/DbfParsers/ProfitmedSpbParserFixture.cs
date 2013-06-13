using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class ProfitmedSpbParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("1365_156-11.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("1365/156-11"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("17.06.2011"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("390"));
			Assert.That(line.Product, Is.EqualTo("Апизартрон мазь 20г"));
			Assert.That(line.Producer, Is.EqualTo("Esparma GmbH/Salutas Pharma GmbH"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(87.0000));
			Assert.That(line.SupplierCost, Is.EqualTo(95.7000));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0.0000));
			Assert.That(line.NdsAmount, Is.EqualTo(26.1000));
			Assert.That(line.Amount, Is.EqualTo(287.1000));
			Assert.That(line.SerialNumber, Is.EqualTo("034039"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ08.Д53042"));
			Assert.That(line.Period, Is.EqualTo("01.10.2012"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(ProfitmedSpbParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1365_156-11.dbf")));
		}
	}
}