using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using System;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class ProtekParserFixture
	{
		private ProtekParser _parser;
		private Document _document;

		[SetUp]
		public void SetUp()
		{
			_parser = new ProtekParser();
			_document = new Document();
		}

		[Test]
		public void Parse()
		{
			var document = _parser.Parse(@"..\..\Data\Waybills\1008fo.pd", _document);
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("25.01.10")));
		}
	}
}