using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public abstract class BaseDbfParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, true);
			var parser = GetParser();
			parser.ToDocument(document, data);
			return document;
		}

		public abstract DbfParser GetParser();
	}
}