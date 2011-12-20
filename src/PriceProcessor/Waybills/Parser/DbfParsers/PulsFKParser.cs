using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	//не указывают кодировку по этому приходится задавать явно
	public class PulsFKParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
		    return new DbfParser()
		        .DocumentHeader(d => d.DocumentDate, "DATEDOC")
		        .DocumentHeader(d => d.ProviderDocumentId, "NDOC")
		        .Line(l => l.Code, "CODEPST")
		        .Line(l => l.Product, "NAME")
		        .Line(l => l.Producer, "FIRM")
		        .Line(l => l.Country, "CNTR")
		        .Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
		        .Line(l => l.SupplierCost, "PRICE2")
		        .Line(l => l.Quantity, "QNT")
				.Line(l => l.ProducerCostWithoutNDS, "MAKERPRICE", "PRICE1")
		        .Line(l => l.Nds, "NDS")
		        .Line(l => l.Period, "GDATE")
		        .Line(l => l.Certificates, "SERTIF")
		        .Line(l => l.RegistryCost, "REGPRC")
		        .Line(l => l.VitallyImportant, "GNVLS")
		        .Line(l => l.SerialNumber, "SER")
		        .Line(l => l.Amount, "SUMPAY")
		        .Line(l => l.SupplierPriceMarkup, "PROCNDB")
		        .Line(l => l.EAN13, "EAN13")
		        .Line(l => l.BillOfEntryNumber, "NUMGTD");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NDOC")
			       && data.Columns.Contains("CNTR")
			       && data.Columns.Contains("SERTIF")
			       && data.Columns.Contains("GDATE")
			       //&& data.Columns.Contains("MAKERPRICE")
			       && data.Columns.Contains("PRICE2");
		}
	}
}
