using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AstrahanFarm12729Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NAKL")
				.DocumentHeader(h => h.DocumentDate, "DATENAKL")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Producer, "ZAVOD")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "GODEN_DO")
				.Line(l => l.SupplierCostWithoutNDS, "CENA_B_N")
				.Line(l => l.SupplierCost, "CENA_S_N")
				.Line(l => l.Amount, "SUM_S_N")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "SUM_NDS")
				.Line(l => l.ProducerCostWithoutNDS, "CENA_IZG")
				.Line(l => l.RegistryCost, "CENA_REE")
				.Line(l => l.VitallyImportant, "JNV");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CENA_B_N") &&
				data.Columns.Contains("JNV") &&
				data.Columns.Contains("ZAVOD");
		}
	}
}
