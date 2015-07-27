using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NHibernate.Mapping;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 12799. На 24.07.2015 Типы отказов: txt(110) и xls(1987)
	/// Парсера для формата xls нет, так как файл не читается
	/// из-за того,что он видимо неправильно сохранен
	/// </summary>
	public class Astika12799RejectParser : RejectParser
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
						if (line.Contains("ИД заявки"))
						{
							rejectFound = true;
							reader.ReadLine();
							continue;
						}

						if (line == "")
							rejectFound = false;

						if (!rejectFound)
							continue;

						var rejectLine = new RejectLine();
						reject.Lines.Add(rejectLine);
						var splat = line.Trim().Split(':');
						rejectLine.Product = splat[0].Trim();
						rejectLine.Ordered = NullableConvert.ToUInt32(splat[1].Replace("-", ""));
						var rejected = NullableConvert.ToUInt32(splat[1]);
						rejectLine.Rejected = rejected != null ? rejected.Value : 0;
					}
				}
			}
		}
	}
