﻿using System;
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
		private readonly ExcelLoader _loader;

		public NativeExcelParser(string priceFileName, MySqlConnection connection, DataTable table)
			: base(priceFileName, connection, table)
		{
			_loader = new ExcelLoader(
				table.Rows[0]["ListName"].ToString().Replace("$", ""), 
				table.Rows[0]["StartLine"] is DBNull ? 0 : Convert.ToInt32(table.Rows[0]["StartLine"]));

			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
		}

		public override void Open()
		{
			convertedToANSI = true;
			CurrPos = 0;

			_loader.PeriodField = GetFieldName(PriceFields.Period);
			_loader.PriceItemId = (uint) priceItemId;
			dtPrice = _loader.Load(priceFileName);

			base.Open();
		}
	}

	public class ExcelLoader
	{
		private readonly int _startLine;
		private readonly string _sheetName;
		
		public ExcelLoader(string sheetName, int startLine) : this()
		{
			_sheetName = sheetName;
			_startLine = startLine;
		}

		public ExcelLoader()
		{}


		public string PeriodField { get; set; }
		public uint PriceItemId { get; set; }

		public DataTable Load(string file)
		{
			var workbook = Workbook.Load(file);
			var worksheet = workbook.Worksheets.Where(w => String.Compare(w.Name, _sheetName, true) == 0).FirstOrDefault();
			if (worksheet == null)
				worksheet = workbook.Worksheets[0];

			var dataTable = new DataTable();

			var cells = worksheet.Cells;
			dataTable.Columns
				.AddRange(Enumerable.Range(cells.FirstColIndex + 1, cells.LastColIndex - cells.FirstColIndex + 1)
				.Select(i => new DataColumn("F" + i))
				.ToArray());

			for(var i = Math.Max(cells.FirstRowIndex, _startLine); i <= cells.LastRowIndex; i++)
			{
				var row = dataTable.NewRow();
				for (var j = cells.FirstColIndex; j <= cells.LastColIndex; j++)
				{
					var cell = cells[i, j];

					var columnName = "F" + (j + 1);
					if (columnName == PeriodField)
					{
						var dateTimeValue = cell.TryToGetValueAsDateTime();
						if (dateTimeValue != null)
						{
							//медицина пишет в срок годносит 1953 год если срок годности не ограничен всякие скальпели и прочее
							if (PriceItemId == 822u && dateTimeValue == new DateTime(1953, 01, 01))
								row[columnName] = DBNull.Value;
							else
								row[columnName] = dateTimeValue;
							continue;
						}
					}
					ProcessFormatIfNeeded(columnName, cell, row);
				}
				dataTable.Rows.Add(row);
			}

			return dataTable;
		}

		private void ProcessFormatIfNeeded(string columnName, Cell cell, DataRow row)
		{
			if (cell.Value is double 
				&& cell.Format.FormatType == CellFormatType.Number
				&& (cell.Format.FormatString == "0.00" || cell.Format.FormatString == "#,##0.00"))
			{
				row[columnName] = Math.Round((double) cell.Value, 2, MidpointRounding.AwayFromZero);
				return;
			}

			if (cell.Value is decimal
				&& cell.Format.FormatType == CellFormatType.Custom
				&& cell.Format.FormatString == "#,##0.0")
			{
				row[columnName] = Math.Round((decimal) cell.Value, 1, MidpointRounding.AwayFromZero);
				return;
			}

			if (cell.Value is double
				&& cell.Format.FormatType == CellFormatType.Custom
				&& cell.Format.FormatString == "#,##0.0")
			{
				row[columnName] = Math.Round((double) cell.Value, 1, MidpointRounding.AwayFromZero);
				return;
			}


			if (cell.Value is double 
				&& cell.Format.FormatType == CellFormatType.Date
				&& cell.Format.FormatString == "d-mmm")
			{
				var value = cell.TryToGetValueAsDateTime();
				if (value != null)
				{
					row[columnName] = value.Value.ToString("dd.MMM");
					return;
				}
			}

			if (cell.Value is double
				&& cell.Format.FormatType == CellFormatType.Custom
				&& (cell.Format.FormatString == "00000000000"
					|| cell.Format.FormatString == "00000000"
					|| cell.Format.FormatString == "00000"))
			{
				var padCount = cell.FormatString.Length;
				row[columnName] = cell.Value.ToString().PadLeft(padCount, '0');
				return;
			}
			row[columnName] = cell.Value;
		}
	}
}
