using System;
using System.Data;
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
	/// Парсер для поставщика 149. На 15.06.2015 Типы отказов: xls(3035) и txt(587).
	/// </summary>
	public class Katren149RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".xls"))
				ParseXLS(reject, filename);
			else if (filename.Contains(".txt"))
				ParseTXT(reject, filename);
			else {
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла XLS
		/// Но на самом деле это файлы dbf
		/// </summary>
		protected void ParseXLS(RejectHeader reject, string filename)
		{
			DataTable data;
			try {
				data = Dbf.Load(filename);
			}
			catch(Exception e)
			{
				var err = string.Format("Не удалось получить файл с отказами '{0}' для лога документа {1}", filename, reject.Log.Id);
				Logger.Warn(err, e);
				return;
			}

			for (var i = 0; i < data.Rows.Count; i++) {
					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					rejectLine.Code = data.Rows[i][0].ToString();
					rejectLine.Product = data.Rows[i][1].ToString();
					rejectLine.Ordered = NullableConvert.ToUInt32(data.Rows[i][2].ToString());
					var rejected = NullableConvert.ToUInt32(data.Rows[i][2].ToString());
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
					if (line.Contains("Код"))
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
					rejectLine.Product = fields[1];
					var overed = fields[5].Split('.');
					rejectLine.Ordered = NullableConvert.ToUInt32(overed[0]);
					var rejected = NullableConvert.ToUInt32(overed[0]);
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}
