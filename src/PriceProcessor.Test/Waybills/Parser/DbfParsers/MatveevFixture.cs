using System;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class MatveevFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"doc00046.DBF");

			Assert.AreEqual("MatveevParser", document.Parser);
			Assert.AreEqual(new DateTime(2013, 10, 3), document.DocumentDate);
			Assert.AreEqual("ИП000000046", document.ProviderDocumentId);
			var line = document.Lines[0];
			Assert.AreEqual("00000000072", line.Code);
			Assert.AreEqual("4607043181190", line.EAN13);
			Assert.AreEqual(22.10, line.SupplierCostWithoutNDS);
			Assert.AreEqual(1, line.Quantity);
			Assert.AreEqual("Вафли \"ВЕРЕСК\" Какао-шоколадные на сорбите 105 гр. /30", line.Product);
			Assert.AreEqual("ООО \"Вереск\"", line.Producer);
			Assert.AreEqual(new DateTime(2013, 08, 06), line.DateOfManufacture);
		}
	}
}