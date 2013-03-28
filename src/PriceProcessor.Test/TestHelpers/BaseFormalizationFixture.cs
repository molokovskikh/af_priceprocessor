using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.TestHelpers
{
	public class BaseFormalizationFixture : IntegrationFixture
	{
		protected string file;
		protected TestPrice price;
		protected TestPriceItem priceItem;
		protected string defaultContent;

		protected IPriceFormalizer formalizer;

		[SetUp]
		public void Setup()
		{
			file = "test.txt";
			defaultContent = @"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;
5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;40;44.71;";
		}

		protected void CreatePrice()
		{
			price = TestSupplier.CreateTestSupplierWithPrice(p => Configure(p));
			var regionalData = new TestPriceRegionalData {
				BaseCost = price.Costs.Single(),
				Region = session.Load<TestRegion>((ulong)1),
				Price = price
			};
			price.RegionalData.Add(regionalData);
			priceItem = price.Costs.First().PriceItem;

			session.Save(regionalData);
			Flush();
		}

		protected void FormalizeDefaultData()
		{
			CreateDefaultSynonym();
			Formalize(defaultContent);
		}

		public void CreateDefaultSynonym()
		{
			price.AddProductSynonym("911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ");
			price.CreateAssortmentBoundSynonyms(
				"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г",
				"Санкт-Петербургская ф.ф.");
			price.CreateAssortmentBoundSynonyms(
				"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ",
				"Валента Фармацевтика/Королев Ф");
			session.Save(price);
			session.Flush();
			session.Transaction.Commit();
		}

		protected void Price(string contents)
		{
			File.WriteAllText(file, contents, Encoding.GetEncoding(1251));
		}

		protected void Formalize(string content)
		{
			Price(content);
			Formalize();
		}

		protected void Formalize()
		{
			session.Flush();
			if (session.Transaction.IsActive)
				session.Transaction.Commit();

			if (formalizer == null)
				formalizer = CreateFormalizer();

			formalizer.Formalize();
		}

		private IPriceFormalizer CreateFormalizer()
		{
			var table = PricesValidator.LoadFormRules(priceItem.Id);
			var row = table.Rows[0];
			var localPrice = session.Load<Price>(price.Id);
			var info = new PriceFormalizationInfo(row, localPrice);
			var reader = new PriceReader(new TextParser(new DelimiterSlicer(";"), Encoding.GetEncoding(1251), -1), file, info);
			return new FakeFormalizer(new BasePriceParser2(reader, info));
		}

		protected TestFormat Configure(TestPrice okpPrice)
		{
			priceItem = okpPrice.Costs[0].PriceItem;
			var rules = priceItem.Format;
			rules.PriceFormat = PriceFormatType.NativeDelimiter1251;
			rules.Delimiter = ";";
			rules.FName1 = "F1";
			rules.FFirmCr = "F2";
			rules.FQuantity = "F3";
			okpPrice.Costs.Single().FormRule.FieldName = "F4";
			return rules;
		}

		public class FakeFormalizer : IPriceFormalizer
		{
			private BasePriceParser2 parser;

			public FakeFormalizer(BasePriceParser2 parser)
			{
				this.parser = parser;
			}

			public void Formalize()
			{
				parser.Formalize();
			}

			public IList<string> GetAllNames()
			{
				throw new System.NotImplementedException();
			}

			public bool Downloaded { get; set; }
			public string InputFileName { get; set; }
			public int formCount { get; private set; }
			public int unformCount { get; private set; }
			public int zeroCount { get; private set; }
			public int forbCount { get; private set; }
			public int maxLockCount { get; private set; }
			public long priceCode { get; private set; }
			public long firmCode { get; private set; }
			public string firmShortName { get; private set; }
			public string priceName { get; private set; }
		}
	}
}