using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	class BahtinParserFixture : DocumentFixture
	{
		[Test]
		public void Parse_Bahtin()
		{
			var parser = new Inforoom.PriceProcessor.Models.Parser("Bahtin_RM60945", appSupplier, EncodingEnum.CP866);
			parser.Add("DOCDAT", "Header_DocumentDate");
			parser.Add("COD", "Code");
			parser.Add("TOVAR", "Product");
			session.Save(parser);

			var ids = new WaybillService().ParseWaybill(new[] { CreateTestLog("000099.DBF").Id });
			var document = session.Load<Document>(ids[0]);
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("27.02.2017")));

			var line = document.Lines[3];
			Assert.That(line.Code, Is.EqualTo("00000070"));
			var product = line.Product;
			Assert.That(product, Is.EqualTo("масло зародышей (ростков) пшеницы 100мл АРС"));
		}
	}
}
