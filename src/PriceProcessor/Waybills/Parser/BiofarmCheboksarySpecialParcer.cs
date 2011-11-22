using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class BiofarmCheboksarySpecialParcer : IDocumentParser
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
				.DocumentHeader(h => h.ProviderDocumentId, "N_NACL", "N_NAKL")
				.DocumentHeader(h => h.DocumentDate, "D_NACL", "D_NAKL")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.EAN13, "SCANCOD")
				.Line(l => l.Producer, "FACTORY")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_MAKE")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_NAKE")
				.Line(l => l.Nds, "NDS_PR")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.RegistryCost, "PRICE_REES")
				.Line(l => l.VitallyImportant, "ISLIFE")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Period, "DATE_VALID")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Certificates, "SERT")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return (data.Columns.Contains("N_NACL") || data.Columns.Contains("N_NAKL")) &&
				(data.Columns.Contains("D_NACL") || data.Columns.Contains("D_NAKL")) &&
				data.Columns.Contains("CODE") &&
				data.Columns.Contains("NAME") &&
				data.Columns.Contains("FACTORY") &&
				data.Columns.Contains("PRICE_MAKE") &&
				data.Columns.Contains("PRICE_NAKE") &&
				data.Columns.Contains("NDS_PR") &&
				data.Columns.Contains("ISLIFE") &&
				data.Columns.Contains("DATE_VALID");
		}
	}
}
