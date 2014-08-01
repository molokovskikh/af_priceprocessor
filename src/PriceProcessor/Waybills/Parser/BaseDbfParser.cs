using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
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
		private static Dictionary<PropertyInfo, int> knownWeights = new Dictionary<PropertyInfo, int> {
			{ typeof(DocumentLine).GetProperty("Product"), 1000 },
			{ typeof(DocumentLine).GetProperty("Quantity"), 1000 },
			{ typeof(DocumentLine).GetProperty("SupplierCost"), 500 },
			{ typeof(DocumentLine).GetProperty("SupplierCostWithoutNDS"), 500 },
		};

		public int CalculateHitPoints(DataTable data)
		{
			var parser = GetParser();
			var fields = parser.LineMap;
			var columns = data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
			var weight = fields
				.Where(p => p.Value.Intersect(columns, StringComparer.OrdinalIgnoreCase).Any())
				.Select(f => f.Key)
				.Sum(p => knownWeights.GetValueOrDefault(p, 1));
			var count = fields.SelectMany(f => f.Value).Intersect(columns, StringComparer.OrdinalIgnoreCase).Count();
			//эвристика для того что бы отсечь форматы в которых совпало несколько ключевых колонок
			//если удалось идентифицировать меньше половины колонок из файла
			//и если определение совпадает с файлом меньше чем на половину
			if (weight < 2500 || (count < columns.Length * 0.5 && count < fields.Count * 0.5))
				return 0;
			return weight;
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