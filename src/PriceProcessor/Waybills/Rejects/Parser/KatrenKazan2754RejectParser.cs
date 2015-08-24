using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NHibernate.Mapping;
using NPOI.SS.Formula.Functions;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 2754. На 24.07.2015 Типы отказов: txt(102), dbf(2041) и otk(409)
	/// Для dbf парсера нет, так как файлы с отказами невозможно разобрать(определить отказы, заказы) 
	/// </summary>
	public class KatrenKazan2754RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".txt") || filename.Contains(".otk"))
				ParseTXT(reject, filename);
			else
			{
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла TXT и OTK
		/// </summary>
		protected void ParseTXT(RejectHeader reject, string filename)
		{
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251)))
			{
				//пропускаем первую строку и начинаем считывание
				reader.ReadLine();
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					var splat = line.Trim().Split(new[] { "\t" }, StringSplitOptions.None);

                    //по причине того,что есть файлы без поля кода товара
				    if (splat.Count() <= 2)
				    {
				        rejectLine.Product = splat[0].Replace("|", "");
				        rejectLine.Ordered = NullableConvert.ToUInt32(splat[1]);
				        var splat2 = NullableConvert.ToUInt32(splat[1]);
				        rejectLine.Rejected = splat2 != null ? splat2.Value : 0;
				    }
				    else
				    {
				        rejectLine.Product = splat[1];
				        rejectLine.Code = splat[0];
				        rejectLine.Ordered = NullableConvert.ToUInt32(splat[2]);
				        var rejected = NullableConvert.ToUInt32(splat[2]);
				        rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				    }
				}
			}
		}
	}
}
