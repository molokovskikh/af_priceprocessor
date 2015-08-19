using System;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class SstExporterFixture
	{
		private Document document;
		private DocumentReceiveLog log;

		[SetUp]
		public void Setup()
		{
			log = new DocumentReceiveLog {
				Id = 100,
				Supplier = new Supplier {
					Id = 201,
					Name = "Тестовый поставщик"
				},
				DocumentType = DocType.Waybill,
				LogTime = DateTime.Now,
				ClientCode = 1001,
				Address = new Address {
					Id = 501,
					Org = new Org {
						FullName = "Тестовое юр.лицо"
					}
				}
			};
			document = new Document(log) {
				ProviderDocumentId = "001-01",
				DocumentDate = new DateTime(2014, 3, 7)
			};
			document.SetInvoice();
			document.NewLine(new DocumentLine {
				Product = "Алька-прим шип.таб. Х10",
				Code = "21603",
				SerialNumber = "S123S",
				Certificates = "200112^Паспорт (рус)^01.01.2012 ООО\"ОЦКК\"  Москва",
				Period = "01.12.2012",
				Producer = "Polfa/Polpharma",
				Country = "Польша",
				ProducerCost = 89.26m,
				SupplierCost = 89.26m,
				SupplierCostWithoutNDS = 98.19m,
				Quantity = 4,
				Nds = 10,
			});
			document.CalculateValues();
		}

		[Test]
		public void ExportProtekSstFile()
		{
			Exporter.Convert(document, log, WaybillFormat.Sst, new WaybillSettings());
			var resultFile = Path.GetFullPath(@"DocumentPath\501\waybills\100_Тестовый поставщик(001-01).sst");
			Assert.That(log.DocumentSize, Is.GreaterThan(0));
			Assert.That(log.FileName, Is.EqualTo("001-01.sst"));
			Assert.That(File.Exists(resultFile), Is.True, "файл накладной несуществует {0}", resultFile);

			var content = File.ReadAllText(resultFile, Encoding.GetEncoding(1251));
			Assert.That(content, Is.StringContaining("[Header]"));
			Assert.That(content, Is.StringContaining("001-01;07.03.2014;432.04;ПОСТАВКА;39.28;0;РУБЛЬ;;;;ЦВ Протек;;;;;;;"));
			Assert.That(content, Is.StringContaining("[Body]"));
			var index = content.IndexOf(";S123S");
			Assert.That(index, Is.GreaterThan(0));
			Assert.That(content, Is.StringContaining("200112^Паспорт (рус)^01.01.2012 ООО\"ОЦКК\"  Москва"));
		}

		[Test]
		public void Write_additional_fields()
		{
			document = new Document(log);
			document.SetInvoice();

			document.ProviderDocumentId = "24681251-001";
			document.DocumentDate = new DateTime(2012, 7, 17);
			document.Invoice.SellerName = "Протек";
			document.Invoice.BuyerId = 278659;
			document.Invoice.BuyerName = "УК Здоровые Люди г. Санкт-Петербург";
			document.Invoice.RecipientId = 278654;
			document.Invoice.RecipientName = "МСЦ г. Санкт-Петербург (Невский 114-116)";
			document.Invoice.DelayOfPaymentInBankDays = -1;
			document.Invoice.DelayOfPaymentInDays = 90;
			document.Invoice.CommissionFee = 0;
			document.Invoice.CommissionFeeContractId = "50-1/2011";

			var line = document.NewLine(new DocumentLine());
			line.Code = "16004";
			line.Product = "КАЛЬЦИЙ Д3 НИКОМЕД ФОРТЕ ТАБ. ЖЕВАТ. №120 С ЛИМОН. ВКУСОМ";
			line.Producer = "Nycomed Pharma";
			line.Country = "НОРВЕГИЯ";
			line.Quantity = 2;
			line.Nds = 10;
			line.SupplierCost = 448.95m;
			line.ProducerCostWithoutNDS = 381.44m;
			line.ExpireInMonths = 36;
			line.BillOfEntryNumber = "10130130/120512/0009561";
			line.Certificates = "POCC NO.ФM08.Д47572/Паспорт (рус)^^";
			line.SerialNumber = "10749253";
			line.DateOfManufacture = new DateTime(2011, 12, 1);
			line.Period = "01.12.2014";
			line.EAN13 = "5709932004838";
			line.CertificateAuthority = "ООО\"ОЦКК\"  Москва^ООО\"ОЦКК\"  Москва";
			line.Amount = 897.91m;
			line.NdsAmount = 81.63m;

			line = document.NewLine(new DocumentLine());
			line.Code = "1314";
			line.Product = "ЭГЛОНИЛ КАПС. 50МГ №30";
			line.VitallyImportant = true;
			line.Quantity = 1;
			line.SupplierCost = 175.13m;
			line.Nds = 10;
			document.CalculateValues();

			Exporter.Convert(document, log, WaybillFormat.SstLong, new WaybillSettings());
			document.Lines[0].CertificateAuthority = "Test";
			var resultFile = Path.GetFullPath(@"DocumentPath\501\waybills\100_Тестовый поставщик(24681251-001).sst");
			var content = File.ReadLines(resultFile, Encoding.GetEncoding(1251)).ToArray();
			Assert.That(content[0], Is.EqualTo("- Этим символом могут быть обозначены комментарии к файлу"));
			Assert.That(content[1], Is.EqualTo("- В следующей строке перечислены:"));
			Assert.That(content[2], Is.EqualTo("- Номер документа;Дата документа;Сумма с НДС по документу;Тип накладной;Cумма НДС 10%;Cумма НДС 18%;Тип валюты;Курс валюты;Ставка комиссионного вознаграждения;Номер договора комиссии;Наименование поставщика;Код плательщика;Наименование плательщика;Код получателя;Наименование получателя;Отсрочка платежа в банковских днях;Отсрочка платежа в календарных днях"));
			Assert.That(content[3], Is.EqualTo("[Header]"));
			Assert.That(content[4], Is.EqualTo("24681251-001;17.07.2012;1073.04;ПОСТАВКА;97.55;0;РУБЛЬ;;0;50-1/2011;Протек;278659;УК Здоровые Люди г. Санкт-Петербург;278654;МСЦ г. Санкт-Петербург (Невский 114-116);-1;90;"));
			Assert.That(content[5], Is.EqualTo("- В следующей строке перечислены:"));
			Assert.That(content[6], Is.EqualTo("- Код товара;Наименование товара;Производитель;Страна производителя;Количество;Цена с НДС;Цена производителя без НДС;Цена Протека без НДС;Резерв;Торговая надбавка оптового звена;Заводской срок годности в месяцах;ГТД;Серии сертификатов;Серия производителя;Дата выпуска препарата;Дата истекания срока годности данной серии;Штрих-код производителя;Дата регистрации цены  в реестре;Реестровая цена в рублях;Торговая наценка организации-импортера;Цена комиссионера с НДС;Комиссионное вознаграждение без НДС;НДС с комиссионного вознаграждения;Отпускная цена ЛБО;Стоимость позиции;Кто выдал сертификат;НДС;Сумма НДС;Цена производителя (в валюте, без НДС);Название валюты цены производителя (поля 36)"));
			Assert.That(content[7], Is.EqualTo("[Body]"));
			Assert.That(content[8], Is.EqualTo("16004;КАЛЬЦИЙ Д3 НИКОМЕД ФОРТЕ ТАБ. ЖЕВАТ. №120 С ЛИМОН. ВКУСОМ;Nycomed Pharma;НОРВЕГИЯ;2;448.95;381.44;408.14;448.95;7;36;10130130/120512/0009561;POCC NO.ФM08.Д47572/Паспорт (рус)^^;10749253;01.12.2011;01.12.2014;5709932004838;;;;;;;897.91;ООО\"ОЦКК\"  Москва^ООО\"ОЦКК\"  Москва;10;81.63;;"));
			Assert.That(content[9], Is.StringStarting("1314;ЭГЛОНИЛ КАПС. 50МГ №30 --ЖНиВЛС--;"));
		}
	}
}