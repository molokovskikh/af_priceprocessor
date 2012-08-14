using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class MarimedsnabSpecialParser : IDocumentParser
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
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "SUMNDS10")
				.Line(l => l.Amount, "SUMPAY")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Certificates, "SER")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.OrderId, "NUMZ")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE1")
				.Line(l => l.Country, "CNTRMADE")
				.Line(l => l.SupplierPriceMarkup, "PRCOPT")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "REGPRC").ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DATEDOC")
				&& data.Columns.Contains("NDOC")
				&& data.Columns.Contains("NAME")
				&& data.Columns.Contains("FIRM")
				&& data.Columns.Contains("QNT")
				&& data.Columns.Contains("CODEPST")
				&& data.Columns.Contains("SUMPAY");
		}
	}
}