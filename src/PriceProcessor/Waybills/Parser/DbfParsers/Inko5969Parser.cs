using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Inko5969Parser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser().DocumentHeader(h => h.ProviderDocumentId, "NDOC") //NDOC – номер накладной
				.DocumentHeader(h => h.DocumentDate, "DDOC") //DDOC – дата накладной
				.Line(l => l.Product, "GOOD") //GOOD – наименование товара
				//Пропущено : PRODSBAR- всегда ноль
				.Line(l => l.SerialNumber, "SERIAL") //SERIAL – серия товара или номер партии или дата производства товара
				.Line(l => l.Period, "DATEB") //DATEB – дата окончания срока годности товара
				.Line(l => l.Certificates, "SERT") //SERT – сертификат на товар или иной другой документ подтверждающий регистрацию товара
				//Пропущено : NOMERA- НОМЕР АПТЕКИ, КУДА ПОСТАВЛЯЕТСЯ ТОВАР
				.Line(l => l.Producer, "ENTERP") //ENTERP – производитель товара
				.Line(l => l.Country, "COUNTRY") //COUNTRY – страна производитель
				//Пропущено : TYPEPUK – всегда пусто
				.Line(l => l.Quantity, "QUANT") //QUANT – количество товара
				.Line(l => l.Nds, "NDS") //NDS – НДС на данный товар
				//Пропущено : EDIZM- всегда пусто
				//Пропущено : KOEFED – всегда ноль
				.Line(l => l.SupplierCostWithoutNDS, "PRICEENT") //PRICEENT –цена ПОСТАВЩИКА за штуку без НДС
				//.Line(l => l.ProducerCostWithoutNDS, "PRICEWONDS")//PRICEWONDS – цена за штуку без НДС
				.Line(l => l.NdsAmount, "SUMNDS") //SUMNDS – сумма НДС за штуку
				.Line(l => l.Amount, "SUMSNDS") //SUMSNDS – общая сумма товара с НДС
				.Line(l => l.SupplierCost, "PRICEENTND") // PRICEENTND – Цена ПОСТАВЩИКА за штуку с НДС
				//.Line(l => l.ProducerCost, "CENASNDS")//CENASNDS – Цена за штуку с НДС
				.ToDocument(document, data);

			CheckAndFixSerialFormat(document.Lines);

			return document;
		}

		private void CheckAndFixSerialFormat(IList<DocumentLine> lines)
		{
			foreach (var documentLine in lines) {
				DateTime tmp;

				var serialNumber = documentLine.SerialNumber;

				if (DateTime.TryParse(serialNumber, out tmp)) {
					documentLine.SerialNumber = serialNumber.Substring(3);
				}
			}
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var columns = data.Columns;

			return columns[0].ColumnName.Equals("NDOC")
				&& columns[1].ColumnName.Equals("DDOC")
				&& columns.Contains("GOOD")
				&& columns.Contains("PRICEENTND")
				&& columns.Contains("CENASNDS");
		}
	}
}
