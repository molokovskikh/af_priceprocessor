using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public abstract class BaseDbfParser : IDocumentParser
	{
		public Encoding Encdoing;

		public virtual Document Parse(string file, Document document)
		{
			DataTable data;
			if (Encdoing == null)
				data = Dbf.Load(file);
			else
				data = Dbf.Load(file, Encdoing);

			var parser = GetParser();
			parser.ToDocument(document, data);
			PostParsing(document);
			return document;
		}

		public abstract DbfParser GetParser();

		public virtual void PostParsing(Document doc)
		{
			return;
		}

		// Если требуется дополнительная обработка документа после разбора
	}
}