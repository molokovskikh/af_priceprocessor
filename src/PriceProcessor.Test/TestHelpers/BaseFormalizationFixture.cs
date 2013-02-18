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
		private BasePriceParser2 formalizer;

		[SetUp]
		public void Setup()
		{
			file = "test.txt";
		}

		protected void CreatePrice()
		{
			price = TestSupplier.CreateTestSupplierWithPrice(p => {
				var rules = p.Costs.Single().PriceItem.Format;
				rules.PriceFormat = PriceFormatType.NativeDelimiter1251;
				rules.Delimiter = ";";
				rules.FName1 = "F1";
				rules.FFirmCr = "F2";
				rules.FQuantity = "F3";
				p.Costs.Single().FormRule.FieldName = "F4";
			});
			var regionalData = new TestPriceRegionalData {
				BaseCost = price.Costs.Single(),
				Region = session.Load<TestRegion>((ulong)1),
				Price = price
			};
			session.Save(regionalData);
			price.RegionalData.Add(regionalData);
			priceItem = price.Costs.First().PriceItem;
			Flush();
		}

		protected void FormalizeDefaultData()
		{
			CreateDefaultSynonym();

			Formalize(@"9 ������� ���� �/������������ � ��������� �������� 150��;������� ������������/������� �;2864;220.92;
5 ���� ����� �/��� ���������� �10 ���. 25�;�����-������������� �.�.;24;73.88;
911 �������� ���� �/ ��� ��� ������� ���� � ������ ���� 100��;����� ���;40;44.71;");
		}

		public void CreateDefaultSynonym()
		{
			price.AddProductSynonym("911 �������� ���� �/ ��� ��� ������� ���� � ������ ���� 100��");
			price.CreateAssortmentBoundSynonyms(
				"5 ���� ����� �/��� ���������� �10 ���. 25�",
				"�����-������������� �.�.");
			price.CreateAssortmentBoundSynonyms(
				"9 ������� ���� �/������������ � ��������� �������� 150��",
				"������� ������������/������� �");
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
			if (session.Transaction.IsActive)
				session.Transaction.Commit();

			var table = PricesValidator.LoadFormRules(priceItem.Id);
			var row = table.Rows[0];
			var localPrice = session.Load<Price>(price.Id);
			var info = new PriceFormalizationInfo(row, localPrice);
			var reader = new PriceReader(row, new TextParser(new DelimiterSlicer(";"), Encoding.GetEncoding(1251), -1), file, info);
			formalizer = new BasePriceParser2(reader, info);
			formalizer.Formalize();
		}
	}
}