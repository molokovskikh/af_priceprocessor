using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SmileParserFixture
	{
		[Test]
		public void Parse()
		{			
			var doc = WaybillParser.Parse("3103_385.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(4));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("РНN11-014385"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("31.03.2011"));			
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Нестожен-1 сухая молочная смесь 350г"));
			Assert.That(line.Code, Is.EqualTo("11242"));
			Assert.That(line.Producer, Is.EqualTo("Нестле Россия ООО"));
			Assert.That(line.Country, Is.EqualTo("RU"));
			Assert.That(line.Quantity, Is.EqualTo(8));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(146.73));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0.00));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("С-СН.АЯ46.В51338"));			
			Assert.That(line.Period, Is.EqualTo("31.01.2013"));
			Assert.That(line.Amount, Is.EqualTo(1291.2));
			Assert.That(line.NdsAmount, Is.EqualTo(117.36));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(SmileParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\3103_385.dbf")));
		}
	}
}
