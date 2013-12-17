using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class RostaOmskParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding.GetEncoding(1251), false, false);
			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNUM")
				.DocumentHeader(h => h.DocumentDate, "DOCDATE")
				.Line(l => l.Code, "ID")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.SupplierCostWithoutNDS, "PRICENONDS")
				.Line(l => l.Nds, "NDSPROC")
				.Line(l => l.NdsAmount, "NDSSUM")
				.Line(l => l.Amount, "SUMWNDS")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.Producer, "PRO")
				.Line(l => l.ProducerCostWithoutNDS, "PRONONDS")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.EAN13, "BARCODE")
				.Invoice(i => i.ShipperInfo, "VENDOR")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PRICENONDS")
				&& data.Columns.Contains("NDSPROC")
				&& data.Columns.Contains("PRONONDS");
		}
	}
}
