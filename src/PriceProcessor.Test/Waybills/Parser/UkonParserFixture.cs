using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using System;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class UkonParserFixture
	{
		[Test]
		public void Parse()
		{
			var parser = new UkonParser();
			var doc = new Document();
			parser.Parse(@"..\..\Data\Waybills\0004076.sst", doc);
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("0000004076"));

			Assert.That(doc.Lines[0].Product, Is.EqualTo("Солодкового корня сироп фл.100 г"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("201109^РОСС RU.ФМ05.Д11132^01.12.11201109^74-2347154^25.11.09 ГУЗ ОЦСККЛ г. Челябинск"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.12.11"));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			
			Assert.That(doc.Lines[1].Product, Is.EqualTo("Эвкалипта настойка фл.25 мл"));
			Assert.That(doc.Lines[1].Certificates, Is.EqualTo("151209^РОСС ФМ05.Д36360^01.12.14151209^74-2370989^18.01.10 ГУЗ ОЦСККЛ г. Челябинск"));
			Assert.That(doc.Lines[1].Period, Is.EqualTo("01.12.14"));
			Assert.That(doc.Lines[1].Nds.Value, Is.EqualTo(10));
		}

		[Test]
		public void ParseWithMultilineComments()
		{
			var parser = new UkonParser();
			var doc = new Document();
			parser.Parse(@"..\..\Data\Waybills\3645763_ОАС(114504).sst", doc);
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("114504"));

			Assert.That(doc.Lines[0].Product, Is.EqualTo("Трамадол р-р д/и 50мг/мл 2мл амп N5x1 МЭЗ РОС"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("461009^РОСС RU.ФМ01.Д91475^01.03.2010 ФГУ \"ЦЭККМП\" Росздравнадзор^01.11.2012"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo(""));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));

			Assert.That(doc.Lines[1].Product, Is.EqualTo("Трамадол р-р д/и 50мг/мл 2мл амп N5x1 МЭЗ РОС"));
			Assert.That(doc.Lines[1].Certificates, Is.EqualTo("461009^РОСС RU.ФМ01.Д91475^01.03.2010 ФГУ \"ЦЭККМП\" Росздравнадзор^01.11.2012"));
			Assert.That(doc.Lines[1].Period, Is.EqualTo(""));
			Assert.That(doc.Lines[1].Nds.Value, Is.EqualTo(10));
		}

		[Test]
		public void Parse_without_header()
		{
			var parser = new UkonParser();
			var doc = new Document();
			try
			{
				parser.Parse(@"..\..\Data\Waybills\without_header.sst", doc);
				Assert.Fail("Должны были выбросить исключение");
			}
			catch (Exception e)
			{
				Assert.That(e.Message, Text.Contains("Не найден заголовок накладной"));
			}
		}

		[Test]
		public void Parse_only_comments()
		{
			var parser = new UkonParser();
			var doc = new Document();
			try
			{
				parser.Parse(@"..\..\Data\Waybills\only_comments.sst", doc);
				Assert.Fail("Должны были выбросить исключение");
			}
			catch (Exception e)
			{
				Assert.That(e.Message, Text.Contains("Не найден заголовок накладной"));
			}
		}

		[Test]
		public void Parse_without_body()
		{
			var parser = new UkonParser();
			var doc = new Document();
			try
			{
				parser.Parse(@"..\..\Data\Waybills\without_body.sst", doc);
				Assert.Fail("Должны были выбросить исключение");
			}
			catch (Exception e)
			{
				Assert.That(e.Message, Text.Contains("Не найдено тело накладной"));
			}
		}
	}
}
