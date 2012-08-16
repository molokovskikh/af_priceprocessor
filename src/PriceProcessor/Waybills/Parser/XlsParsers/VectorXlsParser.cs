using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelLibrary.BinaryFileFormat;
using ExcelLibrary.SpreadSheet;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XlsParsers
{
	public class VectorXlsParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var book = Workbook.Load(file);
			var sheet = book.Worksheets[0];

			string value = sheet.Cells[0, 0].StringValue;
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
				line.Code = row.GetCell(0).StringValue;
				line.Product = row.GetCell(1).StringValue;
				line.Quantity = Convert.ToUInt32(row.GetCell(3).Value);
				line.SupplierCost = Convert.ToDecimal(row.GetCell(6).StringValue);
				line.Nds = Convert.ToUInt32(row.GetCell(8).StringValue);
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var workbook = Workbook.Load(file);
			var sheet = workbook.Worksheets[0];
			return (sheet.Cells[5, 0].StringValue.ToLower().Equals("код")) &&
				(sheet.Cells[5, 1].StringValue.ToLower().Equals("наименование товара")) &&
				(sheet.Cells[5, 3].StringValue.ToLower().Equals("количество")) &&
				(sheet.Cells[5, 6].StringValue.ToLower().Equals("цена")) &&
				(sheet.Cells[5, 8].StringValue.ToLower().Equals("ндс"));
		}
	}
}