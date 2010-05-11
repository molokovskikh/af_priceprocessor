using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
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
		[Test, Ignore("не работает, т.к. нужны были для проверки формализации новых форматов и сравнения со старыми")]
		public void Try_to_formalize()
		{
			List<TestPriceItem> items;
			uint[] ids;
			using(new SessionScope())
			{
				items = TestPriceItem.Queryable.Where(i => i.Format.PriceFormat == PriceFormatType.Xls).ToList();
				ids = items.Select(i => i.Id).ToArray();
			}

			foreach (var id in ids)
			{
				try
				{
					var file = Path.Combine(@"..\..\..\Data\Excel\", String.Format("{0}.xls", id));
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

		[Test, Ignore("Не тест служит для того что бы подготовить эталанное данные")]
		public void Prepare_etalon_data()
		{
			if (Directory.Exists(@"..\..\data\excel\"))
				Directory.Delete(@"..\..\data\excel\", true);
			Directory.CreateDirectory(@"..\..\data\excel\");

			List<TestPriceItem> items;
			using(new SessionScope())
			{
				items = TestPriceItem.Queryable.Where(i => i.Format.PriceFormat == PriceFormatType.Xls).ToList();
			}

			foreach (var item in items)
			{
				var price = String.Format(@"\\fms.adc.analit.net\prices\base\{0}.xls", item.Id);
				if (File.Exists(price))
					File.Copy(price, String.Format(@"..\..\Data\Excel\{0}.xls", item.Id));
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
			var file = @"C:\Projects\Production\PriceProcessor\trunk\src\PriceProcessor.Test\Data\Excel\39.xls";
			var w=  Workbook.Load(file);
		}

		[Test]
		public void Test2()
		{
			var file = @"C:\Projects\Production\PriceProcessor\trunk\src\PriceProcessor.Test\Data\Excel\196.xls";
			var workbook = Workbook.Load(file);
		}
	}
}
