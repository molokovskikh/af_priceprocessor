using System;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using Inforoom.PriceProcessor.Formalizer;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;
using Inforoom.Common;
using FileHelper = Inforoom.Common.FileHelper;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	class RequestTest
	{
		private TestPrice price;
		private TestPriceItem priceItem;

		[Test]
		[Ignore("Починить")]
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

		[Test]
		public void GetAllNamesTest()
		{         
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				price = TestSupplier.CreateTestSupplierWithPrice(p =>
				{
					var rules = p.Costs.Single().PriceItem.Format;
					rules.PriceFormat = PriceFormatType.NativeDelimiter1251;
					rules.Delimiter = ";";
					rules.FName1 = "F2";
					rules.FFirmCr = "F3";
					rules.FQuantity = "F5";                    
					p.Costs.Single().FormRule.FieldName = "F4";
					rules.FRequestRatio = "F6";
					p.ParentSynonym = 5;
				});
				priceItem = price.Costs.First().PriceItem;
				scope.VoteCommit();
			}
			string basepath = FileHelper.NormalizeDir(Settings.Default.BasePath);
			if (!Directory.Exists(basepath)) Directory.CreateDirectory(basepath);

			File.Copy(Path.GetFullPath(@"..\..\Data\222.txt"), Path.GetFullPath(@"..\..\Data\2222.txt"));       
			File.Move(Path.GetFullPath(@"..\..\Data\2222.txt"), Path.GetFullPath(String.Format(@"{0}{1}.txt", basepath, priceItem.Id)));
					   
			PriceProcessItem item = PriceProcessItem.GetProcessItem(priceItem.Id);
			var names = item.GetAllNames();
			File.Delete(Path.GetFullPath(String.Format(@"{0}{1}.txt", basepath, priceItem.Id)));
			Assert.That(names.Count(), Is.EqualTo(35));
		}

		[Test]
		public void GetFileTest()
		{
			string[] files = new string[] {"file1.txt", "file2", "file3.dbf", "file5.xls"};

			Assert.That(PriceProcessItem.GetFile(files, FormatType.NativeDelimiter1251), Is.EqualTo("file1.txt"));
			Assert.That(PriceProcessItem.GetFile(files, FormatType.NativeXls), Is.EqualTo("file5.xls"));
			Assert.That(PriceProcessItem.GetFile(files, FormatType.NativeDbf), Is.EqualTo("file3.dbf"));
			Assert.That(PriceProcessItem.GetFile(files, FormatType.Xml), Is.EqualTo("file1.txt"));

			files = new string[] {"file"};
			Assert.That(PriceProcessItem.GetFile(files, FormatType.NativeDelimiter1251), Is.EqualTo("file"));
		}
	}
}
