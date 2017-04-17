using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Models;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Avesta_6256_SpecialParserFixture
	{
		[Test]
		public void Parse_document_id()
		{
			var doc = WaybillParser.Parse("6172057_Сиа Интернейшнл(Р2346228).DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("6172057_Сиа Интернейшнл(Р2346228)"));

			var doc1 = WaybillParser.Parse("6159795_Сиа Интернейшнл(Р23463543).DBF");
			Assert.That(doc1.ProviderDocumentId, Is.EqualTo("6159795_Сиа Интернейшнл(Р23463543)"));

			var doc2 = WaybillParser.Parse("6161231_Сиа Интернейшнл(Р2346542).DBF");
			Assert.That(doc2.ProviderDocumentId, Is.EqualTo("Р-2346542"));

			var doc3 = WaybillParser.Parse("Р-1098578.DBF");
			Assert.That(doc3.ProviderDocumentId, Is.EqualTo("2"));
		}

		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("761517.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("761517"));
		}

		[Test]
		public void ParseEan13()
		{
			// #48463 Доработка формата накладной для Поставщика Авеста-Фармацевтика, Код 6256
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 6256u } };
			var doc = WaybillParser.Parse("90444.dbf", documentLog);
			var line = doc.Lines[0];
			Assert.That(line.EAN13, Is.EqualTo("4602156000024"));
		}

	}
}