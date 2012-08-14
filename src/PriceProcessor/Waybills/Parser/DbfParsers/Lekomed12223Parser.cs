using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Lekomed12223Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			/*
			N1 Идентификатор поставщика> () +i.SellerName
			N2 <Дата документа>+d.DocumentDate
			N3 <Номер документа>+d.ProviderDocumentId
			N4 <Идентификатор номенклатурной единицы>+l.Code
			N5 <Наименование номенклатурной единицы>+l.Product
			N6 <Штрих код>+l => EAN13
			N7 <Ставка НДС>+l.Nds
			N8 <Номер сертификата>+l.Certificates
			N9 <Дата выдачи сертификата>+l.CertificatesDate
			N10 <Дата окончания действия сертификата>- совпадает со сроком годности
			N11 <Страна>+l.Country
			N12 <ГТД>+l.BillOfEntryNumber
			N13 <Производитель>+l.Producer
			N14 <Серия производителя>+l.SerialNumber
			N15 <Дата изготовления>?
			N16 <Дата истечения срока годности>+l.Period
			N17 <Цена производителя без НДС>+l.ProducerCostWithoutNDS
			N18 <Цена производителя с НДС>+l.ProducerCost
			N19 <Цена поставщика без НДС>+l.SupplierCostWithoutNDS
			N20 <Цена поставщика с НДС>+l.SupplierCost
			N21 <Количество>+l.Quantity
			N22 <Сумма поставщика без НДС>-
			N23 <Сумма поставщика с НДС>+l.Amount
			N24 <Декларант>-
			N25 <ЖВП>+l.VitallyImportant	
			N26 <Зарегистрированная предельная отпускная цена производителя в руб>*+l.RegistryCost
			N27 <Зарегистрированная предельная отпускная цена производителя в валюте>*-
			N28 <Код валюты>*-
			N29 <Фактическая отпускная цена производителя без НДС в руб>**-
			*/
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "N2")
				.DocumentHeader(d => d.ProviderDocumentId, "N3")
				.DocumentInvoice(i => i.SellerName, "N1")
				.Line(l => l.Code, "N4")
				.Line(l => l.Product, "N5")
				.Line(l => l.EAN13, "N6")
				.Line(l => l.Nds, "N7")
				.Line(l => l.Certificates, "N8")
				.Line(l => l.CertificatesDate, "N9") //дата выдачи
				.Line(l => l.Country, "N11")
				.Line(l => l.BillOfEntryNumber, "N12")
				.Line(l => l.Producer, "N13")
				.Line(l => l.SerialNumber, "N14")
				.Line(l => l.Period, "N16")
				.Line(l => l.ProducerCostWithoutNDS, "N17")
				.Line(l => l.ProducerCost, "N18")
				.Line(l => l.SupplierCostWithoutNDS, "N19")
				.Line(l => l.SupplierCost, "N20")
				.Line(l => l.Quantity, "N21")
				.Line(l => l.Amount, "N23")
				.Line(l => l.VitallyImportant, "N25")
				.Line(l => l.RegistryCost, "N26");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return
				data.Columns[0].ColumnName == "N1"
					&& data.Columns.Contains("N8")
					&& data.Columns.Contains("N9")
					&& data.Columns.Contains("N17")
					&& data.Columns.Contains("N23")
					&& data.Columns.Contains("N26")
					&& data.Columns.Contains("N29");
		}
	}
}