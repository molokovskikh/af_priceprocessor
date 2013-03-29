using System;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer.Core;
using NUnit.Framework;
using Inforoom.PriceProcessor.Formalizer;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;
using FileHelper = Common.Tools.FileHelper;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	internal class RequestTest
	{
		private TestPriceItem priceItem;

		[Test]
		public void GetAllNamesTest()
		{
			var supplier = TestSupplier.Create();
			var price = supplier.Prices[0];
			price.CostType = CostType.MultiColumn;

			priceItem = price.Costs.First().PriceItem;
			var format = price.Costs.Single().PriceItem.Format;
			format.PriceFormat = PriceFormatType.NativeDelimiter1251;
			format.Delimiter = ";";
			format.FName1 = "F2";
			format.FFirmCr = "F3";
			format.FQuantity = "F5";
			format.FRequestRatio = "F6";
			var costFormRule = price.Costs.Single().FormRule;
			costFormRule.FieldName = "F4";

			price.Save();

			var basepath = Settings.Default.BasePath;
			if (!Directory.Exists(basepath))
				Directory.CreateDirectory(basepath);

			var source = Path.GetFullPath(@"..\..\Data\222.txt");
			var destination = Path.GetFullPath(Path.Combine(basepath, priceItem.Id + ".txt"));
			File.Copy(source, destination);

			var item = PriceProcessItem.GetProcessItem(priceItem.Id);
			var names = item.GetAllNames();
			File.Delete(destination);
			Assert.That(names.Count(), Is.EqualTo(35));
		}

		[Test]
		public void GetFileTest()
		{
			var files = new[] { "file1.txt", "file2", "file3.dbf", "file5.xls" };

			Assert.That(PriceProcessItem.GetFile(files, FormatType.NativeDelimiter1251), Is.EqualTo("file1.txt"));
			Assert.That(PriceProcessItem.GetFile(files, FormatType.NativeXls), Is.EqualTo("file5.xls"));
			Assert.That(PriceProcessItem.GetFile(files, FormatType.NativeDbf), Is.EqualTo("file3.dbf"));
			Assert.That(PriceProcessItem.GetFile(files, FormatType.Xml), Is.EqualTo("file1.txt"));

			files = new[] { "file" };
			Assert.That(PriceProcessItem.GetFile(files, FormatType.NativeDelimiter1251), Is.EqualTo("file"));
		}
	}
}