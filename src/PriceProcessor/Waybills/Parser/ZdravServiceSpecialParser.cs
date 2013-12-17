using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class ZdravServiceSpecialParser : IDocumentParser
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

		public Document Parse(string file, Document document)
		{
			var data = Load(file);
			new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.Invoice(i => i.InvoiceNumber, "BILLNUM")
				.Invoice(i => i.InvoiceDate, "BILLDT")
				.Invoice(i => i.SellerName, "PROVIDER")
				.Invoice(i => i.SellerAddress, "PADDR")
				.Invoice(i => i.SellerINN, "PINNKPP")
				.Invoice(i => i.SellerKPP, "PINNKPP")
				.Invoice(i => i.ShipperInfo, "CONSIGNOR")
				.Invoice(i => i.RecipientAddress, "CONSIGNEE")
				.Invoice(i => i.PaymentDocumentInfo, "NPAYDOC")
				.Invoice(i => i.BuyerName, "PAYER")
				.Invoice(i => i.BuyerAddress, "PAYERADDR")
				.Invoice(i => i.BuyerINN, "PAYERINNKPP")
				.Invoice(i => i.BuyerKPP, "PAYERINNKPP")
				.Invoice(i => i.AmountWithoutNDS0, "SUM0")
				.Invoice(i => i.AmountWithoutNDS10, "SUM10")
				.Invoice(i => i.NDSAmount10, "NDS10")
				.Invoice(i => i.Amount10, "SUMNDS10")
				.Invoice(i => i.AmountWithoutNDS18, "SUM18")
				.Invoice(i => i.NDSAmount18, "NDS18")
				.Invoice(i => i.Amount18, "SUMNDS18")
				.Invoice(i => i.AmountWithoutNDS, "SUMPAYNDS")
				.Invoice(i => i.NDSAmount, "SUMNDS")
				.Invoice(i => i.Amount, "SUMPAY")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Unit, "UNITS")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.SupplierCost, "PRICE2")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.SupplierPriceMarkup, "PROCNDB")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE1N")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "SUMNDS2")
				.Line(l => l.Amount, "SUM1")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.EAN13, "EAN13")
				.ToDocument(document, data);

			if (document.Invoice != null) {
				if (document.Invoice.SellerINN != null) {
					var innkpp = document.Invoice.SellerINN.Split('/');
					if (innkpp.Length == 2) {
						document.Invoice.SellerINN = innkpp[0];
						document.Invoice.SellerKPP = innkpp[1];
					}
				}
				if (document.Invoice.BuyerINN != null) {
					var innkpp = document.Invoice.BuyerINN.Split('/');
					if (innkpp.Length == 2) {
						document.Invoice.BuyerINN = innkpp[0];
						document.Invoice.BuyerKPP = innkpp[1];
					}
				}
			}
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NDOC")
				&& data.Columns.Contains("DATEDOC")
				&& data.Columns.Contains("PROVIDER")
				&& data.Columns.Contains("PADDR")
				&& data.Columns.Contains("PINNKPP")
				&& data.Columns.Contains("CONSIGNOR")
				&& data.Columns.Contains("CONSIGNEE")
				&& data.Columns.Contains("NPAYDOC")
				&& data.Columns.Contains("PAYER")
				&& data.Columns.Contains("CODEPST")
				&& data.Columns.Contains("EAN13")
				&& data.Columns.Contains("QNT")
				&& data.Columns.Contains("UNITS")
				&& data.Columns.Contains("PRICE2N")
				&& data.Columns.Contains("BILLNUM")
				&& data.Columns.Contains("BILLDT");
		}
	}
}