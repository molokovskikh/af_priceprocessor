п»їusing System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class DetstvoOmskParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("3905490_Р”РµС‚СЃС‚РІРѕ(11038-04.06.10).txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(5));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("11038"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("04.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("141455"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("MAMASENSE РќРђР‘РћР  РћР‘РЈР§ Р›РћР–РљРђ Р