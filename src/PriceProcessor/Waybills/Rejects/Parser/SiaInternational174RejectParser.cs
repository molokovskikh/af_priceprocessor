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
	/// Парсер для поставщика 174. На 15.06.2015 Типы отказов: csv(3987).
	/// </summary>
	public class SiaInternational174RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".csv"))
				ParseCSV(reject, filename);
			else
			{
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла CSV
		/// </summary>
		protected void ParseCSV(RejectHeader reject, string filename)
		{
			//Открываем файл, при помощи unsing, чтобы вконце си шарп отпустил занятые ресурсы. Подробнее гугли IDisposable.
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251)))
			{
				string line;
				var rejectFound = false;
				//Читаем, пока не кончатся строки в файле
				while ((line = reader.ReadLine()) != null)
				{
					//Делим строку на части по разделителю.
					//CSV файл, который ожидает данный парсер - это эксел таблица, строки которой разделены переносом строки, а ячейки символом ";"
					var fields = line.Split(';');
					//Ищем в ячейке место, с которого начинаются отказы в таблице
					if (fields[1].Trim() == "ОТКАЗАНО")
					{
						//После строчки с этой надписью, будет еще одна строка с наименованием таблицы, ее мы пропустим
						//И со следующей строчки будут уже идти отказы
						//В итоге надо пропустить 2 строчки и проставить флаг, что дальше пора считывать отказы
						//Отказы идут прямо до конца файла, так что в условии остановки считывания нужды нет
						reader.ReadLine();
						rejectFound = true;
						continue;
					}
					//Если мы еще не дошли до места с которого начинаются отказы, то продолжаем
					if (!rejectFound)
						continue;

					//Если мы дошли до этого места, значит все что осталось в файле - это строки с отказами
					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					rejectLine.Code = fields[1];
					rejectLine.Product = fields[2];
					rejectLine.Producer = fields[3];
					rejectLine.Cost = NullableConvert.ToDecimal(fields[4].Trim());
					rejectLine.Ordered = NullableConvert.ToUInt32(fields[5].Trim());
					//тонкий момент - это поле является обязательным, но теоретически может отсутствовать в файле
					//в случае отсутствия мы записываем 0, тогда далее другие классы разберут этот случай
					//и сделают необходимые действия по урегулированию ситуации
					var rejected = NullableConvert.ToUInt32(fields[7].Trim());
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}
