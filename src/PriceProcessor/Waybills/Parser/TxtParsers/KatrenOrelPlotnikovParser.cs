using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	/// <summary>
	/// К задаче
	/// http://redmine.analit.net/issues/28339
	/// </summary>
	public class KatrenOrelPlotnikovParser : BaseIndexingParser
	{
		private bool SkippedFirstLine = false;

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headerCaption = reader.ReadLine();
				if (headerCaption == null)
					return false;
				if (!headerCaption.Equals("[header]", StringComparison.InvariantCultureIgnoreCase))
					return false;
				var headerLine = reader.ReadLine();
				if (headerLine == null)
					return false;

				var bodyCaption = reader.ReadLine();
				if (bodyCaption == null)
					return false;
				if (!bodyCaption.Equals("[body]", StringComparison.InvariantCultureIgnoreCase))
					return false;

				var bodyLine = reader.ReadLine();
				if (bodyLine == null)
					return false;

				var body = bodyLine.Split(';');

				var CodeIndex = Array.FindIndex(body, p => p.Equals("Код_товара", StringComparison.CurrentCultureIgnoreCase));
				var ProductIndex = Array.FindIndex(body, p => p.Equals("Наименование", StringComparison.CurrentCultureIgnoreCase));
				var SupplierCostIndex = Array.FindIndex(body, p => p.Equals("Цена с НДС", StringComparison.CurrentCultureIgnoreCase));
				var SupplierCostWithoutNdsIndex = Array.FindIndex(body, p => p.Equals("Цена без НДС", StringComparison.CurrentCultureIgnoreCase));
				var NdsIndex = Array.FindIndex(body, p => p.Equals("Ставка НДС", StringComparison.CurrentCultureIgnoreCase));
				
				if (CodeIndex == -1 || ProductIndex == -1)
					return false;

				if (SupplierCostIndex != -1 && SupplierCostWithoutNdsIndex != -1)
					return true;
				if (SupplierCostIndex != -1 && NdsIndex != -1)
					return true;
				if (SupplierCostWithoutNdsIndex != -1 && NdsIndex != -1)
					return true;
			}
			return false;
		}

		protected override void SetIndexes()
		{
			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			ProducerCostWithoutNdsIndex = 5;
			SupplierCostIndex = 6;
			SupplierCostWithoutNdsIndex = 7;
			NdsIndex = 8;
			BillOfEntryNumberIndex = 13;
			CertificatesIndex = 14;
			CertificateAuthorityIndex = 15;
			SerialNumberIndex = 11;
			PeriodIndex = 12;
			RegistryCostIndex = 18;
			VitallyImportantIndex = 20;
		}

		protected void MapIndexesFromLine(string line)
		{
			var parts = line.Split(';');
			CodeIndex = Array.FindIndex(parts, p => p.Equals("Код_товара", StringComparison.CurrentCultureIgnoreCase));
			ProductIndex = Array.FindIndex(parts, p => p.Equals("Наименование", StringComparison.CurrentCultureIgnoreCase));
			ProducerIndex = Array.FindIndex(parts, p => p.Equals("Производитель", StringComparison.CurrentCultureIgnoreCase));
			CountryIndex = Array.FindIndex(parts, p => p.Equals("Страна", StringComparison.CurrentCultureIgnoreCase));
			QuantityIndex = Array.FindIndex(parts, p => p.Equals("Кол-Во", StringComparison.CurrentCultureIgnoreCase));
			ProducerCostWithoutNdsIndex = Array.FindIndex(parts, p => p.Equals("ЦЗИ", StringComparison.CurrentCultureIgnoreCase));
			SupplierCostIndex = Array.FindIndex(parts, p => p.Equals("Цена с НДС", StringComparison.CurrentCultureIgnoreCase));
			SupplierCostWithoutNdsIndex = Array.FindIndex(parts, p => p.Equals("Цена без НДС", StringComparison.CurrentCultureIgnoreCase));
			NdsIndex = Array.FindIndex(parts, p => p.Equals("Ставка НДС", StringComparison.CurrentCultureIgnoreCase));
			BillOfEntryNumberIndex = Array.FindIndex(parts, p => p.Equals("ГТД", StringComparison.CurrentCultureIgnoreCase));
			CertificatesIndex = Array.FindIndex(parts, p => p.Equals("Сертификат", StringComparison.CurrentCultureIgnoreCase));
			CertificateAuthorityIndex = Array.FindIndex(parts, p => p.Equals("Орган выдавший сертификат", StringComparison.CurrentCultureIgnoreCase));
			SerialNumberIndex = Array.FindIndex(parts, p => p.Equals("Серия", StringComparison.CurrentCultureIgnoreCase));
			PeriodIndex = Array.FindIndex(parts, p => p.Equals("Срок годности", StringComparison.CurrentCultureIgnoreCase));
			RegistryCostIndex = Array.FindIndex(parts, p => p.Equals("Цена реестра", StringComparison.CurrentCultureIgnoreCase));
			VitallyImportantIndex = Array.FindIndex(parts, p => p.Equals("Признак ЖВ", StringComparison.CurrentCultureIgnoreCase)); 
		}

		protected override void ReadBody(Document document, string line)
		{
			if (!SkippedFirstLine) {
				SkippedFirstLine = true;
				MapIndexesFromLine(line);
				return;
			}

			base.ReadBody(document, line);
		}
	}
}