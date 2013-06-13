using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	// Отдельный парсер для челябинского Морона (код 338)
	// (вообще-то формат тот же что и у SiaParser, но в колонке PRICE цена БЕЗ Ндс)
	public class Moron_338_SpecialParser : BaseDbfParser
	{
		public static DataTable Load(string file)
		{
			try {
				return Dbf.Load(file);
			}
			catch (DbfException) {
				return Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			}
		}

		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_DOC")
				.DocumentHeader(d => d.DocumentDate, "DATE_DOC")
				.Line(l => l.Code, "CODE_TOVAR")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.Producer, "PROIZ")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.ProducerCostWithoutNDS, "PR_PROIZ")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.SupplierPriceMarkup, "NACENKA")
				.Line(l => l.Quantity, "VOLUME")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "DOCUMENT", "CER_NUMBER")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Nds, "PCT_NDS")
				.Line(l => l.VitallyImportant, "ZHNVLS", "ISZHVP", "ISZNVP", "JNVLS", "GZWL", "Priznak_pr", "VITAL", "GVLS", "GV")
				.Line(l => l.RegistryCost, "cach_reest", "REESTR", "OTHER", "PR_REG", "PRICE_RR", "REESTRPRIC");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODE_TOVAR") &&
				data.Columns.Contains("NAME_TOVAR") &&
				data.Columns.Contains("PROIZ") &&
				data.Columns.Contains("COUNTRY") &&
				data.Columns.Contains("PR_PROIZ") &&
				data.Columns.Contains("PCT_NDS") &&
				data.Columns.Contains("VOLUME");
		}
	}
}