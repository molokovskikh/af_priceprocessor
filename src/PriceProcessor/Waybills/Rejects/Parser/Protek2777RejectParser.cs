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
	/// Парсер для поставщика 2777. На 16.07.2015 Типы отказов: txt(2923)
	/// </summary>
	public class Protek2777RejectParser : RejectParser
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
		/// </summary>
		protected void ParseTXT(RejectHeader reject, string filename)
		{
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding("Utf-8")))
			{
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
						fields = line.Trim().Split(';');
						var rejectLine = new RejectLine();
						reject.Lines.Add(rejectLine);
						rejectLine.Code = fields[0].Trim();
						rejectLine.Product = fields[1].Trim();
						rejectLine.Ordered = NullableConvert.ToUInt32(fields[2].Trim());
						var rejected = NullableConvert.ToUInt32(fields[3].Trim());
						rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}
