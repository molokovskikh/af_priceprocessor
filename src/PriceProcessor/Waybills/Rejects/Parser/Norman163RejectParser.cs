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
	/// Парсер для поставщика 163. На 17.06.2015 Типы отказов: dbf(2852), txt(185) и xls(97)
	/// </summary>
	public class Norman163RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".dbf"))
				ParseDBF(reject, filename);
			else if (filename.Contains(".txt"))
				ParseTXT(reject, filename);
			else
			{
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла DBF
		/// </summary>
		protected void ParseDBF(RejectHeader reject, string filename)
		{
			var data = Dbf.Load(filename);
			for (var i = 0; i < data.Rows.Count; i++)
			{
				var rejectLine = new RejectLine();
				reject.Lines.Add(rejectLine);
				rejectLine.Code = data.Rows[i][0].ToString();
				rejectLine.Product = data.Rows[i][1].ToString();
				var ordered = data.Rows[i][2].ToString().Split(',');
				rejectLine.Ordered = NullableConvert.ToUInt32(ordered[0]);
				var rejected = NullableConvert.ToUInt32(ordered[0]);
				rejectLine.Rejected = rejected != null ? rejected.Value : 0;
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
					if (line.Contains("Заказали"))
					{
						rejectFound = true;
						continue;
					}

					if (line == "")
						rejectFound = false;

					if (!rejectFound)
						continue;

					var rejectLine = new RejectLine();
					var fields = line.Split('\t');
					reject.Lines.Add(rejectLine);
					rejectLine.Code = fields[0];
					rejectLine.Product = fields[5];
					rejectLine.Ordered = NullableConvert.ToUInt32(fields[1]);
					var rejected = NullableConvert.ToUInt32(fields[3]);
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}