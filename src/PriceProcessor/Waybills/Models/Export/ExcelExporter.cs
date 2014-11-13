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

				var row = sheet1.CreateRow(1);
				row.CreateCell(5).SetCellValue("Наименование организации: Представитель АК \"Инфорум\", г.Воронеж, Ленинский пр-т.160, офис 415");
				
				row = sheet1.CreateRow(2);
				row.CreateCell(2).SetCellValue("Отдел:");
				row.CreateCell(3).SetCellValue("_______________________________________");

				row = sheet1.CreateRow(3);
				row.CreateCell(0).SetCellValue("Требование №");
				row.CreateCell(1).SetCellValue("_______________________");
				row.CreateCell(5).SetCellValue("Накладная №");
				row.CreateCell(6).SetCellValue("_______________________");

				row = sheet1.CreateRow(4);
				row.CreateCell(1).SetCellValue("от \"___\"_________________20___г");
				row.CreateCell(6).SetCellValue("от \"___\"_________________20___г");

				row = sheet1.CreateRow(5);
				row.CreateCell(0).SetCellValue("Кому: Аптечный пункт");
				row.CreateCell(1).SetCellValue("_______________________");
				row.CreateCell(5).SetCellValue("Через кого");
				row.CreateCell(6).SetCellValue("_______________________");

				row = sheet1.CreateRow(6);
				row.CreateCell(0).SetCellValue("Основание отпуска");
				row.CreateCell(1).SetCellValue("_______________________");
				row.CreateCell(5).SetCellValue("Доверенность №_____");
				row.CreateCell(6).SetCellValue("от \"___\"_________________20___г");

				row = sheet1.CreateRow(8);
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

				var i = 8;
				var lines = document.Lines.OrderBy(b=>b.Id);
				foreach (var line in lines) {
					row = sheet1.CreateRow(++i);
					row.CreateCell(0).SetCellValue(i-8);
					row.CreateCell(1).SetCellValue(line.Product);
					row.CreateCell(2).SetCellValue(line.Certificates);
					row.CreateCell(3).SetCellValue(line.Period);
					row.CreateCell(4).SetCellValue(line.Producer);
					row.CreateCell(5).SetCellValue(line.ProducerCostWithoutNDS.ToString()); 
					row.CreateCell(6).SetCellValue(line.Quantity.ToString()); //сложно вычисляется с ордерами
					row.CreateCell(7).SetCellValue(line.SupplierPriceMarkup.ToString());
					row.CreateCell(8).SetCellValue(line.SupplierCostWithoutNDS.ToString()); 
					row.CreateCell(9).SetCellValue(line.Nds.ToString());
					row.CreateCell(10).SetCellValue(line.SupplierCost.ToString()); 
					row.CreateCell(11).SetCellValue(""); //нельзя вычислить на клиенте
					row.CreateCell(12).SetCellValue(line.RetailCost.ToString());
					row.CreateCell(13).SetCellValue(line.Quantity.ToString());
					row.CreateCell(14).SetCellValue("");
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
