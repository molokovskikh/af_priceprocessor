using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public abstract class UnsafeBaseDbfParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			DataTable data = null;
			try
			{
				data = Dbf.Load(file);
			}
			catch (DbfException)
			{
				data = Dbf.Load(file, Encoding, true, false);
			}
			var parser = GetParser();
			parser.ToDocument(document, data);
			return document;
		}

		protected void SetEncoding(Encoding encoding)
		{
			Encoding = encoding;
		}

		public abstract DbfParser GetParser();
	}
}
