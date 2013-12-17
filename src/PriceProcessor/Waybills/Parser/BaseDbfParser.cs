using System;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	/// <summary>
	/// Базовый класс для dbf парсера отличается от BaseDbfParser
	/// тем что умеет определять формат на основании совпадения полей
	/// те метод CheckFileFormat не нужен если парсер строится на основе BaseDbfParser2
	/// </summary>
	public abstract class BaseDbfParser2 : BaseDbfParser
	{
		public int CalculateHitPoints(DataTable data)
		{
			var parser = GetParser();
			var fields = parser.LineFields;
			var columns = data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
			var matchCount = fields.Intersect(columns, StringComparer.OrdinalIgnoreCase).Count();
			if (matchCount < fields.Count * 0.5)
				return 0;
			return matchCount;
		}
	}

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