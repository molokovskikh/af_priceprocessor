using System.Data;


namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class PustolyakovAA7759Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			/*
			Nom_dok ("s",11,0) - Номер накладной 
			Data_dok ("d",8 ,0) - Дата накладной
			Kod_TT ("s",9 ,0) - Комер Точки(Из нашего справочника ТТ, можем использовать ваши коды) -необходимо согласовать
			Adress_TT ("s",100 ,0); - Адрес Точки
			Kod_tov ("s",11,0); - Код товара(в нашей базе)
			Tovar ("s",80,0); - Название товара
			Stavka_NDS ("n",9, 0); - Ставка НДС (%)
			SERYA ("s",20,0); - Серия — зачастую не заполняется
			CenaSNDS ("n",9, 2); - Цена товара с НДС
			Summa ("n",9, 2); - Сумма товара с НДС
			Kol ("n",9, 2); - Количество (шт.)
			ShtrihKod ("s",13,0); - Штрихкод производителя
			Nomer_GTD ("s",30,0); - ГТД
			Strana ("s",15,0); - Название страны фирмы-производителя
			Proizv ("s",40,0); - Название фирмы-производителя
			Nomer_Sert ("s",20,0); - № сертификата
			Data_Sert ("d",8 ,0); - Дата сертификата
			Srok_Sert ("d",8 ,0); - Срок действия сертификата
			Kem_vidan ("s",80,0); - Кем выдан сертификат
			*/
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATA_DOK")
				.DocumentHeader(d => d.ProviderDocumentId, "NOM_DOK")
				.DocumentInvoice(i => i.BuyerAddress, "ADRESS_TT")
				.Line(l => l.Code, "KOD_TOV")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.Nds, "STAVKA_NDS")
				.Line(l => l.SerialNumber, "SERYA")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.EAN13, "SHTRIHKOD")
				.Line(l => l.BillOfEntryNumber, "NOMER_GTD")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Certificates, "NOMER_SERT")
				.Line(l => l.CertificatesDate, "DATA_SERT");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KOD_TOV")
				&& data.Columns.Contains("ADRESS_TT")
				&& data.Columns.Contains("Kod_TT")
				&& data.Columns.Contains("SERYA")
				&& data.Columns.Contains("SHTRIHKOD");
		}
	}
}