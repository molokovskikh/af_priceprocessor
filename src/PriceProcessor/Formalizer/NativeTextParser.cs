﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Properties;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public abstract class NativeTextParser : InterPriceParser
	{
		private readonly int _startLine;
		protected Encoding _encoding;
		protected ISlicer _slicer;

		protected NativeTextParser(string priceFileName, MySqlConnection connection, DataTable data) 
			: base(priceFileName, connection, data)
		{
			_startLine = data.Rows[0]["StartLine"] is DBNull ? -1 : Convert.ToInt32(data.Rows[0]["StartLine"]);
		}

		protected NativeTextParser(Encoding encoding, ISlicer slicer, string priceFileName, MySqlConnection connection, DataTable data) 
			: this(priceFileName, connection, data)
		{
			_encoding = encoding;
			_slicer = slicer;
		}

		public override void Open()
		{
			convertedToANSI = true;
			var lineIndex = -1;
			var table = new DataTable();
			using(var file = new StreamReader(priceFileName, _encoding))
			{
				while(!file.EndOfStream)
				{
					var line = file.ReadLine();
					lineIndex++;
					if (lineIndex < _startLine)
						continue;

					if (line.Length == 0)
						continue;

					var row = table.NewRow();

					_slicer.Slice(table, line, row);

					table.Rows.Add(row);
				}
			}
			dtPrice = table;
			CurrPos = 0;
			base.Open();
		}
	}

	public class TxtFieldDef : IComparer, IComparable<TxtFieldDef>
	{
		public string FieldName { get; private set;}
		public int Begin { get; private set;}
		public int End { get; private set;}

		public TxtFieldDef(string fieldName, int posBegin, int posEnd)
		{
			FieldName = fieldName;
			Begin = posBegin;
			End = posEnd;
		}

		public TxtFieldDef() {}

		public int Compare( Object x, Object y )
		{
			return ( ((TxtFieldDef)x).Begin - ((TxtFieldDef)y).Begin );
		}

		public int CompareTo(TxtFieldDef other)
		{
			return Begin - other.Begin;
		}
	}

	public interface ISlicer
	{
		void Slice(DataTable table, string line, DataRow row);
	}

	public class PositionSlicer : ISlicer
	{
		private readonly List<TxtFieldDef> _rules;
		private BasePriceParser _parser;
		private List<CoreCost> _costs;

		public PositionSlicer(DataTable table, BasePriceParser parser, List<CoreCost> coreCosts)
		{
			_parser = parser;
			_costs = coreCosts;
			_rules = LoadRules(table);
		}

		private List<TxtFieldDef> LoadRules(DataTable table)
		{
			var sliceRules = new List<TxtFieldDef>();
			foreach(PriceFields field in Enum.GetValues(typeof(PriceFields)))
			{
				_parser.SetFieldName(field, null);
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

				_parser.SetFieldName(field, name);
				sliceRules.Add(new TxtFieldDef(name, Convert.ToInt32(begin), Convert.ToInt32(end)));
			}

			foreach(var cc in _costs)
			{
				cc.fieldName = "Cost" + cc.costCode;
				sliceRules.Add(
					new TxtFieldDef(
						cc.fieldName,
						cc.txtBegin,
						cc.txtEnd
						)
					);
			}

			if (sliceRules.Count < 1)
				throw new WarningFormalizeException(Settings.Default.MinFieldCountError, _parser.firmCode, _parser.priceCode, _parser.firmShortName, _parser.priceName);

			return sliceRules;
		}

		public void Slice(DataTable table, string line, DataRow row)
		{
			if (table.Columns.Count == 0)
				table.Columns.AddRange(_rules.Select(r => new DataColumn(r.FieldName)).ToArray());

			foreach(var rule in _rules)
			{
				var begin = rule.Begin - 1;
				if (begin > line.Length - 1)
					continue;
				var length = rule.End - rule.Begin + 1;
				if (length > line.Length - begin)
					length = line.Length - begin;
				row[rule.FieldName] = line.Substring(begin, length).Trim();
			}
		}
	}

	public class DelimiterSlicer : ISlicer
	{
		private readonly string _delimiter;

		public DelimiterSlicer(DataTable data)
		{
			_delimiter = data.Rows[0][FormRules.colDelimiter].ToString();
			if (_delimiter == "tab")
				_delimiter = "\t";
		}

		public void Slice(DataTable table, string line, DataRow row)
		{
			line = line.Replace("\"", "");
			var values = line.Split(new [] { _delimiter }, StringSplitOptions.None);

			if (table.Columns.Count < values.Length)
			{
				var begin = table.Columns.Count + 1;
				var count = values.Length;
				for(var i = begin; i <= count; i++)
					table.Columns.Add(new DataColumn("F" + i));
			}

			var columnIndex = 0;
			foreach (DataColumn column in table.Columns)
			{
				if (columnIndex > values.Length - 1)
					break;
				row[column.ColumnName] = values[columnIndex].Trim();
				columnIndex++;
			}
		}
	}

	public class FixedNativeTextParser1251 : NativeTextParser
	{
		public FixedNativeTextParser1251(string priceFileName, MySqlConnection connection, DataTable data)
			: base(priceFileName, connection, data)
		{
			_encoding = Encoding.GetEncoding(1251);
			_slicer = new PositionSlicer(data, this, currentCoreCosts);
		}
	}

	public class FixedNativeTextParser866 : NativeTextParser
	{
		public FixedNativeTextParser866(string priceFileName, MySqlConnection connection, DataTable data)
			: base(priceFileName, connection, data)
		{
			_encoding = Encoding.GetEncoding(866);
			_slicer = new PositionSlicer(data, this, currentCoreCosts);
		}
	}

	public class DelimiterNativeTextParser1251 : NativeTextParser
	{
		public DelimiterNativeTextParser1251(string priceFileName, MySqlConnection connection, DataTable data)
			: base(Encoding.GetEncoding(1251), new DelimiterSlicer(data), priceFileName, connection, data)
		{}
	}

	public class DelimiterNativeTextParser866 : NativeTextParser
	{
		public DelimiterNativeTextParser866(string priceFileName, MySqlConnection connection, DataTable data)
			: base(Encoding.GetEncoding(866), new DelimiterSlicer(data), priceFileName, connection, data)
		{}
	}
}
