using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SiaAstrahanParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(SiaAstrahanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\172460.dbf")));

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\172460.dbf");

			Assert.AreEqual(document.Lines[0].EAN13, "4013054007792");
			Assert.AreEqual(document.Lines[0].CodeCr, "18585155");
		}
	}
}
