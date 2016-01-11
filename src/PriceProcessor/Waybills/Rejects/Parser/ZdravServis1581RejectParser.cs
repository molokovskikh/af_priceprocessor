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
	/// Парсер для поставщика 1581. На 15.06.2015 Типы отказов: txt(2351), sst(3) и dbf(1).
	/// </summary>
	public class ZdravServis1581RejectParser : RejectParser
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
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251))) {
				string line;
				var rejectFound = false;
				while ((line = reader.ReadLine()) != null) {
					if (line.Contains("К сожалению вся позиция уже отсутствует:")) {
						rejectFound = true;
						continue;
					}

					if (line == "")
						rejectFound = false;

					if (!rejectFound)
						continue;

					var index = line.IndexOf("Добавлено:");
					if (index <= 0)
						continue;

					var first = line.Substring(0, index).Trim();
					var product = first.Remove(0, 2);
					var rejectLine = new RejectLine();
					rejectLine.Product = product;

					var second = line.Substring(index).Trim().Replace(",", " ");
					var fields = second.Split(' ');
					rejectLine.Cost = NullableConvert.ToDecimal(fields[7].Trim().Replace(".", ","));
					var ordered = NullableConvert.ToUInt32(fields[4]);
					var delivered = NullableConvert.ToUInt32(fields[1].Trim());
					rejectLine.Ordered = ordered;
					var rejected = ordered - delivered;
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
					reject.Lines.Add(rejectLine);
				}
			}
		}
	}
}
