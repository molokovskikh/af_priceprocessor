using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class PulsBrianskParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNO")
				.DocumentHeader(h => h.DocumentDate, "DOCDAT")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOOD")
				.Line(l => l.SerialNumber, "SERIAL")
				.Line(l => l.Period, "DATEB")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.CertificatesDate, "DATES")
				.Line(l => l.CertificateAuthority, "SERTWHO")
				.Line(l => l.SupplierPriceMarkup, "MARGIN")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Producer, "ENTERP")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEWONDS")
				.Line(l => l.ProducerCostWithoutNDS, "PRICEENT")
				.Line(l => l.Amount, "SUMSNDS")
				.Line(l => l.NdsAmount, "SUMMNDS")
				.Line(l => l.VitallyImportant, "PV")
				.Line(l => l.OrderId, "orderID")
				.Line(l => l.EAN13, "BARCODE")
					.Invoice(i => i.AmountWithoutNDS, "SUMM")
					.Invoice(i => i.RecipientId, "customerCD")
					.Invoice(i => i.RecipientAddress, "customerNM");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DOCNO")
				&& data.Columns.Contains("DOCDAT")
				&& data.Columns.Contains("QUANT")
				&& data.Columns.Contains("orderID")
				&& data.Columns.Contains("PRICEWONDS");
		}
	}
}
