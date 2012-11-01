using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Common.MySql;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
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
			session.Save(_testPrice);
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
			rules.Rows.Add(row);
			var price = session.Query<Price>().FirstOrDefault(p => p.Id == _testPrice.Id);
			price.IsStrict = false;
			session.Save(price);
			Reopen();
			var connection = new MySqlConnection(ConnectionHelper.GetConnectionString());
			var parser = new NativeDbfPriceParser(@"..\..\Data\BadTestFile.dbf",
				connection,
				new PriceFormalizationInfo(row, price));

			Assert.DoesNotThrow(parser.Open);
		}
	}
}
