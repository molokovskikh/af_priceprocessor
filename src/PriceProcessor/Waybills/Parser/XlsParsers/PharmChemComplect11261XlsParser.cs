using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelLibrary.BinaryFileFormat;
using ExcelLibrary.SpreadSheet;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XlsParsers
{
	public class PharmChemComplect11261XlsParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var book = Workbook.Load(file);
			var sheet = book.Worksheets[0];

			document.ProviderDocumentId = sheet.Cells[1, 6].StringValue;
			document.DocumentDate = sheet.Cells[1, 7].TryToGetValueAsDateTime();

			foreach (var row in sheet.Cells.Rows.Values.Skip(3)) {
				if ((String.IsNullOrEmpty(row.GetCell(0).StringValue) || (row.GetCell(0).StringValue.Equals("#NULL!"))) &&
					(String.IsNullOrEmpty(row.GetCell(8).StringValue) || (row.GetCell(8).StringValue.Equals("#NULL!"))) &&
					(String.IsNullOrEmpty(row.GetCell(9).StringValue) || (row.GetCell(9).StringValue.Equals("#NULL!"))))
					return document;

				var line = document.NewLine();

				line.Amount = Convert.ToDecimal(row.GetCell(13).Value);					//13 Сумма с НДС
				line.CertificateAuthority = row.GetCell(6).StringValue;					//6 Орган, выдавший документа качества
				line.Certificates = row.GetCell(7).StringValue;									//7 Информация о сертификате это строка что то вроде РОСС.NL.ФМ09.Д00778
				line.CertificatesDate = DateTimeFormat(row.GetCell(4));					//4 Дата выдачи сертификата сертификата
				line.CertificatesEndDate = row.GetCell(5).TryToGetValueAsDateTime(); //5 Срок действия сертификата, дата окончания
				line.Nds = Convert.ToUInt32(100 * Convert.ToDecimal(row.GetCell(11).Value));  //11 Ставка налога на добавленную стоимость
				line.NdsAmount = Convert.ToDecimal(row.GetCell(12).Value);			//12 Сумма НДС
				line.Period = DateTimeFormat(row.GetCell(3));										//3 Срок годности.А точнее Дата окончания срока годности.
				line.Product = row.GetCell(0).StringValue.Trim();								//0 Наименование продукта
				line.Quantity	= Convert.ToUInt32(row.GetCell(8).Value);         //8 Количество
				line.SerialNumber = row.GetCell(2).StringValue;									//2 Серийный номер продукта
				line.SupplierCostWithoutNDS = Convert.ToDecimal(row.GetCell(9).Value); //9 Цена поставщика без НДС
				line.VitallyImportant = row.GetCell(1).StringValue.Contains("да"); //1 Признак ЖНВЛС
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
			var workbook = Workbook.Load(file);
			var sheet = workbook.Worksheets[0];
			return (sheet.Cells[4, 1].StringValue.ToLower().Equals("ж/в признак")) &&
				(sheet.Cells[4, 7].StringValue.ToLower().Equals("сертификат товара")) &&
				(sheet.Cells[4, 11].StringValue.ToLower().Equals("ндс"));
		}

		private string DateTimeFormat(Cell c)
		{
			var d = c.TryToGetValueAsDateTime();
			if (d.HasValue)
				return d.Value.ToShortDateString();
			else {
				return "";
			}
		}

	}
}
