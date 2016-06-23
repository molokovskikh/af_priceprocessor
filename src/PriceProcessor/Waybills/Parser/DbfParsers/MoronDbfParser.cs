using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MoronDbfParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, null, true, false);
			//var data = Dbf.Load(file, Encoding.GetEncoding(866), false, false);
			new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUMNAK")
				.DocumentHeader(d => d.DocumentDate, "DATAGOT")
				.Line(l => l.Code, "KODNLKLEK")
				.Line(l => l.Product, "NAMLEK")
				.Line(l => l.Producer, "NAMZAVOD")
				.Line(l => l.Country, "NAMSTRANA")
				.Line(l => l.Period, "SROKGOD")
				.Line(l => l.SerialNumber, "SERIJ")
				.Line(l => l.Quantity, "COUNT")
				.Line(l => l.SupplierCost, "CENAPROD")
				.Line(l => l.ProducerCostWithoutNDS, "CENARAS")
				.Line(l => l.Nds, "PRCNDS")
				.Line(l => l.VitallyImportant, "OBAS")
				.Line(l => l.RegistryCost, "CENAREE")
				.Line(l => l.Certificates, "NUMBER")
				.Line(l => l.SupplierCostWithoutNDS, "CENAPRBNDS")
				.Line(l => l.EAN13, "ZSHK")
				.Line(l => l.CertificateAuthority, "NAMEPRINT")
				.Line(l => l.CertificatesEndDate, "SROK")
				.Line(l => l.BillOfEntryNumber, "NUMDECLARE")
				.Line(l => l.Amount, "SUMPROD")
				.Line(l => l.Unit, "VID")
				.Invoice(i => i.SellerName, "SENDER")
				//.Invoice(i => i.BuyerId, "KODAPTEK") // Код точки доставки, предоставляемый покупателем
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUMNAK") &&
				data.Columns.Contains("DATAGOT") &&
				data.Columns.Contains("KODAPTEK") &&
				//data.Columns.Contains("KODPOSTAV") && // no
				data.Columns.Contains("CENAPROD") &&
				data.Columns.Contains("PRCNDS");
		}
	}
}


