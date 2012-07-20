using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	/// <summary>
	/// Осуществляет сохранение накладной в SST формате.
	/// </summary>
	public class SstExporter
	{
		/// <summary>
		/// Сохраняет данные в файл.
		/// </summary>
		public static void Save(Document document)
		{
			document.Log.IsFake = false;
			var id = document.ProviderDocumentId;
			if (string.IsNullOrEmpty(id))
				id = document.Log.Id.ToString();

			document.Log.FileName = id + ".sst";
			var filename = document.Log.GetRemoteFileNameExt();

			using (var fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
				using (var sw = new StreamWriter(fs, Encoding.GetEncoding(1251))) {
					SaveToWriter(document, sw);
				}
			}

			document.Log.DocumentSize = new FileInfo(filename).Length;
		}

		public static void SaveToWriter(Document document, StreamWriter streamWriter)
		{
			streamWriter.WriteLine("[Header]");
			streamWriter.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};",
				//0_ : Код документа;
				document.ProviderDocumentId,
				//1 : Дата оформления документа;
				document.DocumentDate == null ? null : String.Format("{0:dd.MM.yyyy}", document.DocumentDate),
				//2_ Здесь должно быть : Сумма по документу со статусом "Отправлен в аптеку" (с НДС)
				document.Invoice == null ? null : document.Invoice.Amount,
				//3_ Здесь должно быть : Тип поставки ("КОМИССИЯ" или "ПОСТАВКА ");
				"ПОСТАВКА",
				//4_  Сумма НДС 10%;
				document.Invoice == null ? null : NullableDecimalToString(document.Invoice.NDSAmount10),
				//5_  Сумма НДС 18%;
				document.Invoice == null ? null : NullableDecimalToString(document.Invoice.NDSAmount18),
				//6_ Здесь должно быть : Тип валюты. Зарезервированные слова "РУБЛЬ", "ДОЛЛАР";
				"РУБЛЬ",
				//7_ Здесь должно быть : Курс (коэффициент пересчета в рубли);
				null,
				//8_ Здесь должно быть : Ставка комиссионного вознаграждения;
				document.Invoice == null ? null : document.Invoice.CommissionFee,
				//9_ Здесь должно быть : Номер договора комиссии;
				document.Invoice == null ? null : document.Invoice.CommissionFeeContractId,
				//10_ Здесь должно быть : Наименование поставщика ("ЦВ Протек");
				document.Invoice == null ? null : document.Invoice.SellerName,
				//11_ Здесь должно быть : Код плательщика;
				document.Invoice == null ? null : document.Invoice.BuyerId,
				//12_ Здесь должно быть : Наименование плательщика;
				document.Invoice == null ? null : document.Invoice.BuyerName,
				//13_ Здесь должно быть : Код получателя;
				document.Invoice == null ? null : document.Invoice.RecipientId,
				//14_ Здесь должно быть : Наименование получателя;
				document.Invoice == null ? null : document.Invoice.RecipientName,
				//15_ Здесь должно быть : Отсрочка платежа в банковских днях;
				document.Invoice == null ? null : document.Invoice.DelayOfPaymentInBankDays,
				//16_ Здесь должно быть : Отсрочка платежа в календарных днях.
				document.Invoice == null ? null : document.Invoice.DelayOfPaymentInDays
			);

			streamWriter.WriteLine("[Body]");

			foreach (var line in document.Lines) {
				var items = new object[] {
					line.Code, //0_ Код препарата в ЦВ Протек;
					line.Product == null ? null : line.Product.Slice(128).ToUpper(), //1_ Название препарата в верхнем регистре;
					line.Producer.Slice(64), //2_ Название производителя препарата;
					line.Country.Slice(15), //3_ Название страны производителя;
					line.Quantity, //4_ Количество;
					line.Amount, //5_ Итоговая цена (цена Протека с НДС);
					line.ProducerCostWithoutNDS, //6_ Цена производителя без НДС;
					line.SupplierCostWithoutNDS, //7_ Цена поставщика без НДС (цена Протека без НДС);
					line.SupplierCost, //8_ Цена поставщика с НДС (Резерв);
					line.SupplierPriceMarkup, //9_ Наценка посредника (Торговая надбавка оптового звена);
					line.ExpireInMonths, //10_ Заводской срок годности в месяцах;
					line.BillOfEntryNumber.Slice(30), //11_ Грузовая Таможенная Декларация (ГТД);
					GetSNumAndCrtAndCrtDate(line), //12_ Блок, описывающий следующие параметры:
					/*
						Серия препарата (в конце разделитель ^),
						Регистрационный номер сертификата (в конце разделитель ^),
						Дата и орган, выдавший сертификат
					*/
					line.SerialNumber.Slice(35), //13_ Здесь должно быть : Серия производителя
					line.DateOfManufacture, //14_ Здесь должно быть : Дата выпуска препарата;
					line.Period, //15_ Здесь должно быть : Дата истекания срока годности данной серии;
					line.EAN13.Slice(25), //16_ Штрих-код производителя;
					line.RegistryDate, //17_ Здесь должно быть : Дата регистрации цены  в реестре;
					line.RegistryCost, //18_  Реестровая цена  в рублях;
					null, //19_ Здесь должно быть : Торговая наценка организации-импортера;
					null, //20_ Здесь должно быть : Цена комиссионера, вкючая НДС
					null, //21_ Здесь должно быть : Комисионное вознаграждение без НДС
					null, //Отпускная цена ЛБО
					line.Amount, //Стоимость позиции
					line.CertificateAuthority, //Кто выдал сертификат
					line.Nds, //НДС
					line.NdsAmount, //Сумма НДС
					null, //Цена производителя (в валюте, без НДС)
					null //Название валюты цены производителя (поля 36)
				};
				streamWriter.WriteLine(String.Join(";", items.Select(ConvertValue)));
			}
		}

		private static object ConvertValue(object o)
		{
			if (o == null)
				return null;
			if (o is decimal?)
				return NullableDecimalToString((decimal?)o);
			if (o is DateTime?) {
				var dateTime = ((DateTime?) o);
				return dateTime.HasValue ? dateTime.Value.ToShortDateString() : null;
			}
			return o;
		}

		private static string GetSNumAndCrtAndCrtDate(DocumentLine line)
		{
			var str = String.Empty;
			char delimeter = (char)127;

			if (line.SerialNumber != null)
				str += line.SerialNumber;
			str += delimeter;
			if (line.Certificates != null)
				str += line.Certificates;
			str += delimeter;
			if (line.CertificatesDate != null)
				str += line.CertificatesDate;

			return str;
		}
		
		private static string NullableDecimalToString(decimal? value)
		{
			return value.HasValue ? DecimalToString(value.Value) : null;
		}

		private static string DecimalToString(decimal value)
		{
			return value.ToString("0.##", CultureInfo.InvariantCulture);
		}
	}
}