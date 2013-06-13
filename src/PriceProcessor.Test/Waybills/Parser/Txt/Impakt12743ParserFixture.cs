using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class Impakt12743ParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(Impakt12743Parser.CheckFileFormat(@"..\..\Data\Waybills\СКИМП011737.txt"));
			var doc = WaybillParser.Parse("СКИМП011737.txt");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("СКИМП011737"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2012, 07, 18)));
			var invoice = doc.Invoice;
			Assert.That(invoice.Amount, Is.EqualTo(1334.77m));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("1067"));
			Assert.That(line.Product, Is.EqualTo("Бинт Lastotel                4х400"));
			Assert.That(line.Producer, Is.EqualTo("Пауль Хартман"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Unit, Is.EqualTo("шт"));
			Assert.That(line.SupplierCost, Is.EqualTo(14.96));
			Assert.That(line.Amount, Is.EqualTo(14.96));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(1.36));
		}
	}
}
