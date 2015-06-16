using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NPOI.POIFS.FileSystem;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 7975. На 15.06.2015 Типы отказов: csv(508), txt(2284) и dbf(4)
	/// </summary>
	public class PulsBryansk7975RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".csv"))
				ParseCSV(reject, filename);
			else if (filename.Contains(".txt"))
			ParseTXT(reject, filename);
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
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251))) {
				string line;
				var rejectFound = false;
				//Читаем, пока не кончатся строки в файле
				while ((line = reader.ReadLine()) != null) {
					//Делим строку на части по разделителю.
					//CSV файл, который ожидает данный парсер - это эксел таблица, строки которой разделены переносом строки, а ячейки символом ";"
					var fields = line.Split(';');
					//Ищем в ячейке место, с которого начинаются отказы в таблице
					if (fields[0].Trim() == "Номер заказа ПОСТАВЩИКу") {
						rejectFound = true;
						continue;
					}
					//Если мы еще не дошли до места с которого начинаются отказы, то продолжаем
					if (!rejectFound)
						continue;

					//Если мы дошли до этого места, значит все что осталось в файле - это строки с отказами
					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					rejectLine.Code = fields[5];
					rejectLine.Product = fields[6];
					rejectLine.Cost = NullableConvert.ToDecimal(fields[10].Trim());
					rejectLine.Ordered = NullableConvert.ToUInt32(fields[7].Trim());
					var rejected = NullableConvert.ToUInt32(fields[9].Trim());
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}

		/// <summary>
		/// Парсер для формата файла TXT
		/// </summary>
		protected void ParseTXT(RejectHeader reject, string filename)
		{
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251))) {
				string line;
				var rejectFound = false;
				//Читаем, пока не кончатся строки в файле
				while ((line = reader.ReadLine()) != null) {
						var fields = line.Split(';');
						if (fields[0].Trim() == "Код") {
							rejectFound = true;
							continue;
						}

						//Если мы еще не дошли до места с которого начинаются отказы, то продолжаем
						if (!rejectFound)
							continue;

						//Если мы дошли до этого места, значит все что осталось в файле - это строки с отказами
						fields = line.Trim().Split('\t');
						var rejectLine = new RejectLine();
						reject.Lines.Add(rejectLine);
						rejectLine.Code = fields[0].Trim();
						rejectLine.Product = fields[5].Trim();
						rejectLine.Cost = NullableConvert.ToDecimal(fields[4].Replace(".", ",").Trim());
						rejectLine.Ordered = NullableConvert.ToUInt32(fields[1].Trim());
						var rejected = NullableConvert.ToUInt32(fields[3].Trim());
						rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}
