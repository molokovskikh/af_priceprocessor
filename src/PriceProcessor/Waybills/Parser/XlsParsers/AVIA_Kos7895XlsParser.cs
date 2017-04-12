using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using ExcelLibrary.BinaryFileFormat;
using ExcelLibrary.SpreadSheet;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XlsParsers
{
	public class AVIA_Kos7895XlsParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var book = Workbook.Load(file);
			var sheet = book.Worksheets[0];

			string value = sheet.Cells[9, 3].StringValue;
			int positionProviderDocumentId = value.IndexOf("№") + 1;
			int positionDate = value.IndexOf("от");

			document.ProviderDocumentId = value.Substring(positionProviderDocumentId, positionDate - positionProviderDocumentId).Trim();
			document.DocumentDate = DateTime.Parse(value.Substring(positionDate + 2).Trim());


			foreach (var row in sheet.Cells.Rows.Values.Skip(7)) {
				if ((String.IsNullOrEmpty(row.GetCell(0).StringValue) || (row.GetCell(0).StringValue.Equals("#NULL!"))) &&
					(String.IsNullOrEmpty(row.GetCell(4).StringValue) || (row.GetCell(4).StringValue.Equals("#NULL!"))) &&
					(String.IsNullOrEmpty(row.GetCell(6).StringValue) || (row.GetCell(6).StringValue.Equals("#NULL!"))))
					return document;

				var line = document.NewLine();
				line.Code = row.GetCell(2).StringValue;
				line.EAN13 = NullableConvert.ToUInt64(row.GetCell(3).StringValue);
				line.Product = row.GetCell(4).StringValue;
				line.Unit = row.GetCell(5).StringValue;
				line.Quantity = Convert.ToUInt32(row.GetCell(6).Value);
				line.SupplierCost = Convert.ToDecimal(row.GetCell(7).StringValue);
				line.Amount = Convert.ToDecimal(row.GetCell(8).StringValue);
				line.Country = row.GetCell(9).StringValue;
				line.BillOfEntryNumber = row.GetCell(10).StringValue;
			}

			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			Workbook workbook;
			try {
				workbook = Workbook.Load(file);
			} catch(ArgumentOutOfRangeException) {
				return false;
			}
			var sheet = workbook.Worksheets[0];
			return (sheet.Cells[11, 0].StringValue.ToLower().Equals("№ п/п")) &&
				(sheet.Cells[11, 4].StringValue.ToLower().Equals("наименование")) &&
				(sheet.Cells[11, 3].StringValue.ToLower().Equals("штрих-код"));
		}
	}
}
