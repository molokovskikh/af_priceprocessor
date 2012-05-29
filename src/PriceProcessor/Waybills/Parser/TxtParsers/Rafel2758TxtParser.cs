using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class Rafel2758TxtParser : BaseIndexingParser
	{
		/*
			[Header]
			0 <НомерДокумента>;+
			1 <ДатаДокумента>;+
			2 <Пусто>;
			3 <Фирма>;+
			4 <Пусто>;
			[Body]
			0 <ТоварНомер>;+
			1 <ТоварНазвание>;+
			2 <ПроизводительНазвание>;+
			3 <ПроизводительСтрана>;+
			4 <Количество>;+
			5 <Пусто>;
			6 <ЦенаПоставщикаБезНДС>;+
			7 <НДС%>;+
			8 <СуммаНДССтроки>;+
			9 <СерияНомер>;+
			10 <ГоденДо>;+
			11 <Пусто>;
			12 <Пусто>;
			13 <Пусто>;
			14 <Пусто>;
			15 <Пусто>;
			16 <Пусто>;
			17 <Пусто>;
			18 <Пусто>;
			19 <Пусто>;
			20 <Пусто>;
			21 <Пусто>;
			22 <№Разрешения>;+
			23 <ЦЕНАПРОИЗВОДИТЕЛЯСНДС>;
			24 <ЦЕНАПРОИЗВОДИТЕЛЯБЕЗНДС>;+
			25 <ЦенаГосРеестра>;+
			26 <ЛекЖВ>;+
		*/
		protected override void SetIndexes()
		{
			ProviderDocumentIdIndex = 0;
			DocumentDateIndex = 1;
			BuyerNameIndex = 3;
			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			SupplierCostWithoutNdsIndex = 6;
			NdsIndex = 7;
			NdsAmountIndex = 8;
			SerialNumberIndex = 9;
			PeriodIndex = 10;
			CertificatesIndex = 22;
			ProducerCostWithoutNdsIndex = 24;
			RegistryCostIndex = 25;
			VitallyImportantIndex = 26;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length != 6)
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (body.Length != 28)
					return false;
			}
			return true;
		}
	}
}
