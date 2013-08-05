using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using Common.MySql;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	public class NativeDbfPriceParcerFixture : IntegrationFixture
	{
		private TestPrice _testPrice;

		[SetUp]
		public void SetUp()
		{
			var supplier = TestSupplier.Create();
			_testPrice = supplier.Prices[0];
			_testPrice.Costs[0].FormRule.FieldName = "name";
			_testPrice.Costs[0].PriceItem.Format.PriceEncode = 1251;
			session.Save(_testPrice);
			session.Save(_testPrice.Costs[0].PriceItem.Format);
		}

		[Test]
		public void FormalizeWithStrictTest()
		{
			var rules = PricesValidator.LoadFormRules(_testPrice.Costs[0].PriceItem.Id);
			var row = rules.NewRow();
			row[FormRules.colFirmCode] = 0;
			row[FormRules.colPriceCode] = _testPrice.Id;
			row[FormRules.colFormByCode] = 0;
			row[FormRules.colCostType] = _testPrice.CostType;
			row[FormRules.colPriceType] = _testPrice.PriceType;
			row[FormRules.colPriceItemId] = _testPrice.Costs[0].PriceItem.Id;
			row[FormRules.colParentSynonym] = 0;
			row["BuyingMatrix"] = 0;
			row["PriceEncode"] = _testPrice.Costs[0].PriceItem.Format.PriceEncode;
			rules.Rows.Add(row);
			var price = session.Query<Price>().FirstOrDefault(p => p.Id == _testPrice.Id);
			price.IsStrict = false;
			session.Save(price);
			Reopen();
			var info = new PriceFormalizationInfo(row, price);
			var parser = new PriceDbfParser(@"..\..\Data\BadTestFile.dbf", info);
			parser.Formalize();
			Assert.AreEqual(info.CodePage, 1251);
		}
	}
}
