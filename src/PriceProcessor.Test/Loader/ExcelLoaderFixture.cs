using System;
using System.Data;
using System.Text;
using ExcelLibrary.BinaryFileFormat;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class ExcelLoaderFixture
	{
		private ExcelLoader loader;

		[SetUp]
		public void Setup()
		{
			loader = new ExcelLoader();
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
		}

		[Test, Ignore("Пока сломан на будущее")]
		//гадское форматирование даты
		public void Detect_builtin_data_time_formats()
		{
			var data = Load(90);
			Assert.That(data.Rows[3][5], Is.EqualTo("1/12/11"));
		}

		[Test, Ignore("Пока сломан на будущее")]
		//получение значения заводской упаковки через форматирование
		public void Process_custom_format()
		{
			var data = Load(1184);
			Assert.That(data.Rows[194][8], Is.EqualTo("540/10"));
			Assert.That(data.Rows[255][8], Is.EqualTo("200/50"));
		}

		[Test, Ignore("Пока сломан на будущее")]
		//получение значения заводской упаковки через форматирование
		public void Process_custom_format1()
		{
			var data = Load(1170);
			Assert.That(data.Rows[6][5], Is.EqualTo("96/1"));
		}

		[Test]
		//применять форматирование для чисел
		public void Process_numeric_format()
		{
			var data = Load(294);
			Assert.That(data.Rows[1][4], Is.EqualTo("64,37"));
		}

		[Test]
		//применять форматирование для чисел
		public void Custom_numeric_format()
		{
			Load(90);
			var data = Load(235);
			Assert.That(data.Rows[11][5], Is.EqualTo("1889,1"));
			Assert.That(data.Rows[25][5], Is.EqualTo("1081,7"));
		}

		[Test]
		public void Process_numeric_format_with_groups()
		{
			var data = Load(886);
			Assert.That(data.Rows[7][5], Is.EqualTo("102,56"));
		}

		[Test]
		public void Pad_zero_to_left()
		{
			var data = Load(196);
			Assert.That(data.Rows[8][1], Is.EqualTo("00000000422"));
		}

		[Test]
		public void Read_biff8_with_labels()
		{
			var data = Load(951);
			Assert.That(data.Rows[1][1], Is.EqualTo(" Ковш черпак береза 1л арт.Б101 N1x1 Зигер РОС"));
		}

		[Test]
		public void Read_builtid_date_time_format()
		{
			var data = Load(986);
			Assert.That(data.Rows[251][3], Is.EqualTo("24.авг"));
		}

		private DataTable Load(int id)
		{
			return loader.Load(String.Format(@"..\..\data\ExcelLoaderFixture\{0}.xls", id));
		}
	}
}