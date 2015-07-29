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
	/// Парсер для поставщика 5946. На 15.07.2015 Типы отказов: dbf(3186), def(105)
	/// </summary>
	public class SiaInternationalTula5946RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.ToUpper().Contains(".DBF"))
				ParseDBF(reject, filename);
			else if (filename.ToUpper().Contains(".DEF"))
				ParseDEF(reject, filename);
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
			//файлы в которых нет наименования товара не разбираем
			if (!data.Columns[0].ToString().Contains("KOD"))
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не может парсить данный файл из-за отсутсвия наименования товара", filename, GetType().Name);
			else {
				for (var i = 0; i < data.Rows.Count; i++) {
					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					rejectLine.Product = data.Rows[i][1].ToString();
					rejectLine.Code = data.Rows[i][0].ToString();
					var kolvo = data.Rows[i][4].ToString().Split(',');
					rejectLine.Ordered = NullableConvert.ToUInt32(kolvo[0]);
					var rejected = NullableConvert.ToUInt32(kolvo[0]);
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}

		/// <summary>
		/// Парсер для формата файла DEF
		/// </summary>
		protected void ParseDEF(RejectHeader reject, string filename)
		{
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251)))
			{
				string line;
				var rejectFound = false;
				while ((line = reader.ReadLine()) != null)
				{
					if (line.Contains("КОД"))
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
					rejectLine.Ordered = NullableConvert.ToUInt32(fields[4]);
					var rejected = NullableConvert.ToUInt32(fields[4]);
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}
