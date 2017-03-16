using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using log4net;
using NPOI.SS.Formula.Functions;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord(Schema = "Customers", Table = "RejectParsers")]
	public class RejectDataParser
	{
		static ILog Log = LogManager.GetLogger(typeof (RejectDataParser));

		public RejectDataParser()
		{
			Lines = new List<RejectParserLine>();
		}

		public RejectDataParser(string name, Supplier supplier)
			: this()
		{
			Name = name;
			Supplier = supplier;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo]
		public virtual Supplier Supplier { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<RejectParserLine> Lines { get; set; }


		public static RejectHeader Parse(DocumentReceiveLog log, List<RejectDataParser> parsers)
		{
			if (!Path.GetExtension(log.GetFileName()).Match(".dbf"))
				return null;
			DataTable table;
			try {
				table = Dbf.Load(log.GetFileName());
			} catch (Exception e) {
				Log.Warn($"Не удалось разобрать документ {log.GetFileName()} номер входящего документа {log.Id}", e);
				return null;
			}

			var columns = table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();
			foreach (var parser in parsers) {
				if (columns.Intersect(parser.Lines.Select(x => x.Src), StringComparer.CurrentCultureIgnoreCase).Count() !=
					parser.Lines.Count)
					continue;

				var reject = new RejectHeader(log);
				foreach (var dataRow in table.AsEnumerable()) {
					var docLine = new RejectLine();
					foreach (var parserLine in parser.Lines) {
						var propertyName = parserLine.Dst;
						if (String.IsNullOrEmpty(propertyName) || String.IsNullOrEmpty(parserLine.Src))
							continue;
						var property = docLine.GetType().GetProperty(propertyName);
						var value = DbfParser.ConvertIfNeeded(dataRow[parserLine.Src], property.PropertyType);
						property.SetValue(docLine, value);
					}
					docLine.Header = reject;
					reject.Lines.Add(docLine);
				}
				return reject;
			}
			return null;
		}

		public virtual void Add(string src, string dst)
		{
			Lines.Add(new RejectParserLine(this, src, dst));
		}
	}

	[ActiveRecord(Schema = "Customers")]
	public class RejectParserLine
	{
		public RejectParserLine()
		{
		}

		public RejectParserLine(RejectDataParser parser, string src, string dst)
		{
			Parser = parser;
			Src = src;
			Dst = dst;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual RejectDataParser Parser { get; set; }

		[Property]
		public virtual string Src { get; set; }

		[Property]
		public virtual string Dst { get; set; }
	}
}