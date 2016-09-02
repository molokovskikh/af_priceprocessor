using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser.SstParsers
{
	// Номер документа;Дата документа;Сумма с НДС по документу;Тип накладной;Cумма НДС 10%;Cумма НДС 18%;Тип валюты;Курс валюты;Ставка комиссионного вознаграждения;Номер договора комиссии;Наименование поставщика; Код плательщика;Наименование плательщика;Код получателя;Наименование получателя;Отсрочка платежа в банковских днях;Отсрочка платежа в календарных днях;
	// Код товара;Наименование товара;Производитель;Страна производителя;Количество;Цена с НДС;Цена производителя без НДС;Цена Протека без НДС;Резерв;Торговая надбавка оптового звена;Заводской срок годности в месяцах;ГТД;Серии сертификатов;Серия производителя;Дата выпуска препарата;Дата истекания срока годности данной серии;Штрих-код производителя;Дата регистрации цены  в реестре;Реестровая цена в рублях;Торговая наценка организации-импортера;Цена комиссионера с НДС;Комиссионное вознаграждение без НДС;НДС с комиссионного вознаграждения;Отпускная цена ЛБО;Стоимость позиции;Цена производителя с НДС;Признак ЖНВЛС;СтавкаНДС;
	public class Protek3Parser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			CommentMark = "-";

			ProviderDocumentIdIndex = 0;
			DocumentDateIndex = 1;

			InvoiceNDSAmountIndex = 2;
			//Тип накладной
			NDSAmount10Index = 4;
			NDSAmount18Index = 5;
			//Тип валюты
			//Курс валюты
			CommissionFeeIndex = 8;
			CommissionFeeContractIdIndex = 9;
			//Наименование плательщика
			SupplierNameIndex = 10;
			BuyerIdIndex = 11;
			BuyerNameIndex = 12;
			RecipientIdIndex = 13;
			RecipientNameIndex = 14;
			DelayOfPaymentInBankDaysIndex = 15;
			DelayOfPaymentInDaysIndex = 16;

			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			SupplierCostIndex = 5;
			ProducerCostWithoutNdsIndex = 6;
			SupplierCostWithoutNdsIndex = 7;
			//Резерв
			SupplierPriceMarkupIndex = 9;
			ExpireInMonthsIndex = 10;
			BillOfEntryNumberIndex = 11;
			CertificatesIndex = 12;
			SerialNumberIndex = 13;
			DateOfManufactureIndex = 14;
			PeriodIndex = 15;
			EAN13Index = 16;
			RegistryDateIndex = 17;
			RegistryCostIndex = 18;
			//Торговая наценка организации-импортера
			//Цена комиссионера с НДС
			//Комиссионное вознаграждение без НДС
			//НДС с комиссионного вознаграждения
			//Отпускная цена ЛБО
			AmountIndex = 24;
			ProducerCostIndex = 25;
			VitallyImportantIndex = 26;
			NdsIndex = 27;

		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				string line = reader.ReadLine();
				while (line != null) {
					if (line.ToLower().Equals("[header]"))
						break;
					line = reader.ReadLine();
				}
				if (line == null)
					return false;
				var header = reader.ReadLine().Split(';');
				if ((header.Length != 18))
					return false;
				line = reader.ReadLine();
				while (line != null) {
					if (line.ToLower().Equals("[body]"))
						break;
					line = reader.ReadLine();
				}
				if (line == null)
					return false;
				var body = reader.ReadLine().Split(';');
				if (body.Length != 29)
					return false;
			}
			return true;
		}
	}
}