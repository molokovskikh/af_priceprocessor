using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class PulsFK3996Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DocName")
				.DocumentHeader(h => h.DocumentDate, "DateDoc")
				.Invoice(i => i.ShipperInfo, "Vendor")
				.Line(l => l.Code, "Code")
				.Line(l => l.Product, "Good")
				.Line(l => l.SerialNumber, "Serial")
				.Line(l => l.Period, "DateB")
				.Line(l => l.SupplierCost, "Price")
				.Line(l => l.Quantity, "Quant")
				.Line(l => l.Certificates, "Sert")
				.Line(l => l.CertificateAuthority, "SertWho")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "Reestr")
				.Line(l => l.Producer, "Enterp")
				.Line(l => l.Country, "Country")
				.Line(l => l.SupplierCostWithoutNDS, "PriceWONDS")
				.Line(l => l.ProducerCostWithoutNDS, "Priceent")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.VitallyImportant, "ZNVLS")
				.Line(l => l.ProducerCost, "PRDWNDS")
				.Line(l => l.EAN13, "ProdSBar")
				.Line(l => l.Amount, "SUMSNDS")
				.Line(l => l.NdsAmount, "SUMNDS");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("Vendor")
				&& data.Columns.Contains("Enterp")
				&& data.Columns.Contains("Good")
				&& data.Columns.Contains("PriceWONDS")
				&& data.Columns.Contains("ZNVLS");
		}
	}
}
