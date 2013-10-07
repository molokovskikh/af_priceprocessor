using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public abstract class BaseDbfParser : IDocumentParser
	{
		public Encoding Encdoing;
		protected DataTable Data;

		public virtual Document Parse(string file, Document document)
		{
			if (Encdoing == null)
				Data = Dbf.Load(file);
			else
				Data = Dbf.Load(file, Encdoing);

			var parser = GetParser();
			parser.ToDocument(document, Data);
			PostParsing(document);
			return document;
		}

		public abstract DbfParser GetParser();

		// Если требуется дополнительная обработка документа после разбора
		public virtual void PostParsing(Document doc)
		{
		}
	}
}