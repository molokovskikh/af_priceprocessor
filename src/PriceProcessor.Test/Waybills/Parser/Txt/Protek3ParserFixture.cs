using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;


namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class Protek3ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("66909869-001.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(18));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("66909869-001"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("25.08.2016")));

			Assert.That(doc.Invoice.NDSAmount, Is.EqualTo(3947.82m));
			//Тип накладной
			Assert.That(doc.Invoice.NDSAmount10, Is.EqualTo(337.70m));
			Assert.That(doc.Invoice.NDSAmount18, Is.EqualTo(35.56m));
			//Тип валюты
			//Курс валюты
			Assert.That(doc.Invoice.CommissionFee, Is.Null);
			Assert.That(doc.Invoice.CommissionFeeContractId, Is.EqualTo("0.00"));
			//Наименование плательщика
			Assert.That(doc.Invoice.BuyerId, Is.EqualTo(135192));
			Assert.That(doc.Invoice.BuyerName, Is.EqualTo("ИП Ланзат Н.Б."));
			Assert.That(doc.Invoice.RecipientId, Is.EqualTo(260815));
			Assert.That(doc.Invoice.RecipientName, Is.EqualTo("ул. Дзержинского, 79 (павильон № 4)"));
			Assert.That(doc.Invoice.DelayOfPaymentInBankDays, Is.Null);
			Assert.That(doc.Invoice.DelayOfPaymentInDays, Is.Null);


			Assert.That(doc.Lines[0].Code, Is.EqualTo("30538"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Твинс Тэк"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("РОССИЯ"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2.00m));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(60.06m));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(47.79m));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(50.90m));
			//Резерв
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0.00m));
			Assert.That(doc.Lines[0].ExpireInMonths, Is.EqualTo(24));
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.Null);
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("0516^TC RU Д-RU.AB07.B.00135^20.12.2012^Таможенный союз РБ,РК и РФ0516^POCC RU.ДП.3716.П.RU.0021^13.06.2011^ООО \"БИОСАН-ГИД\"0516^77.99.40.915.Д.010532.06.10^29.06.2010^ФСН ППиБЧ"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("0516"));
			Assert.That(doc.Lines[0].DateOfManufacture, Is.EqualTo(Convert.ToDateTime("01.05.2016")));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.05.2018"));
			Assert.That(doc.Lines[0].EAN13, Is.EqualTo("4607010242558"));
			Assert.That(doc.Lines[0].RegistryDate, Is.Null);
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0.00m));
			//Торговая наценка организации-импортера
			//Цена комиссионера с НДС
			//Комиссионное вознаграждение без НДС
			//НДС с комиссионного вознаграждения
			//Отпускная цена ЛБО
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(120.12m));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(56.40m));
			Assert.That(doc.Lines[0].VitallyImportant, Is.EqualTo(false));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(18.00m));


			Assert.That(doc.Lines[7].Code, Is.EqualTo("652"));
			Assert.That(doc.Lines[7].Product, Is.EqualTo("ВЕНТОЛИН АЭР. Д/ИНГ. ДОЗИР. 0,1МГ/ДОЗА 200 ДОЗ №1"));
			Assert.That(doc.Lines[7].Producer, Is.EqualTo("Glaxo Wellcome Production"));
			Assert.That(doc.Lines[7].Country, Is.EqualTo("ФРАНЦИЯ"));
			Assert.That(doc.Lines[7].Quantity, Is.EqualTo(3.00m));
			Assert.That(doc.Lines[7].SupplierCost, Is.EqualTo(126.94));
			Assert.That(doc.Lines[7].ProducerCostWithoutNDS, Is.EqualTo(107.41m));
			Assert.That(doc.Lines[7].SupplierCostWithoutNDS, Is.EqualTo(115.40m));
			//Резерв
			Assert.That(doc.Lines[7].SupplierPriceMarkup, Is.EqualTo(0.00m));
			Assert.That(doc.Lines[7].ExpireInMonths, Is.EqualTo(24));
			Assert.That(doc.Lines[7].BillOfEntryNumber, Is.EqualTo("10130130/180516/0006781/11"));
			Assert.That(doc.Lines[7].Certificates, Is.Null);
			Assert.That(doc.Lines[7].SerialNumber, Is.EqualTo("WW7N"));
			Assert.That(doc.Lines[7].DateOfManufacture, Is.EqualTo(Convert.ToDateTime("01.03.2016")));
			Assert.That(doc.Lines[7].Period, Is.EqualTo("01.03.2018"));
			Assert.That(doc.Lines[7].EAN13, Is.EqualTo("4607008131857"));
			Assert.That(doc.Lines[7].RegistryDate, Is.Null);
			Assert.That(doc.Lines[7].RegistryCost, Is.EqualTo(107.41m));
			//Торговая наценка организации-импортера
			//Цена комиссионера с НДС
			//Комиссионное вознаграждение без НДС
			//НДС с комиссионного вознаграждения
			//Отпускная цена ЛБО
			Assert.That(doc.Lines[7].Amount, Is.EqualTo(380.82m));
			Assert.That(doc.Lines[7].ProducerCost, Is.EqualTo(118.15m));
			Assert.That(doc.Lines[7].VitallyImportant, Is.EqualTo(true));
			Assert.That(doc.Lines[7].Nds, Is.EqualTo(10.00m));

		}
	}
}