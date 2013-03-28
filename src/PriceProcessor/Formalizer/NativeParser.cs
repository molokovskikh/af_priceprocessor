using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public abstract class NativeParser : InterPriceParser
	{
		protected TextParser Parser;

		protected NativeParser(string priceFileName, MySqlConnection connection, PriceFormalizationInfo data)
			: base(priceFileName, connection, data)
		{
		}

		protected NativeParser(Encoding encoding, ISlicer slicer, string priceFileName, MySqlConnection connection, PriceFormalizationInfo data)
			: this(priceFileName, connection, data)
		{
			var row = data.FormRulesData.Rows[0];
			var startLine = row["StartLine"] is DBNull ? -1 : Convert.ToInt32(row["StartLine"]);
			Parser = new TextParser(slicer, encoding, startLine);
		}

		public override void Open()
		{
			var priceItemIds = new List<long>() {
				903, 1177, 951, 235, 910, 996, 1170,
				886, 1160, 90, 494, 822, 1184, 941, 468, 879, 479, 651, 977, 1004, 1032, 917, 628, 8
			};

			convertedToANSI = true;

			// Если текущий priceItemId содержится в этом списке, то для него начальный и конечный символ "
			// будет заменяться на пустоту, а "" на " (с помощью регулярного выражения)
			dtPrice = Parser.Parse(priceFileName, priceItemIds.Contains(priceItemId));
			CurrPos = 0;
			base.Open();
		}
	}

	public interface IConfigurable
	{
		void Configure(PriceReader reader);
	}

	public interface IParser
	{
		DataTable Parse(string filename);
		DataTable Parse(string filename, bool specialProcessing);
	}

	public class TextParser : IParser, IConfigurable
	{
		private readonly ISlicer _slicer;
		private readonly Encoding _encoding;
		private readonly int _startLine;

		public TextParser(ISlicer slicer, Encoding encoding, int startLine)
		{
			_encoding = encoding;
			_slicer = slicer;
			_startLine = startLine;
		}

		public DataTable Parse(string filename, bool specialProcessing)
		{
			var lineIndex = -1;
			var table = new DataTable();
			using (var file = new StreamReader(filename, _encoding)) {
				while (!file.EndOfStream) {
					var line = file.ReadLine();
					lineIndex++;
					if (lineIndex < _startLine)
						continue;

					if (line.Length == 0)
						continue;

					var row = table.NewRow();

					_slicer.Slice(table, line, row, specialProcessing);

					table.Rows.Add(row);
				}
			}
			return table;
		}

		public DataTable Parse(string filename)
		{
			return Parse(filename, false);
		}

		public void Configure(PriceReader reader)
		{
			var configurable = _slicer as IConfigurable;
			if (configurable != null)
				configurable.Configure(reader);
		}
	}

	public class TxtFieldDef : IComparer, IComparable<TxtFieldDef>
	{
		public string FieldName { get; private set; }
		public int Begin { get; private set; }
		public int End { get; private set; }

		public TxtFieldDef(string fieldName, int posBegin, int posEnd)
		{
			FieldName = fieldName;
			Begin = posBegin;
			End = posEnd;
		}

		public TxtFieldDef()
		{
		}

		public int Compare(Object x, Object y)
		{
			return (((TxtFieldDef)x).Begin - ((TxtFieldDef)y).Begin);
		}

		public int CompareTo(TxtFieldDef other)
		{
			return Begin - other.Begin;
		}
	}

	public interface ISlicer
	{
		void Slice(DataTable table, string line, DataRow row, bool specialProcessing);
	}

	public class PositionSlicer : ISlicer, IConfigurable
	{
		private BasePriceParser _parser;
		private PriceReader _reader;

		private List<TxtFieldDef> _rules;
		private List<CoreCost> _costs;
		private DataTable _table;
		private List<CostDescription> _costs2;

		public PositionSlicer(DataTable table)
		{
			_table = table;
		}

		public PositionSlicer(DataTable table, BasePriceParser parser, List<CoreCost> coreCosts)
		{
			_parser = parser;
			_costs = coreCosts;
			_table = table;
		}

		private List<TxtFieldDef> LoadRules(DataTable table)
		{
			var sliceRules = new List<TxtFieldDef>();
			foreach (PriceFields field in Enum.GetValues(typeof(PriceFields))) {
				if (_parser != null)
					_parser.SetFieldName(field, null);
				else
					_reader.SetFieldName(field, null);
				if (field == PriceFields.OriginalName || field == PriceFields.Name2 || field == PriceFields.Name3)
					continue;

				string name;
				if (field == PriceFields.Name1)
					name = "Name";
				else
					name = field.ToString();

				var begin = table.Rows[0]["Txt" + name + "Begin"];
				var end = table.Rows[0]["Txt" + name + "End"];
				if (begin == DBNull.Value || end == DBNull.Value)
					continue;

				if (_parser != null)
					_parser.SetFieldName(field, name);
				else
					_reader.SetFieldName(field, name);
				sliceRules.Add(new TxtFieldDef(name, Convert.ToInt32(begin), Convert.ToInt32(end)));
			}

			if (_costs != null) {
				foreach (var cc in _costs) {
					cc.fieldName = "Cost" + cc.costCode;
					sliceRules.Add(
						new TxtFieldDef(
							cc.fieldName,
							cc.txtBegin,
							cc.txtEnd));
				}
			}

			if (_costs2 != null) {
				foreach (var cc in _costs2) {
					cc.FieldName = "Cost" + cc.Id;
					sliceRules.Add(
						new TxtFieldDef(
							cc.FieldName,
							cc.Begin,
							cc.End));
				}
			}

			if (sliceRules.Count < 1)
				throw new WarningFormalizeException(Settings.Default.MinFieldCountError, _parser.firmCode, _parser.priceCode, _parser.firmShortName, _parser.priceName);

			return sliceRules;
		}

		public void Slice(DataTable table, string line, DataRow row, bool specialProcessing)
		{
			if (table.Columns.Count == 0)
				table.Columns.AddRange(_rules.Select(r => new DataColumn(r.FieldName)).ToArray());

			foreach (var rule in _rules) {
				var begin = rule.Begin - 1;
				if (begin > line.Length - 1)
					continue;
				var length = rule.End - rule.Begin + 1;
				if (length > line.Length - begin)
					length = line.Length - begin;
				row[rule.FieldName] = line.Substring(begin, length).Trim();
			}
		}

		public void Configure(PriceReader reader)
		{
			_reader = reader;
			_costs2 = reader.CostDescriptions;
			_rules = LoadRules(_table);
		}
	}

	public class DelimiterSlicer : ISlicer
	{
		private readonly string _delimiter;

		public DelimiterSlicer(string delimiter)
		{
			_delimiter = delimiter;
			if (_delimiter == "tab")
				_delimiter = "\t";
		}

		public void Slice(DataTable table, string line, DataRow row, bool specialProcessing)
		{
			if (specialProcessing) {
				line = Regex.Replace(line, "\"\"", "\"");
				line = Regex.Replace(line, "\t\"", "\t");
				line = Regex.Replace(line, "\"\t", "\t");
			}
			else {
				line = line.Replace("\"", "");
			}
			var values = line.Split(new[] { _delimiter }, StringSplitOptions.None);

			if (table.Columns.Count < values.Length) {
				var begin = table.Columns.Count + 1;
				var count = values.Length;
				for (var i = begin; i <= count; i++)
					table.Columns.Add(new DataColumn("F" + i));
			}

			var columnIndex = 0;
			foreach (DataColumn column in table.Columns) {
				if (columnIndex > values.Length - 1)
					break;
				row[column.ColumnName] = values[columnIndex].Trim();
				columnIndex++;
			}
		}
	}

	public class FixedNativeTextParser1251 : NativeParser
	{
		public FixedNativeTextParser1251(string priceFileName, MySqlConnection connection, PriceFormalizationInfo data)
			: base(priceFileName, connection, data)
		{
			Parser = new TextParser(new PositionSlicer(data.FormRulesData, this, currentCoreCosts),
				Encoding.GetEncoding(1251),
				-1);
		}
	}

	public class FixedNativeTextParser866 : NativeParser
	{
		public FixedNativeTextParser866(string priceFileName, MySqlConnection connection, PriceFormalizationInfo data)
			: base(priceFileName, connection, data)
		{
			Parser = new TextParser(new PositionSlicer(data.FormRulesData, this, currentCoreCosts),
				Encoding.GetEncoding(866),
				-1);
		}
	}

	public class DelimiterNativeTextParser1251 : NativeParser
	{
		public DelimiterNativeTextParser1251(string priceFileName, MySqlConnection connection, PriceFormalizationInfo data)
			: base(Encoding.GetEncoding(1251),
				new DelimiterSlicer(data.FormRulesData.Rows[0][FormRules.colDelimiter].ToString()),
				priceFileName,
				connection,
				data)
		{
		}
	}

	public class DelimiterNativeTextParser866 : NativeParser
	{
		public DelimiterNativeTextParser866(string priceFileName, MySqlConnection connection, PriceFormalizationInfo data)
			: base(Encoding.GetEncoding(866),
				new DelimiterSlicer(data.FormRulesData.Rows[0][FormRules.colDelimiter].ToString()),
				priceFileName,
				connection,
				data)
		{
		}
	}
}