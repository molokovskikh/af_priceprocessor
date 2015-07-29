using System;
using System.Data;
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
	/// Парсер для поставщика 4365. На 07.07.2015 Типы отказов: txt(2010).
	/// </summary>
	public class FarmkomplektVoronezh4365RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".txt"))
				ParseTXT(reject, filename);
			else {
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла TXT
		/// для формата txt много различных типов файлов
		/// </summary>
		protected void ParseTXT(RejectHeader reject, string filename)
		{
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251)))
			{
				//считываем весь текст из файла и проверяем на наличие слова Отказы
				//из-за проблемы с кодировкой в некоторых файлах
				//это сделано на случай страховки,если вдруг текст в файле все же не будет считываться правильно
				string file = reader.ReadToEnd();
				if (!file.Contains("Отказы"))
					Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не может парсить данный файл из-за проблемы с кодировкой", filename, GetType().Name);
				else {
					var parts = file.Split(new[] { "\r\n" }, StringSplitOptions.None);
					for(var i = 1; i < parts.Count(); i++)
					{
						var rejectFound = true;

						if (parts[i] == "Спасибо")
							rejectFound = false;

						if(parts[i].Contains("\r"))
							rejectFound = false;

						if (parts[i] == "")
							rejectFound = false;

						if (parts[i].Contains("аптека"))
							rejectFound = false;

						if (!rejectFound)
							continue;


						var rejectLine = new RejectLine();
						reject.Lines.Add(rejectLine);
						var splat = parts[i].Split(new[] { "; заказано " }, StringSplitOptions.None);
						var product = splat[0];
						rejectLine.Product = product;
						var splat2 = splat[1].Split(new[] { " отпущено " }, StringSplitOptions.None);
						var overed = NullableConvert.ToUInt32(splat2[0]);
						var released = NullableConvert.ToUInt32(splat2[1].Replace(";", ""));
						rejectLine.Ordered = overed;
						var rejected = overed - released;
						rejectLine.Rejected = rejected != null ? rejected.Value : 0;
					}
				}
			}
		}
	}
}
