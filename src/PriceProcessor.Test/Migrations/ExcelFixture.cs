using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using ExcelLibrary.SpreadSheet;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using log4net.Config;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("только для отладки Романом")]
	public class ExcelFixture
	{
		private List<TestPriceItem> items;
		private uint[] ids;
		private string path =  "excel";

		[SetUp]
		public void Setup()
		{
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			using(new SessionScope())
			{
				items = TestPriceItem.Queryable.Where(i => i.Format.PriceFormat == PriceFormatType.Xls).ToList();
				ids = items.Select(i => i.Id).ToArray();
			}
		}

		[Test, Ignore("не работает, т.к. нужны были для проверки формализации новых форматов и сравнения со старыми")]
		public void Verify()
		{
			foreach (var id in ids)
			{
				try
				{
					var file = Path.Combine(path, String.Format("{0}.xls", id));
					if (!File.Exists(file))
						continue;
					Console.WriteLine(DateTime.Now + " - " + file);
					var priceItemId = Path.GetFileNameWithoutExtension(file);
					TestHelper.Execute(String.Format("update usersettings.PriceItems set RowCount = 0 where id = {0}", priceItemId));
					TestHelper.Formalize<NativeExcelParser>(Path.GetFullPath(file));
					TestHelper.Verify(priceItemId);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}

		[Test, Ignore("не работает, т.к. нужны были для проверки формализации новых форматов и сравнения со старыми")]
		public void Prepare()
		{
			foreach (var id in ids)
			{
				try
				{
					var file = Path.Combine(path, String.Format("{0}.xls", id));
					if (!File.Exists(file))
						continue;
					Console.WriteLine(DateTime.Now + " - " + file);
					var priceItemId = Path.GetFileNameWithoutExtension(file);
					TestHelper.Execute(String.Format("update usersettings.PriceItems set RowCount = 0 where id = {0}", priceItemId));
					TestHelper.Formalize<ExcelPriceParser>(Path.GetFullPath(file));
					TestHelper.Execute(@"
select PriceCode
into @PriceCode
from usersettings.PricesCosts
where priceItemId = {0}
group by PriceCode;

delete cc from CoreCosts_copy cc
join core0_copy c on c.Id = cc.Core_id
where c.PriceCode = @PriceCode;

delete c from core0_copy c
where c.PriceCode = @PriceCode;

insert into farm.core0_copy
SELECT c.* 
FROM farm.Core0 C
where pricecode = @PriceCode;

insert into farm.CoreCosts_copy
SELECT cc.* FROM farm.Core0 C
join CoreCosts cc on cc.Core_Id = c.Id
where pricecode = @PriceCode;

", id);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}


		[Test, Ignore("Не тест служит для того что бы подготовить эталанное данные")]
		public void Prepare_etalon_data()
		{
			if (Directory.Exists(path))
				Directory.Delete(path, true);
			Directory.CreateDirectory(path);

			foreach (var item in items)
			{
				var price = String.Format(@"\\fms.adc.analit.net\prices\base\{0}.xls", item.Id);
				if (File.Exists(price))
					File.Copy(price, Path.Combine(path, String.Format("{0}.xls", item.Id)));
			}

/*			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				connection.Open();
				var adapter = new MySqlDataAdapter(@"
select *
from farm.core0 c
  join corecosts cc on c.id = cc.Core_Id
where pricecode = 3331", connection);
				var data = new DataSet();
				adapter.Fill(data);
				using (var file = File.Create("552.xml"))
				{
					data.WriteXml(file, XmlWriteMode.WriteSchema);
				}
			}*/
		}

		[Test]
		public void Try_to_formalize_ole()
		{
			var files = Directory.GetFiles(Path.GetFullPath(@"..\..\Data\Excel\"));
			foreach (var file in files)
			{
				try
				{
					Console.WriteLine(DateTime.Now + " - " + file);
					var priceItemId = Path.GetFileNameWithoutExtension(file);
					TestHelper.Execute(String.Format("update usersettings.PriceItems set RowCount = 0 where id = {0}", priceItemId));
					TestHelper.Formalize<ExcelPriceParser>(Path.GetFullPath(file));
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}

		[Test]
		public void test()
		{
			//var format = "[Белый][<1] ММ.ГГ; ММ.ГГ";
			//Assert.That(NativeExcelParser.IsADateFormat(format), Is.True);

			Console.WriteLine(Regex.Replace("[Белый][<1] ММ.ГГ; ММ.ГГ", @"\[\w+\]", ""));
			Console.WriteLine(Regex.Replace("[Белый][<1] ММ.ГГ; ММ.ГГ", @"^\[\w+\]+", ""));
		}

		[Test]
		public void Native()
		{
			var file = @"C:\Projects\Production\PriceProcessor\trunk\src\PriceProcessor.Test\Data\Excel\41.xls";
			var priceItemId = Path.GetFileNameWithoutExtension(file);
			TestHelper.Execute(String.Format("update usersettings.PriceItems set RowCount = 0 where id = {0}", priceItemId));
			TestHelper.Formalize<NativeExcelParser>(file);

			TestHelper.Verify(priceItemId);
		}

		[Test]
		public void Ole()
		{
			var file = @"C:\Projects\Production\PriceProcessor\trunk\src\PriceProcessor.Test\Data\Excel\41.xls";
			TestHelper.Execute(String.Format("update usersettings.PriceItems set RowCount = 0 where id = {0}", Path.GetFileNameWithoutExtension(file)));
			TestHelper.Formalize<ExcelPriceParser>(file);
		}

		[Test]
		public void native()
		{
			var file = @"Excel\20.xls";
			var w =  Workbook.Load(file);
			Console.WriteLine("w = " + CleanupCharsThatNotFitIn1251(w.Worksheets[0].Cells[2916, 0].StringValue));
			Console.WriteLine("w1 = " + w.Worksheets[0].Cells[2916, 0].StringValue);
		}

		public string CleanupCharsThatNotFitIn1251(string value)
		{
			var ansi = Encoding.GetEncoding(1251);
			var unicodeBytes = Encoding.Unicode.GetBytes(value);
			var ansiBytes = Encoding.Convert(Encoding.Unicode, ansi, unicodeBytes);
			return ansi.GetString(ansiBytes);
		}


		[Test]
		public void Test2()
		{
			var file = @"C:\Projects\Production\PriceProcessor\trunk\src\PriceProcessor.Test\Data\Excel\196.xls";
			var workbook = Workbook.Load(file);
		}
	}
}
