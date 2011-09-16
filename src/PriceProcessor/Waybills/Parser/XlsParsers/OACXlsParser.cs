using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelLibrary.BinaryFileFormat;
using ExcelLibrary.SpreadSheet;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XlsParsers
{
	public class OACXlsParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var book = Workbook.Load(file);
			var sheet = book.Worksheets[0];

			document.ProviderDocumentId = sheet.Cells[10, 0].StringValue;;
			document.DocumentDate = DateTime.Parse(sheet.Cells[10, 1].StringValue);

			foreach (var row in sheet.Cells.Rows.Values.Skip(5))
			{
				if (String.IsNullOrEmpty(row.GetCell(0).StringValue) &&
					(String.IsNullOrEmpty(row.GetCell(1).StringValue) || (row.GetCell(1).StringValue.Equals("#NULL!"))) &&
					(String.IsNullOrEmpty(row.GetCell(3).StringValue) || (row.GetCell(3).StringValue.Equals("#NULL!"))))
					return document;
				var line = document.NewLine();
				line.Product = row.GetCell(0).StringValue;
				line.Quantity = Convert.ToUInt32(row.GetCell(1).Value);
				line.SupplierCost = Convert.ToDecimal(row.GetCell(3).StringValue);
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var workbook = Workbook.Load(file);
			var sheet = workbook.Worksheets[0];
			return (sheet.Cells[14, 0].StringValue.ToLower().Equals("наименование")) &&
				   (sheet.Cells[14, 3].StringValue.ToLower().Equals("цена")) &&
				   (sheet.Cells[14, 4].StringValue.ToLower().Equals("сумма"));
		}
	}
}
