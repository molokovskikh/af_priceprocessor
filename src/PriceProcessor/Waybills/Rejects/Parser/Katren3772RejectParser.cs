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
	/// Парсер для поставщика 3772. На 27.07.2015 Типы отказов: otk(2179)
	/// </summary>
	public class Katren3772RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".otk"))
				ParseOTK(reject, filename);
			else
			{
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла OTK
		/// </summary>
		protected void ParseOTK(RejectHeader reject, string filename)
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
