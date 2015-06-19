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
	/// Парсер для поставщика 12714. На 15.06.2015 Типы отказов: xls(1195) и dbf(21599).
	/// </summary>
	public class AstiPlus12714RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".dbf"))
				ParseDBF(reject, filename);
			else if (filename.Contains(".xls"))
				ParseXLS(reject, filename);
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
					rejectLine.Product = data.Rows[i][1].ToString();
					rejectLine.Producer = data.Rows[i][2].ToString();
					rejectLine.Ordered = NullableConvert.ToUInt32(data.Rows[i][3].ToString());
					var rejected = NullableConvert.ToUInt32(data.Rows[i][4].ToString());
					rejectLine.Cost = NullableConvert.ToDecimal(data.Rows[i][5].ToString());
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
			}		
		}

		/// <summary>
		/// Парсер для формата файла XLS
		/// </summary>
		protected void ParseXLS(RejectHeader reject, string filename)
		{
			HSSFWorkbook hssfwb;
			using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				try {
					hssfwb = new HSSFWorkbook(file);
				}
				catch (Exception e) 
				{
					var err = string.Format("Не удалось получить файл с отказами '{0}' для лога документа {1}", filename, reject.Log.Id);
					Logger.Warn(err, e);
					return;
				}
			}

			ISheet sheet = hssfwb.GetSheetAt(0);
			//запускаем цикл по строкам
			var rejectFound = false;
			for (var i = 0; i <= sheet.LastRowNum; i++)
			{
				var row = sheet.GetRow(i);
				if (row != null)
				{
					var cell = row.GetCell(0);
					if (cell != null && cell.ToString() == "Товар")
					{
						rejectFound = true;
						continue;
					}

					//проверяем ячейку на null и остальные невидимые значения	
					if (cell == null || string.IsNullOrWhiteSpace(cell.StringCellValue))
						rejectFound = false;

					if (!rejectFound)
						continue;

					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					rejectLine.Product = row.GetCell(0).StringCellValue;
					rejectLine.Producer = row.GetCell(4).StringCellValue;
					rejectLine.Ordered = NullableConvert.ToUInt32(row.GetCell(7).NumericCellValue.ToString());
					rejectLine.Cost = NullableConvert.ToDecimal(row.GetCell(12).NumericCellValue.ToString());
					var rejected = NullableConvert.ToUInt32(row.GetCell(10).NumericCellValue.ToString());
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}