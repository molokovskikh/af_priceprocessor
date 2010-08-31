using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class ZdravServiceParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			return new PulsFKParser{Encdoing = Encoding.GetEncoding(1251)}.Parse(file, document);
		}
	}
}