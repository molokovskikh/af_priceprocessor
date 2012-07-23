using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class LekRusChernozemieSpecialParser : IDocumentParser
	{
		public static DataTable Load(string file)
		{
			try
			{
				return Dbf.Load(file);
			}
			catch (DbfException)
			{
				return Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			}
		}

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_DOC")
				.DocumentHeader(d => d.DocumentDate, "DATE_DOC")

				.DocumentInvoice(i => i.InvoiceNumber, "NUM_SF")
				.DocumentInvoice(i => i.InvoiceDate, "DATE_SF")
				.DocumentInvoice(i => i.RecipientAddress, "ADRESS_G")
				.DocumentInvoice(i => i.ShipperInfo, "ADRESS_P")
				.DocumentInvoice(i => i.AmountWithoutNDS0, "SUMBNDS0")
				.DocumentInvoice(i => i.AmountWithoutNDS10, "SUMBNDS10")
				.DocumentInvoice(i => i.AmountWithoutNDS18, "SUMBNDS18")
				.DocumentInvoice(i => i.NDSAmount10, "SUMNDS10")
				.DocumentInvoice(i => i.NDSAmount18, "SUMNDS18")
				.DocumentInvoice(i => i.Amount10, "SUMSNDS10")
				.DocumentInvoice(i => i.Amount18, "SUMSNDS18")
				.DocumentInvoice(i => i.AmountWithoutNDS, "SUMBNDS")
				.DocumentInvoice(i => i.Amount, "SUMALL")
				.DocumentInvoice(i => i.NDSAmount, "SUMNDS")

				.Line(l => l.Code, "CODE_TOVAR")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.Producer, "PROIZ")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Quantity, "VOLUME")
				.Line(l => l.ProducerCostWithoutNDS, "PR_PROIZ")
				.Line(l => l.Nds, "PCT_NDS")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.SupplierCost, "PRICE_NDS")
				.Line(l => l.RegistryCost, "PRICE_RR")
				.Line(l => l.NdsAmount, "SUMMA_NDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "CER_NUMBER")
				.Line(l => l.VitallyImportant, "JNVLS")
				.Line(l => l.EAN13, "EAN13")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODE_TOVAR")
				&& data.Columns.Contains("NAME_TOVAR")
				&& data.Columns.Contains("PROIZ")
				&& data.Columns.Contains("COUNTRY")
				&& data.Columns.Contains("VOLUME")
				&& data.Columns.Contains("PR_PROIZ")
				&& data.Columns.Contains("PCT_NDS")
				&& data.Columns.Contains("PRICE")
				&& data.Columns.Contains("PRICE_NDS")
				&& data.Columns.Contains("JNVLS")
				&& data.Columns.Contains("DATE_SF")
				&& data.Columns.Contains("NUM_SF")
				&& data.Columns.Contains("SUMALL")
				&& data.Columns.Contains("SUMNDS");
		}
	}
}
