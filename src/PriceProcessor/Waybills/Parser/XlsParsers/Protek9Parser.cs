using System;
using System.Linq;
using ExcelLibrary.SpreadSheet;

namespace Inforoom.PriceProcessor.Waybills.Parser.XlsParsers
{
	public class Protek9Parser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var book = Workbook.Load(file);
			var sheet = book.Worksheets[0];
			var cell = sheet.Cells[2, 0];
			var value = cell.StringValue;

			var separator = value.IndexOf(" от ");
			document.ProviderDocumentId = value.Substring(0, separator).Replace("Счет №", "");
			document.DocumentDate = DateTime.Parse(value.Substring(separator, value.Length - separator).Replace(" от ", ""));
			foreach(var row in sheet.Cells.Rows.Values.Skip(4))
			{
				var line = document.NewLine();
				line.Product = row.GetCell(1).StringValue;
				line.Producer = row.GetCell(2).StringValue;
				line.RegistryCost = Convert.ToDecimal(row.GetCell(3).Value);
				line.SupplierCost = Convert.ToDecimal(row.GetCell(4).StringValue);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(row.GetCell(5).StringValue);
				line.Quantity = Convert.ToUInt32(row.GetCell(6).Value);
				line.ProducerCost = Convert.ToDecimal(row.GetCell(9).Value);
				line.SerialNumber = row.GetCell(10).StringValue;
				line.Certificates = row.GetCell(11).StringValue;
				line.Country = row.GetCell(13).StringValue;
				line.Nds = Convert.ToUInt32(row.GetCell(14).Value);
				line.VitallyImportant = Convert.ToUInt32(row.GetCell(17).Value) == 1;
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var workbook = Workbook.Load(file);
			var sheet = workbook.Worksheets[0];
			return (sheet.Cells[5, 1].StringValue.ToLower().Equals("наименование")) &&
			       (sheet.Cells[5, 2].StringValue.ToLower().Equals("производитель")) &&
			       (sheet.Cells[5, 4].StringValue.ToLower().Equals("цена с ндс"));
		}
	}
}