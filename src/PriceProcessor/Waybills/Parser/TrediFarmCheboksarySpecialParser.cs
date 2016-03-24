using System.Data;
using Common.Tools;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;


namespace Inforoom.PriceProcessor.Waybills.Parser
{
	// Парсер для Тредифарм Чебоксары (требование 3647). Формат похож на SiaKazanParser.
	public class TrediFarmCheboksarySpecialParser : IDocumentParser
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
			var data = Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_NAKL")
				.DocumentHeader(d => d.DocumentDate, "DATA_NAKL")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.ProducerCostWithoutNDS, "CENAPROIZ")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.SupplierPriceMarkup, "NADBPROC")
				.Line(l => l.SupplierCostWithoutNDS, "CENABNDS")
				.Line(l => l.Nds, "NDSPOSTAV")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.NdsAmount, "SUMMANDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.SerialNumber, "SERII")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesEndDate, "SERTDATE")
				.Line(l => l.CertificatesDate, "SERTGIVE")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.Period, "SROK_GODN", "DATAEND")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KOD")
				&& data.Columns.Contains("NAME")
				&& data.Columns.Contains("PROIZV")
				&& data.Columns.Contains("KOLVO")
				&& data.Columns.Contains("CENAPROIZ")
				&& data.Columns.Contains("SERII")
				&& data.Columns.Contains("REESTR")
				&& (data.Columns.Contains("SROK_GODN") || data.Columns.Contains("DATAEND"));
		}
	}
}