using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NPOI.POIFS.FileSystem;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 14960. На 06.07.2015 Типы отказов: txt(4731)
	/// </summary>
	public class Yarfarma14960RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".txt"))
				ParseTXT(reject, filename);
			else
			{
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла TXT
		/// В файле допущена ошибка с тем что в графе запрошено стоит ноль
		/// Раз неизвестно количество заказанных товаров,то невозможно вычислить отказы
		/// Строки, где в запрошено стоит ноль - игнорируем
		/// </summary>
		protected void ParseTXT(RejectHeader reject, string filename)
		{
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251)))
			{
				string line;
				var rejectFound = false;
				while ((line = reader.ReadLine()) != null)
				{
					if (line.Contains("Следующие расхождения с заявкой:"))
					{
						rejectFound = true;
						continue;
					}

					if (line == "")
						rejectFound = false;

					if (!rejectFound)
						continue;

					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					var first = line.Replace("Отказ по количеству: ", "");
					var splat = first.Split(new[] { " - запрошено" }, StringSplitOptions.None);
					var product = splat[0];
					var other = splat[1];
					var splat2 = other.Split(new[] { ", к получению " }, StringSplitOptions.None);
					var ordered = NullableConvert.ToUInt32(splat2[0]);
					var requested = NullableConvert.ToUInt32(splat2[1]);
					if (ordered == 0)
						continue;
					var rejected = ordered - requested;
					rejectLine.Product = product.Trim();
					rejectLine.Ordered = ordered;
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}
