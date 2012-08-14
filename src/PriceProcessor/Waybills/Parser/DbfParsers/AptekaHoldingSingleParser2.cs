using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AptekaHoldingSingleParser2 : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			string vitallyImportantColumn = null;
			string certificatesColumn = null;
			string registryCostColumn = null;

			var data = Dbf.Load(file, Encoding);
			if (data.Columns.Contains("LIFE_REQ"))
				vitallyImportantColumn = "LIFE_REQ";

			if (data.Columns.Contains("REESTR"))
				registryCostColumn = "REESTR";

			if (data.Columns.Contains("SERT"))
				certificatesColumn = "SERT";

			document.Lines = data.Rows.Cast<DataRow>().Select(r => {
				document.ProviderDocumentId = r["NUM_DOC"].ToString();
				if (!Convert.IsDBNull(r["DATA_DOC"]))
					document.DocumentDate = Convert.ToDateTime(r["DATA_DOC"]);
				var line = document.NewLine();
				line.Code = r["CODE"].ToString();
				line.Product = r["GOOD"].ToString();
				line.Producer = r["ENTERP"].ToString();
				line.Country = r["COUNTRY"].ToString();
				line.ProducerCostWithoutNDS = Convert.IsDBNull(r["PRICEENT"]) ? null : (decimal?)Convert.ToDecimal(r["PRICEENT"], CultureInfo.InvariantCulture);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(r["PRICEWONDS"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["QUANT"]);
				line.Period = Convert.IsDBNull(r["DATEB"]) ? null : Convert.ToDateTime(r["DATEB"]).ToShortDateString();

				if (!String.IsNullOrEmpty(registryCostColumn))
					line.RegistryCost = Convert.IsDBNull(r[registryCostColumn]) ? null : (decimal?)Convert.ToDecimal(r[registryCostColumn], CultureInfo.InvariantCulture);

				if (!String.IsNullOrEmpty(certificatesColumn))
					line.Certificates = Convert.IsDBNull(r[certificatesColumn]) ? null : r[certificatesColumn].ToString();

				line.SerialNumber = Convert.IsDBNull(r["SERIAL"]) ? null : r["SERIAL"].ToString();
				line.Nds = Convert.ToUInt32(r["NDS"], CultureInfo.InvariantCulture);
				if (!String.IsNullOrEmpty(vitallyImportantColumn))
					line.VitallyImportant = Convert.IsDBNull(r[vitallyImportantColumn]) ? null : (bool?)(Convert.ToUInt32(r[vitallyImportantColumn]) == 1);
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUM_DOC") &&
				data.Columns.Contains("DATA_DOC") &&
				data.Columns.Contains("ENTERP") &&
				data.Columns.Contains("PRICEENT") &&
				data.Columns.Contains("PRICEWONDS") &&
				data.Columns.Contains("QUANT");
		}
	}
}