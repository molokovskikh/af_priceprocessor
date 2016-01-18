using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using PriceProcessor.Test.TestHelpers;

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

		// Требование 42661 для Поставщика Фарматика-МК, Код 18481
		[Test]
		public void ParseFarmaticaMkPenza()
		{
			var doc = WaybillParser.Parse("137.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("137"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("21.12.2015"));
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Метформин таб 1000мг №60"));
			Assert.That(line.Code, Is.EqualTo("25296"));
			Assert.That(line.Producer, Is.EqualTo("Озон ООО"));
			Assert.That(line.Country, Is.EqualTo("Россия (Российская Федерация)"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(203.82));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(210.6));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.EqualTo("1300715"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д97922"));
			Assert.That(line.Period, Is.EqualTo("01.08.2018"));
			Assert.That(line.Amount, Is.EqualTo(224.2));
			Assert.That(line.NdsAmount, Is.EqualTo(20.38));

			Assert.That(line.RegistryCost, Is.EqualTo(210.6));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(SmileParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\3103_385.dbf")));
		}
	}
}