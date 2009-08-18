using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExcelLibrary.BinaryFileFormat;
using ExcelLibrary.SpreadSheet;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class NativeExcelParser : InterPriceParser
	{
		private readonly string _sheetName;
		private readonly int _startLine;

		public NativeExcelParser(string priceFileName, MySqlConnection connection, DataTable table)
			: base(priceFileName, connection, table)
		{
			_sheetName = table.Rows[0]["ListName"].ToString().Replace("$", "");
			_startLine = table.Rows[0]["StartLine"] is DBNull ? 0 : Convert.ToInt32(table.Rows[0]["StartLine"]);
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
		}

		public override void Open()
		{
			convertedToANSI = true;
			CurrPos = 0;

			var workbook = Workbook.Load(priceFileName);
			var worksheet = workbook.Worksheets.Where(w => String.Compare(w.Name, _sheetName, true) == 0).FirstOrDefault();
			if (worksheet == null)
				worksheet = workbook.Worksheets[0];

			var cells = worksheet.Cells;
			var dataTable = new DataTable();
			dataTable.Columns
				.AddRange(Enumerable.Range(cells.FirstColIndex + 1, cells.LastColIndex - cells.FirstColIndex + 1)
				.Select(i => new DataColumn("F" + i))
				.ToArray());

			var perionColumn = GetFieldName(PriceFields.Period);

			for(var i = Math.Max(cells.FirstRowIndex, _startLine); i <= cells.LastRowIndex; i++)
			{
				var row = dataTable.NewRow();
				for (var j = cells.FirstColIndex; j <= cells.LastColIndex; j++)
				{
					var cell = cells[i, j];

					var columnName = "F" + (j + 1);
					if (columnName == perionColumn)
					{
						var dateTimeValue = cell.TryToGetValueAsDateTime();
						if (dateTimeValue != null)
						{
							row[columnName] = dateTimeValue;
							continue;
						}
					}
					row[columnName] = cell.Value;
				}
				dataTable.Rows.Add(row);
			}

			dtPrice = dataTable;
			base.Open();
		}
	}
}
