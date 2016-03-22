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
	/// Парсер для поставщика 15365. На 06.07.2015 Типы отказов: csv(118), txt(5078) и dbf(311)
	/// Для dbf парсера нет, так как в файлах не понятно что является отказом
	/// </summary>
	public class PulsVoronezh15365RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".csv"))
				ParseCSV(reject, filename);
			else if (filename.Contains(".txt"))
				ParseTXT(reject, filename);
			else
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
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
					if (fields[0].Length > 0 && fields[0].Trim() == "Номер заказа ПОСТАВЩИКу") {
						rejectFound = true;
						continue;
					}
					//Если мы еще не дошли до места с которого начинаются отказы, то продолжаем
					if (!rejectFound)
						continue;

					//Если мы дошли до этого места, значит все что осталось в файле - это строки с отказами
						if (fields.Length < 11)
							continue;
					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					rejectLine.Code = fields[5];
					rejectLine.Product = fields[6];
					rejectLine.Cost = NullableConvert.ToDecimal(fields[10].Replace(".", ",").Trim());
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
						if (fields.Length > 0 && fields[0].Trim() == "Код") {
							rejectFound = true;
							continue;
						}

						//Если мы еще не дошли до места с которого начинаются отказы, то продолжаем
						if (!rejectFound)
							continue;

						//Если мы дошли до этого места, значит все что осталось в файле - это строки с отказами
						fields = line.Trim().Split('\t');
						if (fields.Length < 6)
							continue;
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
