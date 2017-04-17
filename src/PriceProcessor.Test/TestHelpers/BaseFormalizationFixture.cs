using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.TestHelpers
{
	public class BaseFormalizationFixture : IntegrationFixture
	{
		protected string file;
		protected TestSupplier supplier;
		protected TestPrice price;
		protected TestPriceItem priceItem;
		protected string defaultContent;

		protected IPriceFormalizer formalizer;
		protected bool Downloaded;

		[SetUp]
		public void BaseSetup()
		{
			Downloaded = false;
			formalizer = null;
			file = "test.txt";
			defaultContent = @"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;1;
5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;0;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;40;44.71;1;";
		}

		protected void CreatePrice()
		{
			supplier = TestSupplier.CreateNaked();
			price = supplier.Prices[0];
			priceItem = price.Costs.First().PriceItem;
			Configure(price);

			session.Save(supplier);
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

			formalizer.Downloaded = Downloaded;
			formalizer.Formalize();
			formalizer = null;
		}

		private IPriceFormalizer CreateFormalizer()
		{
			var table = PricesValidator.LoadFormRules(priceItem.Id);
			var row = table.Rows[0];
			var localPrice = session.Load<Price>(price.Id);
			var info = new PriceFormalizationInfo(row, localPrice);
			return new BufferFormalizer(file, info);
		}

		protected TestFormat Configure(TestPrice okpPrice)
		{
			priceItem = okpPrice.Costs[0].PriceItem;
			var rules = priceItem.Format;
			rules.PriceFormat = PriceFormatType.NativeDelim;
			rules.Delimiter = ";";
			rules.FName1 = "F1";
			rules.FFirmCr = "F2";
			rules.FQuantity = "F3";
			rules.FOptimizationSkip = "F6";

			okpPrice.Costs.Single().FormRule.FieldName = "F4";
			return rules;
		}
	}
}