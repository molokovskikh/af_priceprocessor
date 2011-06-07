using System;
using System.Data;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
    [Ignore("Починить")]
	public class UpdateCoreTimestampsFixture
	{
		private int priceItemId;
		private uint pricecode;

		private DataSet coreData;
		private DataTable core;
		private object originalId;
		
		[SetUp]
		public void Setup()
		{
			Settings.Default.SyncPriceCodes.Clear();
			
			priceItemId = 348;
			pricecode =  Convert.ToUInt32(TestHelper.Fill(String.Format(@"
select pricecode 
from usersettings.pricescosts
where priceitemid = {0}", priceItemId)).Tables[0].Rows[0][0]);

			Settings.Default.SyncPriceCodes.Add(pricecode.ToString());

			TestHelper.Execute("delete from farm.core0 where pricecode = {0}", pricecode);
			TestHelper.Execute("update usersettings.PriceItems set RowCount = 0 where id = {0}", priceItemId);

			TestHelper.Formalize<DelimiterNativeTextParser1251>(@"..\..\Data\UpdateCoreTimeStamp.txt", priceItemId);

			coreData = TestHelper.Fill(String.Format("select * from farm.Core0 where priceCode = {0}", pricecode));
			core = coreData.Tables[0];
			Assert.That(core.Rows.Count, Is.EqualTo(1));
			Assert.That(core.Rows[0]["QuantityUpdate"], Is.EqualTo(DateTime.MinValue));
			Assert.That(core.Rows[0]["UpdateTime"], Is.EqualTo(DateTime.MinValue));
			originalId = core.Rows[0]["Id"];

		}

		[Test]
		public void Update_quantity_time_stamp_if_quantity_updated()
		{
			var begin = DateTime.Now;
			TestHelper.Formalize<DelimiterNativeTextParser1251>(@"..\..\Data\UpdateCoreTimeStamp-update-quantity.txt", priceItemId);

			coreData = TestHelper.Fill(String.Format("select * from farm.Core0 where priceCode = {0}", pricecode));
			core = coreData.Tables[0];

			Assert.That(core.Rows[0]["Id"], Is.EqualTo(originalId));
			Assert.That(core.Rows[0]["Quantity"], Is.EqualTo("5"));
			Assert.That(core.Rows[0]["QuantityUpdate"], Is.GreaterThan(begin));
			Assert.That(core.Rows[0]["UpdateTime"], Is.EqualTo(DateTime.MinValue));
		}

		[Test]
		public void Update_time_stamp_if_quantity_not_updated()
		{
			var begin = DateTime.Now;
			TestHelper.Formalize<DelimiterNativeTextParser1251>(@"..\..\Data\UpdateCoreTimeStamp-update.txt", priceItemId);
			

			coreData = TestHelper.Fill(String.Format("select * from farm.Core0 where priceCode = {0}", pricecode));
			core = coreData.Tables[0];

			Assert.That(core.Rows[0]["Id"], Is.EqualTo(originalId));
			Assert.That(core.Rows[0]["Volume"], Is.EqualTo("351"));
			Assert.That(core.Rows[0]["QuantityUpdate"], Is.EqualTo(DateTime.MinValue));
			Assert.That(core.Rows[0]["UpdateTime"], Is.GreaterThan(begin));
		}
	}
}
