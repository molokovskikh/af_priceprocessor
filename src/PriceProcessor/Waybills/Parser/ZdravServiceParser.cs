using System.Data;
using System.IO;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class ZdravServiceParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			return new PulsFKParser{Encdoing = Encoding.GetEncoding(1251)}.Parse(file, document);
		}

        public static bool CheckFileFormat(string file)
        {
            if (Path.GetExtension(file.ToLower()) != ".dbf")
                return false;

            var data = Dbf.Load(file);
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