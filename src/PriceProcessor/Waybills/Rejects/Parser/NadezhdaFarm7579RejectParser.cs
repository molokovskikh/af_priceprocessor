using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 7579. На 15.06.2015 Типы отказов: DBF(189) и txt(18482).
	/// </summary>
	public class NadezhdaFarm7579RejectParser : RejectParser
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
				var rejectFound = false;
				while ((line = reader.ReadLine()) != null)
				{
					if (line.Trim() == "О Т К А З Ы")
					{
						rejectFound = true;
						continue;
					}
					if (line.Trim() == "СФОРМИРОВАННЫЙ ЗАКАЗ")
					{
						break;
					}
					if (!rejectFound)
						continue;
					if (line.Length == 0)
						continue;
					//пропускаем заголовок
					if (line[0] == '¦')
						continue;
					//пропускаем разделители строк
					if (line.All(c => c == '-'))
						continue;
					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					rejectLine.Product = line.Substring(0, 35).Trim();
					rejectLine.Producer = line.Substring(35, 13).Trim();
					rejectLine.Cost = NullableConvert.ToDecimal(line.Substring(48, 9).Trim(), CultureInfo.InvariantCulture);
					rejectLine.Ordered = (uint?)NullableConvert.ToFloatInvariant(line.Substring(57, 9).Trim());
					var rejectedCount = (rejectLine.Ordered - (uint?)NullableConvert.ToFloatInvariant(line.Substring(66, 9).Trim()));
					rejectLine.Rejected = rejectedCount.GetValueOrDefault();
				}
			}
		}
	}
}
