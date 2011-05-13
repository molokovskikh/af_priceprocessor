using System;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using NUnit.Framework;
using Inforoom.PriceProcessor.Formalizer;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	class RequestTest
	{
		private TestPrice price;
		private TestPriceItem priceItem;

		[Test]
		public void Test()
		{
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				price = TestSupplier.CreateTestSupplierWithPrice(p =>
				{
					var rules = p.Costs.Single().PriceItem.Format;
					rules.PriceFormat = PriceFormatType.NativeDelimiter1251;
					rules.Delimiter = ";";
					rules.FName1 = "F1";
					rules.FFirmCr = "F2";
					rules.FQuantity = "F3";
					p.Costs.Single().FormRule.FieldName = "F4";
					rules.FRequestRatio = "F5";
					p.ParentSynonym = 5;
				});
				priceItem = price.Costs.First().PriceItem;
				scope.VoteCommit();
			}
			File.Copy(Path.GetFullPath(@".\Data\1222.txt"), Path.GetFullPath(@".\Data\222.txt"));
			File.Move(Path.GetFullPath(@".\Data\222.txt"), Path.GetFullPath(String.Format(@".\Data\{0}.txt", priceItem.Id)));
			TestHelper.Formalize<DelimiterNativeTextParser1251>(Path.GetFullPath(String.Format(@".\Data\{0}.txt", priceItem.Id)));

			var data = TestHelper.Fill
				(String.Format(@"select * from farm.core0 c where pricecode = (select pricecode from usersettings.PricesCosts P where priceitemid = {0})", priceItem.Id));
			Assert.That(data.Tables[0].Rows[0]["RequestRatio"], Is.EqualTo(DBNull.Value));
			Assert.That(data.Tables[0].Rows[1]["RequestRatio"], Is.EqualTo(110));
			Assert.That(data.Tables[0].Rows[2]["RequestRatio"], Is.EqualTo(245));
		}

	}
}
