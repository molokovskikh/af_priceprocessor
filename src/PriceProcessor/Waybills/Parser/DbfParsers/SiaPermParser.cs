using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SiaPermParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			string vitallyImportantColumn = null;
			string certificatesColumn = null;
			string registryCostColumn = null;

			var data = Dbf.Load(file, Encoding);
			if (data.Columns.Contains("ISLIVE"))
				vitallyImportantColumn = "ISLIVE";

			if (data.Columns.Contains("CENA_R"))
				registryCostColumn = "CENA_R";

			if (data.Columns.Contains("NOM_SERT"))
				certificatesColumn = "NOM_SERT";

			document.Lines = data.Rows.Cast<DataRow>().Select(r =>
			{
				document.ProviderDocumentId = Convert.ToString(r["DOK"]);
				if (!Convert.IsDBNull(r["DD"]))
					document.DocumentDate = Convert.ToDateTime(r["DD"]);
				var line = document.NewLine();
				line.Code = r["IDPOS"].ToString();
				line.Product = r["NP"].ToString();
				line.Producer = r["NAME_PRO"].ToString();
				line.Country = r["NAME_COU"].ToString();
				line.ProducerCost = Convert.IsDBNull(r["PROCENA"]) ? null : (decimal?)Convert.ToDecimal(r["PROCENA"], CultureInfo.InvariantCulture);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(r["CENA"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["CENA_S_NDS"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["KOL"]);
				line.Period = Convert.IsDBNull(r["DG"]) ? null : Convert.ToDateTime(r["DG"]).ToShortDateString();

				if (!String.IsNullOrEmpty(registryCostColumn))
					line.RegistryCost = Convert.IsDBNull(r[registryCostColumn]) ? null :
						(decimal?)Convert.ToDecimal(r[registryCostColumn], CultureInfo.InvariantCulture);

				if (!String.IsNullOrEmpty(certificatesColumn))
					line.Certificates = Convert.IsDBNull(r[certificatesColumn]) ? null : r[certificatesColumn].ToString();

				line.SerialNumber = Convert.IsDBNull(r["SERIY"]) ? null : r["SERIY"].ToString();
				line.Nds = Convert.IsDBNull(r["NDS"]) ? null : (uint?)Convert.ToUInt32(r["NDS"], CultureInfo.InvariantCulture);
				if (!String.IsNullOrEmpty(vitallyImportantColumn))
					line.VitallyImportant = Convert.IsDBNull(r[vitallyImportantColumn]) ? null : (bool?)(Convert.ToUInt32(r[vitallyImportantColumn]) == 1);
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DOK") &&
				   data.Columns.Contains("DD") &&
				   data.Columns.Contains("IDPOS") &&
				   data.Columns.Contains("NAME_PRO") &&
				   data.Columns.Contains("CENA") &&
				   data.Columns.Contains("CENA_S_NDS") &&
				   data.Columns.Contains("KOL") &&
				   data.Columns.Contains("NDS");
		}
	}
}
