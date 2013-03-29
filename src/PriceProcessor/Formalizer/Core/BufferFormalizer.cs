using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using ExcelLibrary.BinaryFileFormat;
using Inforoom.Formalizer;

namespace Inforoom.PriceProcessor.Formalizer.Core
{
	public class BufferFormalizer : BaseFormalizer, IPriceFormalizer
	{
		protected Encoding Encoding;
		protected IParser Parser;

		public BufferFormalizer(string filename, PriceFormalizationInfo data)
			: base(filename, data)
		{
			Encoding = Encoding.GetEncoding(1251);
		}

		public void Formalize()
		{
			var reader = CreateReader();
			FormalizePrice(reader);
		}

		public IList<string> GetAllNames()
		{
			var reader = CreateReader();
			return reader.Read()
				.Select(p => p.PositionName)
				.Where(n => !String.IsNullOrEmpty(n))
				.ToList();
		}

		private PriceReader CreateReader()
		{
			var row = Info.FormRulesData.Rows[0];
			var slicer = new DelimiterSlicer(row[FormRules.colDelimiter].ToString());
			var startLine = row["StartLine"] is DBNull ? -1 : Convert.ToInt32(row["StartLine"]);
			if (Parser == null)
				Parser = new TextParser(slicer, Encoding, startLine);
			var reader = new PriceReader(Parser, _fileName, Info);
			return reader;
		}
	}

	public class DelimiterTextParser1251 : BufferFormalizer
	{
		public DelimiterTextParser1251(string filename, PriceFormalizationInfo data)
			: base(filename, data)
		{
		}
	}

	public class DelimiterTextParser866 : BufferFormalizer
	{
		public DelimiterTextParser866(string filename, PriceFormalizationInfo data)
			: base(filename, data)
		{
			Encoding = Encoding.GetEncoding(866);
		}
	}

	public class FixedTextParser1251 : BufferFormalizer
	{
		public FixedTextParser1251(string filename, PriceFormalizationInfo data)
			: base(filename, data)
		{
			Parser = new TextParser(new PositionSlicer(data.FormRulesData, data),
				Encoding.GetEncoding(1251),
				-1);
		}
	}

	public class FixedTextParser866 : BufferFormalizer
	{
		public FixedTextParser866(string filename, PriceFormalizationInfo data)
			: base(filename, data)
		{
			Parser = new TextParser(new PositionSlicer(data.FormRulesData, data),
				Encoding.GetEncoding(866),
				-1);
		}
	}

	public class ExcelParser : BufferFormalizer
	{
		public ExcelParser(string filename, PriceFormalizationInfo data)
			: base(filename, data)
		{
			var row = data.FormRulesData.Rows[0];
			Parser = new ExcelReader(
				row["ListName"].ToString().Replace("$", ""),
				row["StartLine"] is DBNull ? 0 : Convert.ToInt32(row["StartLine"]));

			//медицина пишет в срок годносит 1953 год если срок годности не ограничен всякие скальпели и прочее
			if (data.PriceItemId == 822)
				((ExcelReader)Parser).NullDate = new DateTime(1953, 01, 01);
			StringDecoder.DefaultEncoding = Encoding.GetEncoding(1251);
		}
	}

	public class DbfReader : IParser
	{
		private bool strict;

		public DbfReader(bool strict)
		{
			this.strict = strict;
		}

		public DataTable Parse(string filename)
		{
			return Dbf.Load(filename, Encoding.GetEncoding(866), false, strict);
		}

		public DataTable Parse(string filename, bool specialProcessing)
		{
			return Parse(filename);
		}
	}

	public class PriceDbfParser : BufferFormalizer
	{
		public PriceDbfParser(string filename, PriceFormalizationInfo data)
			: base(filename, data)
		{
			Parser = new DbfReader(data.Price.IsStrict);
		}
	}
}