using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	public enum WaybillFormat
	{
		[Description("SST")]
		SST = 0,
		[Description("DBF")]
		DBF = 1,
		SSTLong = 2
	}


	/// <summary>
	/// Осуществляет сохранение накладной в SST формате.
	/// </summary>
	public class Exporter
	{
		/// <summary>
		/// Сохраняет данные в файл.
		/// </summary>
		public static void Save(Document document, WaybillFormat type)
		{
			document.Log.IsFake = false;
			var id = document.ProviderDocumentId;
			if (string.IsNullOrEmpty(id))
				id = document.Log.Id.ToString();

			if (type == WaybillFormat.DBF)
				document.Log.FileName = id + ".dbf";
			else
				document.Log.FileName = id + ".sst";
			var filename = document.Log.GetRemoteFileNameExt();

			if (type == WaybillFormat.DBF) {
				DbfExporter.SaveProtek(document, filename);
			} else {
				using (var fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
					using (var sw = new StreamWriter(fs, Encoding.GetEncoding(1251))) {
						if (type == WaybillFormat.SST)
							SaveShort(document, sw);
						else
							SaveLong(document, sw);
					}
				}
			}

			document.Log.DocumentSize = new FileInfo(filename).Length;
		}

		public static void SaveShort(Document document, StreamWriter streamWriter)
		{
			streamWriter.WriteLine("[Header]");
			streamWriter.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};",
				document.ProviderDocumentId, //0_ : Код документа;
				document.DocumentDate == null ? null : String.Format("{0:dd.MM.yyyy}", document.DocumentDate),
				//1 : Дата оформления документа;
				null, //2_ Здесь должно быть : Сумма по документу со статусом "Отправлен ваптеку" (с НДС)
				null, //3_ Здесь должно быть : Тип поставки ("КОМИССИЯ" или "ПОСТАВКА ");
				document.Invoice == null ? null : NullableDecimalToString(document.Invoice.NDSAmount10), //4_  Сумма НДС 10%;
				document.Invoice == null ? null : NullableDecimalToString(document.Invoice.NDSAmount18), //5_  Сумма НДС 18%;
				null, //6_ Здесь должно быть : Тип валюты. Зарезервированные слова "РУБЛЬ", "ДОЛЛАР";
				null, //7_ Здесь должно быть : Курс (коэффициент пересчета в рубли);
				null, //8_ Здесь должно быть : Ставка комиссионного вознаграждения;
				null, //9_ Здесь должно быть : Номер договора комиссии;
				null, //10_ Здесь должно быть : Наименование поставщика ("ЦВ Протек");
				null, //11_ Здесь должно быть : Код плательщика;
				null, //12_ Здесь должно быть : Наименование плательщика;
				null, //13_ Здесь должно быть : Код получателя;
				null, //14_ Здесь должно быть : Наименование получателя;
				null, //15_ Здесь должно быть : Отсрочка платежа в банковских днях;
				null //16_ Здесь должно быть : Отсрочка платежа в календарных днях.
				);

			streamWriter.WriteLine("[Body]");

			foreach (var line in document.Lines) {
				streamWriter.WriteLine(
					"{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};",
					line.Code, //0_ Код препарата в ЦВ Протек;
					line.Product == null ? null : line.Product.Slice(128).ToUpper(), //1_ Название препарата в верхнем регистре;
					line.Producer.Slice(64), //2_ Название производителя препарата;
					line.Country.Slice(15), //3_ Название страны производителя;
					line.Quantity, //4_ Количество;
					NullableDecimalToString(line.Amount), //5_ Итоговая цена (цена Протека с НДС);
					NullableDecimalToString(line.ProducerCostWithoutNDS), //6_ Цена производителя без НДС;
					NullableDecimalToString(line.SupplierCostWithoutNDS), //7_ Цена поставщика без НДС (цена Протека без НДС);
					NullableDecimalToString(line.SupplierCost), //8_ Цена поставщика с НДС (Резерв);
					NullableDecimalToString(line.SupplierPriceMarkup), //9_ Наценка посредника (Торговая надбавка оптового звена);
					line.Period, //10_ Заводской срок годности в месяцах;
					line.BillOfEntryNumber.Slice(30), //11_ Грузовая Таможенная Декларация (ГТД);
					GetSNumAndCrtAndCrtDate(line), //12_ Блок, описывающий следующие параметры:
					/*		Серия препарата (в конце разделитель ^),
							Регистрационный номер сертификата (в конце разделитель ^),
							Дата и орган, выдавший сертификат*/
					line.SerialNumber.Slice(35), //13_ Здесь должно быть : Серия производителя
					null, //14_ Здесь должно быть : Дата выпуска препарата;
					null, //15_ Здесь должно быть : Дата истекания срока годности данной серии;
					line.EAN13.Slice(25), //16_ Штрих-код производителя;
					null, //17_ Здесь должно быть : Дата регистрации цены  в реестре;
					NullableDecimalToString(line.RegistryCost), //18_  Реестровая цена  в рублях;
					null, //19_ Здесь должно быть : Торговая наценка организации-импортера;
					null, //20_ Здесь должно быть : Цена комиссионера, вкючая НДС
					null, //21_ Здесь должно быть : Комисионное вознаграждение без НДС
					null //22_ Здесь должно быть : ДС с комисионного вознаграждения
					);
			}
		}

		public static void SaveLong(Document document, StreamWriter streamWriter)
		{
			streamWriter.WriteLine("- Этим символом могут быть обозначены комментарии к файлу");
			streamWriter.WriteLine("- В следующей строке перечислены:");
			streamWriter.WriteLine("- Номер документа;Дата документа;Сумма с НДС по документу;Тип накладной;Cумма НДС 10%;Cумма НДС 18%;Тип валюты;Курс валюты;Ставка комиссионного вознаграждения;Номер договора комиссии;Наименование поставщика;Код плательщика;Наименование плательщика;Код получателя;Наименование получателя;Отсрочка платежа в банковских днях;Отсрочка платежа в календарных днях");
			streamWriter.WriteLine("[Header]");

			var items = new object[] {
				//0_ : Код документа;
				document.ProviderDocumentId,
				//1 : Дата оформления документа;
				document.DocumentDate,
				//2_ Здесь должно быть : Сумма по документу со статусом "Отправлен в аптеку" (с НДС)
				document.Invoice == null ? null : document.Invoice.Amount,
				//3_ Здесь должно быть : Тип поставки ("КОМИССИЯ" или "ПОСТАВКА ");
				"ПОСТАВКА",
				//4_  Сумма НДС 10%;
				document.Invoice == null ? null : document.Invoice.NDSAmount10,
				//5_  Сумма НДС 18%;
				document.Invoice == null ? null : document.Invoice.NDSAmount18,
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
			};

			streamWriter.WriteLine(String.Join(";", items.Select(ConvertValue)) + ";");
			streamWriter.WriteLine("- В следующей строке перечислены:");
			streamWriter.WriteLine("- Код товара;Наименование товара;Производитель;Страна производителя;Количество;Цена с НДС;Цена производителя без НДС;Цена Протека без НДС;Резерв;Торговая надбавка оптового звена;Заводской срок годности в месяцах;ГТД;Серии сертификатов;Серия производителя;Дата выпуска препарата;Дата истекания срока годности данной серии;Штрих-код производителя;Дата регистрации цены  в реестре;Реестровая цена в рублях;Торговая наценка организации-импортера;Цена комиссионера с НДС;Комиссионное вознаграждение без НДС;НДС с комиссионного вознаграждения;Отпускная цена ЛБО;Стоимость позиции;Кто выдал сертификат;НДС;Сумма НДС;Цена производителя (в валюте, без НДС);Название валюты цены производителя (поля 36)");
			streamWriter.WriteLine("[Body]");

			foreach (var line in document.Lines) {
				items = new object[] {
					line.Code, //0_ Код препарата в ЦВ Протек;
					line.Product == null ? null : line.Product.Slice(128).ToUpper(), //1_ Название препарата в верхнем регистре;
					line.Producer, //2_ Название производителя препарата;
					line.Country, //3_ Название страны производителя;
					line.Quantity, //4_ Количество;
					line.SupplierCost, //5_ Итоговая цена (цена Протека с НДС);
					line.ProducerCostWithoutNDS, //6_ Цена производителя без НДС;
					line.SupplierCostWithoutNDS, //7_ Цена поставщика без НДС (цена Протека без НДС);
					line.SupplierCost, //8_ Цена поставщика с НДС (Резерв);
					line.SupplierPriceMarkup, //9_ Наценка посредника (Торговая надбавка оптового звена);
					line.ExpireInMonths, //10_ Заводской срок годности в месяцах;
					line.BillOfEntryNumber, //11_ Грузовая Таможенная Декларация (ГТД);
					line.Certificates, //12_ Блок, описывающий следующие параметры:
					line.SerialNumber, //13_ Здесь должно быть : Серия производителя
					line.DateOfManufacture, //14_ Здесь должно быть : Дата выпуска препарата;
					line.Period, //15_ Здесь должно быть : Дата истекания срока годности данной серии;
					line.EAN13, //16_ Штрих-код производителя;
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
				};
				streamWriter.WriteLine(String.Join(";", items.Select(ConvertValue)) + ";");
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