using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KronikaLtdParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			string vitallyImportantColumn = null;
			string certificatesColumn = null;
			string registryCostColumn = null;
			string ndsColumn = null;
			string AmountColumn = null;
			string NdsAmountColumn = null;
			string SupplierPriceMarkupColumn = null;

			string certificateFilenameColumn = null;
			string protocolFilemameColumn = null;
			string passportFilenameColumn = null;

			string billOfEntryNumberColumn = null;
			string countryCode = null;
			string ean13Collumn = null;


			var data = Dbf.Load(file, Encoding);
			if (data.Columns.Contains("PV"))
				vitallyImportantColumn = "PV";

			if (data.Columns.Contains("REG"))
				registryCostColumn = "REG";

			if (data.Columns.Contains("SERTIF"))
				certificatesColumn = "SERTIF";

			if (data.Columns.Contains("EAN13"))
				ean13Collumn = "EAN13";

			if (data.Columns.Contains("NDSstavk"))
				ndsColumn = "NDSstavk";
			else if (data.Columns.Contains("NDSstavk"))
				ndsColumn = "NDSSTAVK";

			if (data.Columns.Contains("SUMMA"))
				AmountColumn = "SUMMA";
			if (data.Columns.Contains("NDS"))
				NdsAmountColumn = "NDS";
			if (data.Columns.Contains("TORGNADB"))
				SupplierPriceMarkupColumn = "TORGNADB";

			if (data.Columns.Contains("F_SERT"))
				certificateFilenameColumn = "F_SERT";
			if (data.Columns.Contains("F_PROT"))
				protocolFilemameColumn = "F_PROT";
			if (data.Columns.Contains("F_PASS"))
				passportFilenameColumn = "F_PASS";

			if(data.Columns.Contains("GTD"))
				billOfEntryNumberColumn = "GTD";

			if(data.Columns.Contains("PRIZN"))
				countryCode = "PRIZN";

			document.Lines = data.Rows.Cast<DataRow>().Select(r => {
				document.ProviderDocumentId = Convert.ToString(r["DOCNO"]);
				if (!Convert.IsDBNull(r["DOCDAT"]))
					document.DocumentDate = Convert.ToDateTime(r["DOCDAT"]);
				var line = document.NewLine();
				line.Code = r["CODE"].ToString();
				if (data.Columns.Contains("CLEANNAME"))
					line.Product = r["CLEANNAME"].ToString();
				else if (data.Columns.Contains("TOVAR"))
					line.Product = r["TOVAR"].ToString();
				line.Producer = r["PROIZV"].ToString();
				line.Country = r["STRANA"].ToString();
				line.ProducerCostWithoutNDS = Convert.IsDBNull(r["ZAVOD"]) ? null : (decimal?)Convert.ToDecimal(r["ZAVOD"], CultureInfo.InvariantCulture);
				if (data.Columns.Contains("TZENA"))
					line.SupplierCostWithoutNDS = Convert.ToDecimal(r["TZENA"], CultureInfo.InvariantCulture);
				line.SupplierCost = ParseHelper.GetDecimal(r["TZENANDS"].ToString());
				line.Quantity = Convert.ToUInt32(r["KOL"]);
				line.Period = Convert.IsDBNull(r["GODEN"]) ? null : Convert.ToDateTime(r["GODEN"]).ToShortDateString();

				if(!String.IsNullOrEmpty(billOfEntryNumberColumn))
					line.BillOfEntryNumber = r[billOfEntryNumberColumn].ToString();

				if(!String.IsNullOrEmpty(countryCode))
					line.CountryCode = r[countryCode].ToString();

				if (!String.IsNullOrEmpty(registryCostColumn) && !Convert.IsDBNull(r[registryCostColumn])) {
					decimal value;
					if (decimal.TryParse(r[registryCostColumn].ToString(), out value))
						line.RegistryCost = value;
				}

				if (!String.IsNullOrEmpty(certificatesColumn))
					line.Certificates = Convert.IsDBNull(r[certificatesColumn]) ? null : r[certificatesColumn].ToString();

				line.SerialNumber = Convert.IsDBNull(r["SERIA"]) ? null : r["SERIA"].ToString();
				line.Nds = Convert.IsDBNull(r[ndsColumn]) ? null : (uint?)Convert.ToUInt32(r[ndsColumn], CultureInfo.InvariantCulture);
				if (!String.IsNullOrEmpty(vitallyImportantColumn))
					line.VitallyImportant = Convert.IsDBNull(r[vitallyImportantColumn]) ? null : (bool?)(Convert.ToUInt32(r[vitallyImportantColumn]) == 1);
				if (!String.IsNullOrEmpty(AmountColumn)) {
					line.Amount = ParseHelper.GetDecimal(r[AmountColumn].ToString());
				}
				if (!String.IsNullOrEmpty(NdsAmountColumn)) {
					line.NdsAmount = ParseHelper.GetDecimal(r[NdsAmountColumn].ToString());
				}
				if (!String.IsNullOrEmpty(SupplierPriceMarkupColumn)) {
					line.SupplierPriceMarkup = ParseHelper.GetDecimal(r[SupplierPriceMarkupColumn].ToString());
				}

				if (!String.IsNullOrEmpty(certificateFilenameColumn))
					line.CertificateFilename = r[certificateFilenameColumn].ToString();
				if (!String.IsNullOrEmpty(protocolFilemameColumn))
					line.ProtocolFilemame = r[protocolFilemameColumn].ToString();
				if (!String.IsNullOrEmpty(passportFilenameColumn))
					line.PassportFilename = r[passportFilenameColumn].ToString();
				if (!string.IsNullOrEmpty(ean13Collumn))
					line.EAN13 = NullableConvert.ToUInt64(r[ean13Collumn].ToString());

				if(data.Columns.Contains("ED_CODE"))
					line.UnitCode = r["ED_CODE"].ToString();
				if (data.Columns.Contains("C_CODE"))
					line.CountryCode = r["C_CODE"].ToString();

				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DOCNO") &&
				data.Columns.Contains("TOVAR") &&
				data.Columns.Contains("CODE") &&
				data.Columns.Contains("PROIZV") &&
				data.Columns.Contains("TZENANDS") &&
				data.Columns.Contains("GODEN") &&
				data.Columns.Contains("KOL") &&
				data.Columns.Contains("DOCDAT");
		}
	}
}