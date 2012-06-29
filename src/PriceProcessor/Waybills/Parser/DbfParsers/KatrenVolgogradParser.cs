using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenVolgogradParser : BaseDbfParser
	{

			/*
NDOC type="C" len="20" Номер накладной+
DATEDOC type="D" len="8" Дата накладной+
CODEPST type="C" len="12" Код препарата из справочника поставщика+
EAN13 type="C" len="30" Штрихкод производителя+
PRICE1 type="N" len="11" prec="4" Цена производителя+
PRICE2 type="N" len="11" prec="4" Цена оптовая с НДС-
PRICE2N type="N" len="11" prec="4" Цена оптовая без НДС-
PRCIMP type="N" len="11" prec="4" Наценка импортера-
PRCOPT type="N" len="11" prec="4" Наценка оптового звена-
QNT type="N" len="9" prec="2" Количество+
SER type="C" len="20" Серия +
GDATE type="D" len="8" Срок годности +
DateMade type="D" len="8" Дата заявки +-???
NAME type="C" len="80" Название препарата +
CNTR type="C" len="15" Название страны +
FIRM type="C" len="40" Название фирмы-производителя +
QNTPACK type="N" len="8" Количество в заводской упаковке -
NDS type="N" len="9" prec="2" Ставка НДС +
NSP type="N" len="9" prec="2" Ставка НСП -
GNVLS type="N" len="1" Признак ЖНВЛС +
REGPRC type="N" len="9" prec="2" Зарегистрированная цена +
DATEPRC type="D" len="8" Дата регистрации цены -
NUMGTD type="C" len="30" ГТД +
SERTIF type="C" len="80" Сертификата +
SERTDATE type="D" len="8" Срок действия сертификата +
SERTORG type="C" len="80" Орган сертификации - 
SUMPAY type="N" len="11" prec="4" Сумма по накладной с НДС (к оплате) +
SUMNDS10 type="N" len="11" prec="4" Сумма по накладной НДС 10% +
SUMNDS20 type="N" len="11" prec="4" Сумма по накладной НДС 18% +
SUM10 type="N" len="11" prec="4" Сумма товаров с признаком НДС 10% без НДС +
SUM20 type="N" len="11" prec="4" Сумма товаров с признаком НДС 18% без НДС +
SUM0 type="N" len="11" prec="4" Сумма товаров, не облагаемых НДС +
EXCHCODE type="N" len="1" Код валюты 0-руб. 1-$ 2-EU -
ERATE type="N" len="9" prec="4" Курс -
PODRCD type="C" len="12" Код грузополучателя в кодировке поставщика -
NUMZ type="N" len="8" Номер исходной заявки аптеки -
DATEZ type="D" len="8" Дата исходной заявки аптеки -
SUMITEM type="N" len="11" prec="4" Сумма по строке с НДС +
SUMS0 type="N" len="12" prec="2"  Сумма по строке НДС +
"CLIENTID" type="C" len="14" -
"DESTID" type="C" len="14" -
			 
			 */
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")

				.DocumentInvoice(i => i.Amount, "SUMPAY")
				.DocumentInvoice(i => i.InvoiceDate, "DATEMADE")
				.DocumentInvoice(i => i.Amount10, "SUMNDS10")
				.DocumentInvoice(i => i.Amount18, "SUMNDS20")
				.DocumentInvoice(i => i.AmountWithoutNDS10, "SUM10")
				.DocumentInvoice(i => i.AmountWithoutNDS18, "SUM20")
				.DocumentInvoice(i => i.AmountWithoutNDS0, "SUM0")

				.Line(l => l.Code, "CODEPST")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.ProducerCost, "PRICE1")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.Amount, "SUMITEM")
				.Line(l => l.NdsAmount, "SUMS0");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PRICE1")
					&& data.Columns.Contains("GNVLS")
					&& data.Columns.Contains("SUMITEM")
					&& data.Columns.Contains("SUMS0");
		}
	}
}
