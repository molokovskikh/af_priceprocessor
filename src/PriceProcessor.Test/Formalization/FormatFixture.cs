using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using ExcelLibrary.SpreadSheet;
using Inforoom.Formalizer;
using NHibernate.Linq;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	public class FormatFixture : BaseFormalizationFixture
	{
		[SetUp]
		public void Setup()
		{
			CreatePrice();
		}

		[Test]
		public void Formalize_excel()
		{
			priceItem.Format.PriceFormat = PriceFormatType.Xls;

			var book = new Workbook();
			var worksheet = new Worksheet("test");
			book.Worksheets.Add(worksheet);
			var lines = Data();
			for (var i = 0; i < lines.Length; i++) {
				var parts = lines[i];
				for (var j = 0; j < parts.Length; j++) {
					worksheet.Cells[i, j] = new Cell(parts[j]);
				}
			}
			file = "test.xls";
			book.Save(file);

			AssertFormalization();
		}

		[Test]
		public void Formalize_dbf()
		{
			priceItem.Format.PriceFormat = PriceFormatType.Dbf;
			file = "test.dbf";

			var table = new DataTable();
			table.Columns.Add("F1");
			table.Columns.Add("F2");
			table.Columns.Add("F3");
			table.Columns.Add("F4");

			var lines = Data();
			for (var i = 0; i < lines.Length; i++) {
				var parts = lines[i];
				var row = table.NewRow();
				table.Rows.Add(row);
				for (var j = 0; j < parts.Length; j++) {
					row["F" + (j + 1)] = parts[j];
				}
			}

			Dbf.Save(table, file);

			AssertFormalization();
		}

		[Test]
		public void Formalize_fixed()
		{
			priceItem.Format.PriceFormat = PriceFormatType.FixedWIN;
			var lines = Data();
			var maxLengths = Enumerable.Range(0, lines[0].Length)
				.Select(i => lines.Select(l => l[i]).Max(l => l.Length))
				.ToArray();
			priceItem.Format.TxtNameBegin = 1;
			priceItem.Format.TxtNameEnd = priceItem.Format.TxtNameBegin + maxLengths[0] - 1;
			priceItem.Format.TxtFirmCrBegin = priceItem.Format.TxtNameEnd;
			priceItem.Format.TxtFirmCrEnd = priceItem.Format.TxtFirmCrBegin + maxLengths[1] - 1;
			priceItem.Format.TxtQuantityBegin = priceItem.Format.TxtFirmCrEnd;
			priceItem.Format.TxtQuantityEnd = priceItem.Format.TxtQuantityBegin + maxLengths[2] - 1;

			var rule = price.Costs.First().FormRule;
			rule.TxtBegin = priceItem.Format.TxtQuantityEnd + 1;
			rule.TxtEnd = rule.TxtBegin + maxLengths[3] - 1;

			var content = lines.Select(p => p.Select((s, i) => s.PadRight(maxLengths[i], ' ')).Implode(""))
				.Implode(Environment.NewLine);
			File.WriteAllText(file, content, Encoding.GetEncoding(1251));

			AssertFormalization();
		}

		private string[][] Data()
		{
			return defaultContent
				.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
				.Select(l => l.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
				.ToArray();
		}

		private void AssertFormalization()
		{
			CreateDefaultSynonym();
			formalizer = PricesValidator.Validate(file, "test1" + Path.GetExtension(file), priceItem.Id);
			Formalize();

			var cores = session.Query<TestCore>().Where(c => c.Price == price).ToList();
			Assert.That(cores.Count, Is.EqualTo(3));
			Assert.That(cores[0].ProductSynonym.Name, Is.EqualTo("9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ"));
		}
	}
}