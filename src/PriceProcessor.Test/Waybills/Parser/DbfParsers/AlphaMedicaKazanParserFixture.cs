using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class AlphaMedicaKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("2710_368.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00000002710"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("03.06.2011"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00000000023"));
			Assert.That(line.Product, Is.EqualTo("Изм. арт. давл. BP AG1-20 механ. стандарт, стет в компл."));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo(null));
			Assert.That(line.Country, Is.EqualTo("Китай"));
			Assert.That(line.Producer, Is.Null);
			Assert.That(line.Period, Is.Null);
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(354.00));
			Assert.That(line.SupplierCost, Is.EqualTo(354.00));
			Assert.That(line.Amount, Is.EqualTo(354.00));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.NdsAmount, Is.EqualTo(0.00));
			Assert.That(line.SerialNumber, Is.EqualTo("10130060/020311/0004013, Китай"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(AlphaMedicaKazanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\2710_368.dbf")));
		}
	}
}