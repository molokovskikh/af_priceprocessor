using System.IO;
using System.Linq;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	internal class RequestTest : IntegrationFixture
	{
		private TestPriceItem priceItem;
		private string basepath;

		[SetUp]
		public void Setup()
		{
			basepath = Settings.Default.BasePath;
			if (!Directory.Exists(basepath))
				Directory.CreateDirectory(basepath);
		}

		[TearDown]
		public void Teardown()
		{
			Directory.Delete(basepath, true);
		}

		[Test]
		public void GetAllNamesTest()
		{
			var supplier = TestSupplier.CreateNaked();
			var price = supplier.Prices[0];

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

			session.Save(price);
			Close();

			File.Copy(Path.GetFullPath(@"..\..\Data\222.txt"), Path.Combine(basepath, priceItem.Id + ".txt"));

			var item = PriceProcessItem.GetProcessItem(priceItem.Id);
			var names = item.GetAllNames();
			Assert.That(names.Count(), Is.EqualTo(35));
		}

		[Test]
		public void Respect_file_extension()
		{
			var supplier = TestSupplier.CreateNaked();
			var price = supplier.Prices[0];

			priceItem = price.Costs.First().PriceItem;
			var format = price.Costs.Single().PriceItem.Format;
			format.PriceFormat = PriceFormatType.UniversalXml;
			session.Save(price);
			Close();

			File.Copy(Path.GetFullPath(@"..\..\Data\222.txt"), Path.Combine(basepath, priceItem.Id + ".txt"));
			File.Copy(Path.GetFullPath(@"..\..\Data\Respect_file_extension.xml"), Path.Combine(basepath, priceItem.Id + ".xml"));
			var item = PriceProcessItem.GetProcessItem(priceItem.Id);
			var names = item.GetAllNames();
			Assert.That(names.Count(), Is.EqualTo(2));
		}
	}
}