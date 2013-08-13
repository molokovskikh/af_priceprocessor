using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Formalization
{
	public class TestFormalizer : BufferFormalizer
	{
		public TestFormalizer(string filename,
			PriceFormalizationInfo data) : base(filename, data)
		{
		}

		public Encoding CodePage
		{
			get { return Encoding; }
		}
	}

	public class TestDbfParser : PriceDbfParser
	{
		public TestDbfParser(string filename,
			PriceFormalizationInfo data) : base(filename, data)
		{
		}

		public IParser TestParser
		{
			get { return Parser; }
		}
	}

	public class TestTextParser : DelimiterTextParser
	{
		public TestTextParser(string filename,
			PriceFormalizationInfo data) : base(filename, data)
		{
		}

		public PriceReader TestCreateReader()
		{
			return CreateReader();
		}

		public IParser TestParser
		{
			get { return Parser; }
		}
	}

	[TestFixture]
	public class EncodingFixture
	{
		[Test]
		public void Base_buffer_formalize_fixture()
		{
			var info = new PriceFormalizationInfo();
			var formalizer = new TestFormalizer("test", info);
			Assert.AreEqual(formalizer.CodePage.CodePage, 1251);
			info.CodePage = 866;
			formalizer = new TestFormalizer("test", info);
			Assert.AreEqual(formalizer.CodePage.CodePage, 866);
		}

		[Test]
		public void Price_formalizer_info()
		{
			var row = GetFormRuleRow();
			row[FormRules.colPriceEncode] = 866;
			var info = new PriceFormalizationInfo(row, new Price());
			Assert.AreEqual(info.CodePage, 866);
			row[FormRules.colPriceEncode] = DBNull.Value;
			info = new PriceFormalizationInfo(row, new Price());
			Assert.AreEqual(info.CodePage, 0);
		}

		[Test]
		public void DbfReaderTest()
		{
			CommonReaderTest(testFile => {
				var table = new DataTable("testTable");
				table.Columns.Add("testCollumn");
				var row = table.NewRow();
				row["testCollumn"] = "тестовая информация";
				table.Rows.Add(row);
				Dbf.Save(table, testFile, Encoding.GetEncoding(1251));

				var ruleRow = GetFormRuleRow();
				ruleRow[FormRules.colPriceEncode] = 1251;
				var parser = new TestDbfParser(testFile, new PriceFormalizationInfo(ruleRow, new Price()));
				var resultTable = parser.TestParser.Parse(testFile);
				Assert.AreEqual(resultTable.Rows[0]["TESTCOLLUM"].ToString(), "тестовая информация");

				ruleRow[FormRules.colPriceEncode] = 866;
				parser = new TestDbfParser(testFile, new PriceFormalizationInfo(ruleRow, new Price()));
				resultTable = parser.TestParser.Parse(testFile);
				Assert.AreEqual(resultTable.Rows[0]["TESTCOLLUM"].ToString(), "ЄхёЄютр  шэЇюЁьрЎш ");
			});
		}

		[Test]
		public void TextReaderTest()
		{
			CommonReaderTest(testFile => {
				File.WriteAllText(testFile, "тестовые данные", Encoding.GetEncoding(866));
				var ruleRow = GetFormRuleRow(table => {
					foreach (PriceFields pf in Enum.GetValues(typeof(PriceFields))) {
						var tmpName = (PriceFields.OriginalName == pf) ? "FName1" : "F" + pf;
						if (!table.Columns.Contains(tmpName))
							table.Columns.Add(tmpName);
					}
					table.Columns.Add("StartLine");
				});
				ruleRow[FormRules.colPriceEncode] = 866;
				var parser = new TestTextParser(testFile, new PriceFormalizationInfo(ruleRow, new Price()));
				parser.TestCreateReader();
				var resultTable = parser.TestParser.Parse(testFile);
				Assert.AreEqual(resultTable.Rows[0][0].ToString(), "тестовые данные");

				ruleRow[FormRules.colPriceEncode] = 1251;
				parser = new TestTextParser(testFile, new PriceFormalizationInfo(ruleRow, new Price()));
				parser.TestCreateReader();
				resultTable = parser.TestParser.Parse(testFile);
				Assert.AreEqual(resultTable.Rows[0][0].ToString(), "вҐбв®ўлҐ ¤ ­­лҐ");
			});
		}

		public void CommonReaderTest(Action<string> testAction)
		{
			var testFile = Path.GetFullPath("testDbfData.dbf");
			try {
				testAction(testFile);
			}
			finally {
				File.Delete(testFile);
			}
		}

		private DataRow GetFormRuleRow(Action<DataTable> tableAction = null)
		{
			var table = new DataTable("testTable");
			table.Columns.Add("region");
			table.Columns.Add("CostName");
			var rules = new FormRule();
			var fields = typeof(FormRules).GetFields().Select(fieldInfo => fieldInfo.GetValue(rules));
			foreach (var name in fields) {
				table.Columns.Add(name.ToString());
			}
			if (tableAction != null)
				tableAction(table);
			var row = table.NewRow();
			foreach (var name in fields) {
				row[name.ToString()] = 0;
			}
			table.Rows.Add(row);
			row[FormRules.colFormByCode] = false;
			return row;
		}
	}
}
