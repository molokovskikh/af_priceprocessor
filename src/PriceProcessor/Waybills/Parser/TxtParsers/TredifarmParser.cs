﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class TredifarmParser : BaseIndexingParser, IDocumentParser
	{
		public new Document Parse(string file, Document document)
		{
			base.Parse(file, document);
			return document;
		}

		protected override void SetIndexes()
		{
			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			ProducerCostIndex = 5;
			SupplierCostWithoutNdsIndex = 6;
			SupplierCostIndex = 7;
			NdsIndex = 8;
			SupplierPriceMarkupIndex = 9;
			SerialNumberIndex = 10;
			PeriodIndex = 11;
			CertificatesIndex = 12;
			RegistryCostIndex = -1;
			VitallyImportantIndex = -1;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length != 7)
					return false;
				if (!header[3].ToLower().Equals("ооо \"трэдифарм\""))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (GetDecimal(body[6]) == null)
					return false;
			}
			return true;
		}
	}
}
