using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using Common.Tools;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BagautdinovKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\41118.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(2));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("41118"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("2772"));
			Assert.That(line.Product, Is.EqualTo("Мин.вода \"Нарзан\"  природ.газ. ПЭТ 1,8 л*6"));
			Assert.That(line.Producer, Is.Null);
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.SupplierCost, Is.EqualTo(44.8));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(37.97));
			Assert.That(line.Period, Is.Null);
			Assert.That(line.VitallyImportant, Is.Null);
			Assert.That(line.Nds.Value, Is.EqualTo(18));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АЯ99.В14241"));
			Assert.That(line.SerialNumber, Is.Null);
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(BagautdinovKazanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\41118.dbf")));
		}
	}
}