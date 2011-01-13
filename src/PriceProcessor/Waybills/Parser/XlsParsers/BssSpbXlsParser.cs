using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelLibrary.BinaryFileFormat;
using ExcelLibrary.SpreadSheet;

namespace Inforoom.PriceProcessor.Waybills.Parser.XlsParsers
{
	public class BssSpbXlsParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var book = Workbook.Load(file);
			var sheet = book.Worksheets[0];

			document.ProviderDocumentId = sheet.Cells[2, 2].StringValue;
			DateTime docDate;
			if (DateTime.TryParse(sheet.Cells[1, 2].StringValue, out docDate))
				document.DocumentDate = docDate;
			foreach (var row in sheet.Cells.Rows.Values.Skip(23))
			{
				var line = document.NewLine();
				line.Product = row.GetCell(1).StringValue;
				line.Code = row.GetCell(2).StringValue;
				line.Quantity = Convert.ToUInt32(row.GetCell(6).Value);
				line.SupplierCost = Convert.ToDecimal(row.GetCell(7).StringValue);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(row.GetCell(8).StringValue);
				line.Nds = Convert.ToUInt32(row.GetCell(9).Value);
				line.Producer = row.GetCell(14).StringValue;
				line.Country = row.GetCell(16).StringValue;
				line.ProducerCost = Convert.ToDecimal(row.GetCell(17).Value);
				line.Period = row.GetCell(22).StringValue;
				line.SerialNumber = row.GetCell(24).StringValue;
				line.Certificates = row.GetCell(25).StringValue;
				uint vi;
				line.VitallyImportant = UInt32.TryParse(row.GetCell(28).StringValue, out vi) && (vi == 1);
				decimal regCost;
				line.RegistryCost = Decimal.TryParse(row.GetCell(30).StringValue, out regCost) ? (decimal?)regCost : null;
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var workbook = Workbook.Load(file);
			var sheet = workbook.Worksheets[0];
			return (sheet.Cells[23, 1].StringValue.ToLower().Equals("товар")) &&
				   (sheet.Cells[23, 14].StringValue.ToLower().Equals("производитель")) &&
				   (sheet.Cells[23, 6].StringValue.ToLower().Equals("кол"));
		}
	}
}
