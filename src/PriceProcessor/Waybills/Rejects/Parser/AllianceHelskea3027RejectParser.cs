using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Common.Tools;
using ExcelLibrary.SpreadSheet;
using Inforoom.PriceProcessor.Models;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	public class AllianceHelskea3027RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			HSSFWorkbook hssfwb;
			using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				hssfwb = new HSSFWorkbook(file);
			}

			ISheet sheet = hssfwb.GetSheetAt(0);
			//запускаем цикл по строкам
			var rejectFound = false;
			for (var i = 0; i <= sheet.LastRowNum; i++) {
				var row = sheet.GetRow(i);
				if (row != null) {
					var cell = row.GetCell(0);
					if (cell != null && cell.ToString() == "Товар") {
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
					rejectLine.Code = row.GetCell(6).NumericCellValue.ToString();
					rejectLine.Producer = row.GetCell(4).StringCellValue;
					rejectLine.Ordered = NullableConvert.ToUInt32(row.GetCell(7).NumericCellValue.ToString());
					var rejected = NullableConvert.ToUInt32(row.GetCell(8).NumericCellValue.ToString());
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
			}
		}
	}
}
