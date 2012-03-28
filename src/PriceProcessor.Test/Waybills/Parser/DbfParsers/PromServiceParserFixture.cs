using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	public class PromServiceParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(PromServiceParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\4376.DBF")));
			var document = WaybillParser.Parse("4376.DBF");

			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("23.03.2012"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("15796"));
			Assert.That(line.Product, Is.EqualTo("Грин Слим Ти  чай 2г №30 ф/п (БАД)"));
			Assert.That(line.Producer, Is.EqualTo("Польша"));
			Assert.That(line.SupplierCost, Is.EqualTo(38.43));
			Assert.That(line.ProducerCost, Is.EqualTo(31.25));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Amount, Is.EqualTo(76.86));
			Assert.That(line.NdsAmount, Is.EqualTo(11.72));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.SerialNumber, Is.EqualTo("11"));
			Assert.That(line.Period, Is.EqualTo("01.12.2013"));
		}
	}
}
