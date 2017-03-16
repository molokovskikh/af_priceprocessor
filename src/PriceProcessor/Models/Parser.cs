using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using log4net;

namespace Inforoom.PriceProcessor.Models
{
	public enum EncodingEnum
	{
		Default,
		UTF8,
		UTF7,
		CP1251,
		CP866
	}

	[ActiveRecord(Schema = "Customers")]
	public class Parser
	{
		static ILog Log = LogManager.GetLogger(typeof(Parser));

		public Parser()
		{
			Lines = new List<ParserLine>();
		}

		public Parser(string name, Supplier supplier)
			: this()
		{
			Name = name;
			Supplier = supplier;
		}

		public Parser(string name, Supplier supplier, EncodingEnum encoding)
			: this(name, supplier)
		{
			Encoding = encoding;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo]
		public virtual Supplier Supplier { get; set; }

		[Property]
		public virtual EncodingEnum Encoding { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<ParserLine> Lines { get; set; }

		public static Document Parse(DocumentReceiveLog log, string file, List<Parser> parsers)
		{
			if (!Path.GetExtension(file).Match(".dbf"))
				return null;
			DataTable table;
			try {
				table = Dbf.Load(file);
			} catch(Exception e) {
				Log.Warn($"Не удалось разобрать документ {file} номер входящего документа {log.Id}", e);
				return null;
			}

			var columns = table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();
			foreach (var parser in parsers) {
				if (columns.Intersect(parser.Lines.Select(x => x.Src), StringComparer.CurrentCultureIgnoreCase).Count() != parser.Lines.Count)
					continue;

				if (parser.Encoding != EncodingEnum.Default)
				{
					if (parser.Encoding == EncodingEnum.UTF8)
					{
						table = Dbf.Load(file, System.Text.Encoding.UTF8, false);
					}

					if (parser.Encoding == EncodingEnum.UTF7)
					{
						table = Dbf.Load(file, System.Text.Encoding.UTF7, false);
					}

					if (parser.Encoding == EncodingEnum.CP1251)
					{
						table = Dbf.Load(file, System.Text.Encoding.GetEncoding(1251), false);
					}

					if (parser.Encoding == EncodingEnum.CP866)
					{
						table = Dbf.Load(file, System.Text.Encoding.GetEncoding(866), false);
					}
				}
				var document = new Document(log, parser.Name);
				foreach (var dataRow in table.AsEnumerable()) {
					var docLine = document.NewLine();
					foreach (var parserLine in parser.Lines) {

						var propertyName = parserLine.Dst;
						if (String.IsNullOrEmpty(propertyName) || String.IsNullOrEmpty(parserLine.Src))
							continue;
						object target = docLine;
						if (propertyName.StartsWith("Header_")) {
							target = document;
							propertyName = propertyName.Replace("Header_", "");
						} else if (propertyName.StartsWith("Invoice_")) {
							if (document.Invoice == null)
								document.SetInvoice();
							target = document.Invoice;
							propertyName = propertyName.Replace("Invoice_", "");
						}
						var property = target.GetType().GetProperty(propertyName);
						var value = DbfParser.ConvertIfNeeded(dataRow[parserLine.Src], property.PropertyType);

						property.SetValue(target, value);
					}
				}
				return document;
			}
			return null;
		}

		public virtual void Add(string src, string dst)
		{
			Lines.Add(new ParserLine(this, src, dst));
		}
	}

	[ActiveRecord(Schema = "Customers")]
	public class ParserLine
	{
		public ParserLine()
		{
		}

		public ParserLine(Parser parser, string src, string dst)
		{
			Parser = parser;
			Src = src;
			Dst = dst;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Src { get; set; }

		[Property]
		public virtual string Dst { get; set; }

		[BelongsTo]
		public virtual Parser Parser { get; set; }
	}
}