using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using ExcelLibrary.SpreadSheet;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("только для отладки Романом")]
	public class ExcelFixture
	{
		[Test, Ignore("не работает, т.к. нужны были для проверки формализации новых форматов и сравнения со старыми")]
		public void Try_to_formalize()
		{
			var ids = new[]
			{
				1, 10, 102, 103, 109, 110, 111, 116, 117, 118, 122, 123, 126, 130, 134, 144, 145, 146, 150, 158, 159, 16, 163, 165,
				169, 17, 172, 174, 176, 183, 185, 192, 193, 197, 200, 21, 219, 220, 222, 227, 236, 24, 244, 25, 253, 26, 260, 266,
				271, 273, 275, 283, 284, 29, 299, 305, 31, 318, 323, 326, 332, 335, 341, 343, 346, 350, 358, 369, 374, 375, 382, 383
				, 387, 39, 395, 397, 398, 400, 403, 404, 405, 407, 42, 422, 423, 425, 434, 442, 446, 45, 451, 456, 467, 470, 473,
				475, 477, 480, 485, 49, 490, 491, 492, 493, 497, 50, 502, 507, 529, 530, 532, 533, 539, 542, 548, 551, 560, 561, 563
				, 564, 571, 573, 58, 581, 587, 588, 595, 6, 600, 609, 610, 621, 623, 625, 629, 631, 637, 639, 643, 655, 656, 658,
				659, 660, 661, 662, 68, 684, 685, 686, 69, 690, 698, 705, 706, 707, 709, 718, 724, 727, 732, 735, 739, 74, 745, 747,
				765, 776, 777, 788, 79, 80, 816, 819, 84, 85, 878, 884, 885, 898, 900, 904, 91, 928, 929, 93, 931, 94, 940, 958, 959
				, 96, 960, 961, 962, 965, 967
			};
			var files = Directory.GetFiles(Path.GetFullPath(@"..\..\Data\Excel\"));
			foreach (var id in ids)
			{
				try
				{
					var file = Path.Combine(@"..\..\Data\Excel\", String.Format("{0}.xls", id));
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
