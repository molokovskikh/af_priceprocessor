using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Castle.Components.DictionaryAdapter.Xml;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 12423. На 22.07.2015 Типы отказов: dbf(107), txt(2455)
	/// Для dbf парсера нет, так как файлы с отказами невозможно разобрать(определить отказы, заказы, наименование товара) 
	/// </summary>
	public class SiaAstrakhan12423RejectParser : RejectParser
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
		/// </summary>
		protected void ParseTXT(RejectHeader reject, string filename)
		{
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251)))
			{
				string line;
				var rejectFound = false;
				while ((line = reader.ReadLine()) != null)
				{
					//доходим до строчки, в которой содерится слово ТОВАР и начинаем чтение
					if (line.Contains("ТОВАР")) {
						rejectFound = true;
						//пропускаем линию-разделения таблицы
						reader.ReadLine();
						continue;
					}

					if (line == "")
						rejectFound = false;

					var rejectLine = new RejectLine();
					//проверяем на наличие линий-разделения таблицы
					if (line.Contains("+="))
						rejectFound = false;
					
					if (!rejectFound)
						continue;

					var fields = line.Split('|');
					reject.Lines.Add(rejectLine);
					rejectLine.Product = fields[2];
					rejectLine.Ordered = NullableConvert.ToUInt32(fields[5]);
					var rejected = NullableConvert.ToUInt32(fields[5]);
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}