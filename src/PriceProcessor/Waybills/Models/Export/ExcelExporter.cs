using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XWPF.UserModel;
namespace Inforoom.PriceProcessor.Waybills.Models.Export
{
	public class ExcelExporter
	{
		public static void SaveLipetskFarmacia(Document document, string filename)
		{
			using (FileStream sw = File.Create(filename)) {
				var workbook = new HSSFWorkbook();
				ISheet sheet1 = workbook.CreateSheet("Sheet1");
				var row = sheet1.CreateRow(0);
				row.CreateCell(0).SetCellValue("№ пп");
				row.CreateCell(1).SetCellValue("Наименование и краткая характеристика товара");
				row.CreateCell(2).SetCellValue("Серия товара Сертификат");
				row.CreateCell(3).SetCellValue("Срок годности");
				row.CreateCell(4).SetCellValue("Производитель");
				row.CreateCell(5).SetCellValue("Цена без НДС, руб");
				row.CreateCell(6).SetCellValue("Затребован.колич.");
				row.CreateCell(7).SetCellValue("Опт. надб. %");
				row.CreateCell(8).SetCellValue("Отпуск. цена пос-ка без НДС, руб");
				row.CreateCell(9).SetCellValue("НДС пос-ка, руб");
				row.CreateCell(10).SetCellValue("Отпуск. цена пос-ка с НДС, руб");
				row.CreateCell(11).SetCellValue("Розн. торг. надб. %");
				row.CreateCell(12).SetCellValue("Розн. цена за ед., руб");
				row.CreateCell(13).SetCellValue("Кол-во");
				row.CreateCell(14).SetCellValue("Розн. сумма, руб");
				row.CreateCell(15).SetCellValue("Штрихкод");
				row.CreateCell(16).SetCellValue("Код страны");
				row.CreateCell(17).SetCellValue("Код единицы");
				row.CreateCell(18).SetCellValue("ГТД");

				var i = 0;
				foreach (var line in document.Lines) {
					row = sheet1.CreateRow(++i);
					row.CreateCell(0).SetCellValue(i);
					row.CreateCell(1).SetCellValue(line.Product);
					row.CreateCell(2).SetCellValue(line.Certificates);
					row.CreateCell(3).SetCellValue(line.Period);
					row.CreateCell(4).SetCellValue(line.Producer);
					row.CreateCell(5).SetCellValue(line.ProducerCostWithoutNDS.ToString()); //цена без ндс это именно цена производителя?
					row.CreateCell(6).SetCellValue(line.Quantity.ToString()); //как узнать затребованное кол-во?
					row.CreateCell(7).SetCellValue(""); //что такое оптовая надбавка?
					row.CreateCell(8).SetCellValue(line.SupplierCostWithoutNDS.ToString()); //то ли это? не уверен, что отпускная
					row.CreateCell(9).SetCellValue(line.NdsAmount.ToString());
					row.CreateCell(10).SetCellValue(line.SupplierCost.ToString()); //то ли это? не уверен, что отпускная
					row.CreateCell(11).SetCellValue(""); //что такое розничная торговая надбавка?
					row.CreateCell(12).SetCellValue(line.RetailCost.ToString());
					row.CreateCell(13).SetCellValue(line.Quantity.ToString());
					row.CreateCell(14).SetCellValue(line.Amount.ToString());
					row.CreateCell(15).SetCellValue(line.EAN13);
					row.CreateCell(16).SetCellValue(line.CountryCode);
					row.CreateCell(17).SetCellValue(line.UnitCode);
					row.CreateCell(18).SetCellValue(line.BillOfEntryNumber);
				}
				workbook.Write(sw);
			}
		}
	}
}
