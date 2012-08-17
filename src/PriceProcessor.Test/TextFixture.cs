using System;
using System.Data;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("некорректно работают, т.к. завязаны на синонимы поставщиков и формализуются неполностью")]
	public class TextFixture
	{
		[SetUp]
		public void Setup()
		{
			SystemTime.Now = () => new DateTime(2009, 08, 21);
		}

		[TearDown]
		public void TearDown()
		{
			SystemTime.Reset();
		}

		[Test, Ignore]
		public void Formilize_1251_delimiter_price()
		{
			Run<DelimiterNativeTextParser1251>(104);
		}

		[Test, Ignore]
		public void Formilize_866_delimiter_price()
		{
			Run<DelimiterNativeTextParser866>(362);
		}

		[Test, Ignore]
		public void Formilize_1251_fixed_price()
		{
			Run<FixedNativeTextParser1251>(217);
		}

		[Test, Ignore]
		public void Formilize_866_fixed_price()
		{
			Run<FixedNativeTextParser866>(138);
		}

		[Test, Ignore]
		public void Clean_up_input_string()
		{
			var priceItemId = 203;
			Run<FixedNativeTextParser866>(priceItemId);
		}

		[Test, Ignore]
		public void Ignore_lines_with_not_enough_columns()
		{
			var priceItemId = 688;
			Run(priceItemId);
		}

		[Test, Ignore]
		public void Formilize_price_with_wrong_start_line()
		{
			var priceItemId = 348;
			Run(priceItemId);
		}

		private void Run(int priceItemId)
		{
			var file = String.Format(@"..\..\Data\{0}.txt", priceItemId);

			var etalon = new DataTable();
			etalon.ReadXml(String.Format(@"..\..\Data\{0}-etalon.xml", priceItemId));

			var rules = new DataTable();
			rules.ReadXml(String.Format(@"..\..\Data\{0}-rules.xml", priceItemId));

			TestHelper.Execute(String.Format("update usersettings.PriceItems set RowCount = 0 where id = {0}", priceItemId));
			TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);
			TestHelper.Verify(priceItemId.ToString(), etalon);
		}

		private void Run<T>(int priceItemId)
		{
			var file = String.Format(@"..\..\Data\{0}.txt", priceItemId);

			var etalon = new DataTable();
			etalon.ReadXml(String.Format(@"..\..\Data\{0}-etalon.xml", priceItemId));

			var rules = new DataTable();
			rules.ReadXml(String.Format(@"..\..\Data\{0}-rules.xml", priceItemId));

			TestHelper.Execute(String.Format("update usersettings.PriceItems set RowCount = 0 where id = {0}", priceItemId));
			TestHelper.Formalize(typeof(T), rules, file, priceItemId);
			TestHelper.Verify(priceItemId.ToString(), etalon);
		}

		[Test, Ignore("это не тест")]
		public void Save_data()
		{
			var priceItemId = 203;
			var pricecode = TestHelper.Fill(String.Format(@"
select pricecode 
from usersettings.pricescosts
where priceitemid = {0}",
				priceItemId)).Tables[0].Rows[0][0];

			var costcode = TestHelper.Fill(String.Format(@"
select costcode
from usersettings.pricescosts
where priceitemid = {0}",
				priceItemId)).Tables[0].Rows[0][0];

			var etalonCore0 = TestHelper.Fill(String.Format(@"
select c.*, cc.Cost
from core0_copy c
  join corecosts_copy cc on cc.Core_Id = c.Id
where c.pricecode = {0} and cc.pc_costcode = {1};",
				pricecode, costcode)).Tables[0];

			etalonCore0.WriteXml(String.Format(@"..\..\Data\{0}-etalon.xml", priceItemId), XmlWriteMode.WriteSchema);

			var rules = PricesValidator.LoadFormRules((uint)priceItemId);
			rules.WriteXml(String.Format(@"..\..\Data\{0}-rules.xml", priceItemId), XmlWriteMode.WriteSchema);
		}
	}
}