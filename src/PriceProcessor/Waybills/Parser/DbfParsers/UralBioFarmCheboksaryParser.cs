using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class UralBioFarmCheboksaryParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NSF")
				.DocumentHeader(h => h.DocumentDate, "DTOTG")
				.Invoice(i => i.RecipientAddress, "ADRPOTR")
				.Line(l => l.Code, "NFS")
				.Line(l => l.Product, "NMFS")
				.Line(l => l.Producer, "ZIZG")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.ProducerCostWithoutNDS, "ZNIZG")
				.Line(l => l.ProducerCost, "ZNIZG_S_N")
				.Line(l => l.SupplierCostWithoutNDS, "ZNPROD")
				.Line(l => l.SupplierCost, "ZNPROD_S_N")
				.Line(l => l.RegistryCost, "ZNIZG_F")
				.Line(l => l.Amount, "SUMNDS")
				.Line(l => l.NdsAmount, "NDSSUM")
				.Line(l => l.Quantity, "KOLF")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "DATA_SERT")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.BillOfEntryNumber, "OKDP")
				.Line(l => l.EAN13, "EAN")
				.Line(l => l.VitallyImportant, "JNVLS")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NSF") &&
				data.Columns.Contains("ADRPOTR") &&
				data.Columns.Contains("ZNPROD_S_N") &&
				data.Columns.Contains("STRANA") &&
				data.Columns.Contains("JNVLS") &&
				data.Columns.Contains("SERTIF");
		}
	}
}