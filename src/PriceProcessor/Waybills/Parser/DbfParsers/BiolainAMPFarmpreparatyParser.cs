using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BiolainAMPFarmpreparatyParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			/*
			Поле Имя поля Число знаков в дробной части 
			Номер строки NUM 2 +
			Номенклатура CODE +
			Название номенклатуры GOOD +
			Номер партии SERIAL +
			Срок годности DATEB +
			Цена с НДС PRICE 2 +
			Отобрано QUANT 2 +
			Регистрационный номер сертификата SERT +
			Дата DATES + - дата выдачи сертификата
			Орган, выдавший сертификат SERTWHO -
			Ставка НДС NDS 2 +
			Зарегистрированная цена REESTR 2 +
			Название производителя ENTERP +
			Описание COUNTRY +
			Цена без НДС PRICEWONDS 2 +
			Жизненно-важное PV +
			*/

			return new DbfParser()
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOOD")
				.Line(l => l.SerialNumber, "SERIAL")
				.Line(l => l.Period, "DATEB")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.CertificatesDate, "DATES")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Producer, "ENTERP")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEWONDS")
				.Line(l => l.VitallyImportant, "PV");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var columns = data.Columns;
			return columns.Contains("CODE")
				&& columns.Contains("GOOD")
				&& columns.Contains("DATEB")
				&& columns.Contains("QUANT")
				&& columns.Contains("REESTR")
				&& columns.Contains("PRICEWONDS")
				&& columns.Contains("PV")
				&& columns.Contains("NACENKAROZ")
				&& columns.Contains("PRICEROZ");
		}
	}
}
