using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AptekaHoldingIzhevskParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			string vitallyImportantColumn = null;
			string certificatesColumn = null;
			string registryCostColumn = null;

			var data = Dbf.Load(file, Encoding);
			if (data.Columns.Contains("ZHVL"))
				vitallyImportantColumn = "ZHVL";

			if (data.Columns.Contains("CENAR"))
				registryCostColumn = "CENAR";

			if (data.Columns.Contains("SERT"))
				certificatesColumn = "SERT";

			document.Lines = data.Rows.Cast<DataRow>().Select(r =>
			{
				document.ProviderDocumentId = r["NTTN"].ToString();
				if (!Convert.IsDBNull(r["DTTN"]))
					document.DocumentDate = Convert.ToDateTime(r["DTTN"]);
				var line = document.NewLine();
				line.Code = r["KOD"].ToString();
				line.Product = r["TOVAR"].ToString();
				line.Producer = r["IZGOT"].ToString();
				line.Country = r["STRANA"].ToString();
				line.ProducerCost = Convert.IsDBNull(r["CENAIZG"]) ? null : (decimal?)Convert.ToDecimal(r["CENAIZG"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["CENAOPT"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["KOL"]);
				line.Period = Convert.IsDBNull(r["SRGOD"]) ? null : Convert.ToDateTime(r["SRGOD"]).ToShortDateString();

				if (!String.IsNullOrEmpty(registryCostColumn))
					line.RegistryCost = Convert.IsDBNull(r[registryCostColumn]) ? null : (decimal?)Convert.ToDecimal(r[registryCostColumn], CultureInfo.InvariantCulture);

				if (!String.IsNullOrEmpty(certificatesColumn))
					line.Certificates = Convert.IsDBNull(r[certificatesColumn]) ? null : r[certificatesColumn].ToString();

				line.SerialNumber = Convert.IsDBNull(r["SERIA"]) ? null : r["SERIA"].ToString();
				line.SetNds(Convert.ToDecimal(r["STAVKANDS"], CultureInfo.InvariantCulture));
				if (!String.IsNullOrEmpty(vitallyImportantColumn))
					line.VitallyImportant = Convert.IsDBNull(r[vitallyImportantColumn]) ? null : (bool?)(Convert.ToUInt32(r[vitallyImportantColumn]) == 1);
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NTTN") &&
				   data.Columns.Contains("DTTN") &&
				   data.Columns.Contains("IZGOT") &&
				   data.Columns.Contains("KOD") &&
				   data.Columns.Contains("CENAIZG") &&
				   data.Columns.Contains("CENAOPT") &&
				   data.Columns.Contains("SRGOD") &&
				   data.Columns.Contains("SERIA") &&
				   data.Columns.Contains("STAVKANDS") &&
				   !data.Columns.Contains("CENIZGBNDS");
		}
	}
}
