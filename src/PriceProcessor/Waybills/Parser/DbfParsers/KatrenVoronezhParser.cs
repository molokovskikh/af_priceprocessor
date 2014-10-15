using System;
using System.Data;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenVoronezhParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "DocNum")
				.DocumentHeader(d => d.DocumentDate, "QueryDate")
				.Invoice(i => i.InvoiceNumber, "DocNum")
				.Invoice(i => i.InvoiceDate, "DocDate")
				.Invoice(i => i.SellerName, "SellerName")
				.Invoice(i => i.SellerAddress, "SAddress")
				.Invoice(i => i.SellerINN, "SINN")
				.Invoice(i => i.SellerKPP, "SINN")
				.Invoice(i => i.ShipperInfo, "GAddress")
				.Invoice(i => i.RecipientAddress, "CAddress")
				.Invoice(i => i.PaymentDocumentInfo, "CAccount")
				.Invoice(i => i.BuyerName, "Contractor")
				.Invoice(i => i.BuyerAddress, "StAddress")
				.Invoice(i => i.BuyerINN, "CINN")
				.Invoice(i => i.BuyerKPP, "CINN")
				.Invoice(i => i.AmountWithoutNDS0, "STWONDS0")
				.Invoice(i => i.AmountWithoutNDS10, "STWONDS10")
				.Invoice(i => i.NDSAmount10, "SUMNDS10")
				.Invoice(i => i.Amount10, "SUMTOTAL10")
				.Invoice(i => i.AmountWithoutNDS18, "STWONDS18")
				.Invoice(i => i.NDSAmount18, "SUMNDS18")
				.Invoice(i => i.Amount18, "SUMTOTAL18")
				.Invoice(i => i.AmountWithoutNDS, "STOTWONDS")
				.Invoice(i => i.NDSAmount, "SUMNDS")
				.Invoice(i => i.Amount, "SUMTOTAL")
				.Line(l => l.Product, "GOODE")
				.Line(l => l.Unit, "IZM")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Producer, "PRODUSER")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2")
				.Line(l => l.ExciseTax, "AKZ")
				.Line(l => l.SupplierPriceMarkup, "MARGIN")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.ProducerCost, "PPRICENDS")
				.Line(l => l.ProducerCostWithoutNDS, "PPRICEWT")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "NDSSUM")
				.Line(l => l.Amount, "SPRICENDS")
				.Line(l => l.SerialNumber, "SERIAL")
				.Line(l => l.Period, "DATEB")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.CountryCode, "CNTRCODE")
				.Line(l => l.UnitCode, "UNITCODE")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.CertificatesDate, "D_SERTIF")
				.Line(l => l.VitallyImportant, "GV")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.OrderId, "QueryNum");
		}

		public override void PostParsing(Document doc)
		{
			if (doc.Invoice != null) {
				var innkpp = doc.Invoice.SellerINN.Split('/');
				if (innkpp.Length == 2) {
					doc.Invoice.SellerINN = String.IsNullOrEmpty(innkpp[0]) ? null : innkpp[0];
					doc.Invoice.SellerKPP = String.IsNullOrEmpty(innkpp[1]) ? null : innkpp[1];
				}
				innkpp = doc.Invoice.BuyerINN.Split('/');
				if (innkpp.Length == 2) {
					doc.Invoice.BuyerINN = String.IsNullOrEmpty(innkpp[0]) ? null : innkpp[0];
					doc.Invoice.BuyerKPP = String.IsNullOrEmpty(innkpp[1]) ? null : innkpp[1];
				}
			}
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DocNum")
				&& data.Columns.Contains("DocDate")
				&& data.Columns.Contains("QueryNum")
				&& data.Columns.Contains("QueryDate")
				&& data.Columns.Contains("SellerName")
				&& data.Columns.Contains("SAddress")
				&& data.Columns.Contains("SINN")
				&& data.Columns.Contains("GAddress")
				&& data.Columns.Contains("GOODE");
		}
	}
}