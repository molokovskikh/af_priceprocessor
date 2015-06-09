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
	public class ZdravServis1581RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".txt"))
			{
				ParseTXT(reject, filename);
			}
			else if (filename.Contains(".dbf"))
				ParseDBF(reject, filename);
		}

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

					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					var index = line.IndexOf("Добавлено:");

					var first = line.Substring(0, index).Trim();
					var product = first.Remove(0, 2);
					rejectLine.Product = product;

					var second = line.Substring(index).Trim().Replace(",", " ");
					var fields = second.Split(' ');
					rejectLine.Cost = NullableConvert.ToDecimal(fields[7].Trim().Replace(".", ","));
					var ordered = NullableConvert.ToUInt32(fields[4]);
					var delivered = NullableConvert.ToUInt32(fields[1].Trim());
					rejectLine.Ordered = ordered;
					var rejected = ordered - delivered;
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}

		protected void ParseDBF(RejectHeader reject, string filename)
		{
			Logger.WarnFormat("Не удалось получить файл с отказами '{0}' для лога документа {1}, так как для формата dbf парсера нет", filename, reject.Log.Id);	
		}
	}
}
