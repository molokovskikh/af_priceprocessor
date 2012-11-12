using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelLibrary.BinaryFileFormat;
using ExcelLibrary.SpreadSheet;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XlsParsers
{
	public class VselennajaZdorovyaParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var book = Workbook.Load(file);
			var sheet = book.Worksheets[0];

			string value = sheet.Cells[1, 0].StringValue;
			int positionProviderDocumentId = value.IndexOf("№") + 1;
			int positionDate = value.IndexOf("от");

			document.ProviderDocumentId = value.Substring(positionProviderDocumentId, positionDate - positionProviderDocumentId).Trim();
			document.DocumentDate = DateTime.Parse(value.Substring(positionDate + 2).Trim());

			foreach (var row in sheet.Cells.Rows.Values.Skip(4)) {
				if ((String.IsNullOrEmpty(row.GetCell(0).StringValue) || (row.GetCell(0).StringValue.Equals("#NULL!"))) &&
					(String.IsNullOrEmpty(row.GetCell(1).StringValue) || (row.GetCell(1).StringValue.Equals("#NULL!"))) &&
					(String.IsNullOrEmpty(row.GetCell(3).StringValue) || (row.GetCell(3).StringValue.Equals("#NULL!"))))
					return document;

				var line = document.NewLine();
				line.Product = row.GetCell(1).StringValue;
				line.Producer = row.GetCell(2).StringValue;
				line.SupplierCost = Convert.ToDecimal(row.GetCell(3).StringValue);
				line.Quantity = Convert.ToUInt32(row.GetCell(4).Value);
				line.Amount = Convert.ToDecimal(row.GetCell(5).StringValue);
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var workbook = Workbook.Load(file);
			var sheet = workbook.Worksheets[0];
			return (sheet.Cells[7, 0].StringValue.ToLower().Equals("№ п/п")) &&
				(sheet.Cells[7, 1].StringValue.ToLower().Equals("наименование продукции")) &&
				(sheet.Cells[7, 2].StringValue.ToLower().Equals("производитель")) &&
				(sheet.Cells[7, 3].StringValue.ToLower().Equals("цена")) &&
				(sheet.Cells[7, 4].StringValue.ToLower().Equals("количество")) &&
				(sheet.Cells[7, 5].StringValue.ToLower().Equals("сумма"));
		}
	}
}
