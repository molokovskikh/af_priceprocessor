using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class ImperiaFarmaSpecialParser : IDocumentParser 
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
			var data = Load(file);
			new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "D_NAKL")
				.DocumentHeader(d => d.ProviderDocumentId, "N_NAKL")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.ProducerCost, "CENAPROIZ")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.SupplierPriceMarkup, "NABDPROC")
				.Line(l => l.SupplierCostWithoutNDS, "CENABNDS")
				.Line(l => l.Nds, "NDSPOSTAV")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.NdsAmount, "SUMMANDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.SerialNumber, "SERII")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.Period, "SROK")
				.Line(l => l.VitallyImportant, "ISLIFE")
				.ToDocument(document, data);			
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KOD")
				&& data.Columns.Contains("NAME")
				&& data.Columns.Contains("PROIZV")
				&& data.Columns.Contains("COUNTRY")
				&& data.Columns.Contains("SERTIF")
				&& data.Columns.Contains("CENABNDS")
				&& data.Columns.Contains("CENASNDS")
				&& data.Columns.Contains("SROK")
				&& data.Columns.Contains("PROIZV")
				&& data.Columns.Contains("ISLIFE");
		}
	}
}
