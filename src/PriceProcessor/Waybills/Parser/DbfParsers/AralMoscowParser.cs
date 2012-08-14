using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AralMoscowParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATEM")
				.DocumentHeader(d => d.ProviderDocumentId, "NOM")
				.Line(l => l.Code, "STOREID")
				.Line(l => l.Product, "GOODNAME")
				.Line(l => l.Producer, "BUILDNAME")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCost, "FULLPRICE")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.ProducerCostWithoutNDS, "BUILDERPRI")
				.Line(l => l.Period, "BESTBEFORE")
				.Line(l => l.Certificates, "NUMSERTIF")
				.Line(l => l.CertificatesDate, "DTASERTIF")
				.Line(l => l.Nds, "NDSRATE")
				.Line(l => l.RegistryCost, "GOSREG")
				.Line(l => l.VitallyImportant, "GVLS")
				.Line(l => l.SerialNumber, "SERIE");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("STOREID")
				&& data.Columns.Contains("GOODNAME")
				&& data.Columns.Contains("BUILDNAME")
				&& data.Columns.Contains("FULLPRICE")
				&& data.Columns.Contains("BUILDERPRI");
		}
	}
}