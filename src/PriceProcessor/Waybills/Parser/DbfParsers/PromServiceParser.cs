﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class PromServiceParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Period, "DATEGODN")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.ProducerCost, "PRPROIZV")
				.Line(l => l.Amount, "ITOG")
				.Line(l => l.NdsAmount, "SUM_NDS")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.SerialNumber, "SERIA", "SERIYA")
				.Line(l => l.Nds, "STNDS");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("SUM_B_NDS") &&
				data.Columns.Contains("ITOG") &&
				data.Columns.Contains("PRPROIZV") &&
				data.Columns.Contains("PROIZV") &&
				data.Columns.Contains("KOD") &&
				data.Columns.Contains("STNDS");
		}
	}
}