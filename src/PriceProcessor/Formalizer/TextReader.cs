using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer.Core;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
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
		private readonly Encoding _encoding;
		private readonly int _startLine;

		public TextParser(ISlicer slicer, Encoding encoding, int startLine)
		{
			_encoding = encoding;
			Slicer = slicer;
			_startLine = startLine;
		}

		public ISlicer Slicer { get; private set; }

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

					Slicer.Slice(table, line, row, specialProcessing);

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
			var configurable = Slicer as IConfigurable;
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
		private PriceReader _reader;

		private List<TxtFieldDef> _rules;
		private List<CostDescription> _costs;
		private PriceFormalizationInfo _info;
		private DataTable _table;

		public PositionSlicer(DataTable table, PriceFormalizationInfo info)
		{
			_table = table;
			_info = info;
		}

		private List<TxtFieldDef> LoadRules(DataTable table)
		{
			var sliceRules = new List<TxtFieldDef>();
			foreach (PriceFields field in Enum.GetValues(typeof(PriceFields))) {
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

				_reader.SetFieldName(field, name);
				sliceRules.Add(new TxtFieldDef(name, Convert.ToInt32(begin), Convert.ToInt32(end)));
			}

			foreach (var cc in _costs) {
				cc.FieldName = "Cost" + cc.Id;
				sliceRules.Add(
					new TxtFieldDef(
						cc.FieldName,
						cc.Begin,
						cc.End));
			}


			if (sliceRules.Count < 1)
				throw new WarningFormalizeException(Settings.Default.MinFieldCountError, _info.FirmCode, _info.PriceCode, _info.FirmShortName, _info.PriceName);

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
				var chunk = line.Substring(begin, length).Trim();
				row[rule.FieldName] = chunk;
			}
		}

		public void Configure(PriceReader reader)
		{
			_reader = reader;
			_costs = reader.CostDescriptions;
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
}