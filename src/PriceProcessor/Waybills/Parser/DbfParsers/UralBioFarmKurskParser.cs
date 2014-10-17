using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class UralBioFarmKurskParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NSF")
				.DocumentHeader(h => h.DocumentDate, "DOTG")
				.Invoice(i => i.InvoiceNumber, "NSF")
				.Invoice(i => i.InvoiceDate, "DOTG")
				.Invoice(i => i.BuyerName, "NMPOST")
				.Invoice(i => i.BuyerAddress, "ADRPOST")
				.Line(l => l.Code, "NFS")
				.Line(l => l.Product, "NMFS")
				.Line(l => l.Producer, "ZIZG")
				.Line(l => l.ProducerCostWithoutNDS, "ZNIZG")
				.Line(l => l.ProducerCost, "ZNIZG_S_N")
				.Line(l => l.SupplierCostWithoutNDS, "ZNPROD")
				.Line(l => l.SupplierCost, "ZNPROD_S_N")
				.Line(l => l.RegistryCost, "GR_CENA")
				.Line(l => l.Amount, "SUMNDS")
				.Line(l => l.NdsAmount, "NDSSUM")
				.Line(l => l.Quantity, "KOLF")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.BillOfEntryNumber, "OKDP")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "PV")
				.Line(l => l.OrderId, "NUMZAK")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NSF") &&
				data.Columns.Contains("DOTG") &&
				data.Columns.Contains("ZNPROD_S_N") &&
				data.Columns.Contains("ZNIZG") &&
				data.Columns.Contains("NDSSUM") &&
				data.Columns.Contains("SERTIF");
		}
	}
}