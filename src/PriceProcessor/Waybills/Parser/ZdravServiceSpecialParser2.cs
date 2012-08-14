using System.Data;
using System.IO;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class ZdravServiceSpecialParser2 : IDocumentParser
	{
		private static Encoding encoding = Encoding.GetEncoding(1251);

		public static DataTable Load(string file)
		{
			return Dbf.Load(file, encoding);
		}

		public Document Parse(string file, Document document)
		{
			return new PulsFKParser { Encdoing = encoding }.Parse(file, document);
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NDOC")
				&& data.Columns.Contains("CNTR")
				&& data.Columns.Contains("SERTIF")
				&& data.Columns.Contains("GDATE")
				&& data.Columns.Contains("QNT")
				&& !data.Columns.Contains("PROVIDER")
				&& !data.Columns.Contains("CONSIGNOR");
		}
	}
}