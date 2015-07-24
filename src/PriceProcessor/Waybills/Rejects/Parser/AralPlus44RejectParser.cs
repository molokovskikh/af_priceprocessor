using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NHibernate.Mapping;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 44. На 17.07.2015 Типы отказов: txt(195).
	/// </summary>
	public class AralPlus44RejectParser : RejectParser
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
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251)))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (line != "") {
						var rejectLine = new RejectLine();
						reject.Lines.Add(rejectLine);
						var splat = line.Trim().Split(' ');
						var splatCountLast = splat.Count() - 1;
						var splatLast = splat[splatCountLast];
						splat[splatCountLast] = "";
						var product = String.Join(" ", splat, 0, splat.Count());
						rejectLine.Product = product.Trim();
						var ordered = splatLast.Split('>');
						rejectLine.Ordered = NullableConvert.ToUInt32(ordered[0].Replace("-", ""));
						var received = NullableConvert.ToUInt32(ordered[1]);
						var rejected = rejectLine.Ordered - received;
						rejectLine.Rejected = rejected != null ? rejected.Value : 0;
					}
				}
			}
		}
	}
}
