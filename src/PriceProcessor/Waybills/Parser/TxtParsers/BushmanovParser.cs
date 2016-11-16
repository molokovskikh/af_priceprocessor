using Inforoom.PriceProcessor.Waybills.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using System.Text.RegularExpressions;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	// http://redmine.analit.net/issues/56821
	public class BushmanovParser : BaseIndexingParser
	{
		private static Regex regex = new Regex(@"(?<bn>.*)\s\((?<ba>.*)\)\s-\s(?<pd>\d+)-\w*\.tdf");

		protected override void SetIndexes()
		{
			CodeIndex = 0;
			EAN13Index = 1;
			ProductIndex = 2;
			ProducerIndex = 3;
			CountryIndex = 4;
			QuantityIndex = 5;
			SupplierCostIndex = 6;
			AmountIndex = 7;
			NdsIndex = 8;
			CertificatesIndex = 9;
			CertificatesDateIndex = 10;
			CertificatesEndDateIndex = 11;
			CertificateAuthorityIndex = 12;
		}

		public override Document Parse(string file, Document document)
		{
			var match = regex.Match(file);
			document.ProviderDocumentId = match.Groups["pd"].Value;
			document.DocumentDate = DateTime.Today;
			document.SetInvoice().BuyerName = match.Groups["bn"].Value;
			document.SetInvoice().BuyerAddress = match.Groups["ba"].Value;

			SetIndexes();
			using (var reader = new StreamReader(file, Encoding.GetEncoding(866))) {
				using (var parser = new HeaderBodyParser(reader, CommentMark)) {
					foreach (var body in parser.Lines())
						ReadBody(document, body);
				}
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			if (!regex.IsMatch(file))
				return false;
			using (var reader = new StreamReader(file, Encoding.GetEncoding(866)))
			{
				var line = reader.ReadLine();
				if (String.IsNullOrEmpty(line))
					return false;
				if (line.Split(';').Length != 13)
					return false;
				return true;
			}
		}
	}
}