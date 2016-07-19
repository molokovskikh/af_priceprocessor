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

		/// <summary>
		/// Для задачи
		/// http://redmine.analit.net/issues/28711
		/// </summary>
		[Test]
		public void ParseOrder()
		{
			Assert.IsTrue(SiaAstrahanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\410442.dbf")));

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\410442.dbf");

			Assert.That(document.Lines[0].Code, Is.EqualTo("58069217"));
			Assert.That(document.Lines[0].OrderId, Is.EqualTo(63116419));
		}

		/// <summary>
		/// Для задачи
		/// http://redmine.analit.net/issues/52098
		/// </summary>
		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"397173.dbf");
			Assert.AreEqual(document.Lines[0].EAN13, "9006968004006");
		}

	}
}
