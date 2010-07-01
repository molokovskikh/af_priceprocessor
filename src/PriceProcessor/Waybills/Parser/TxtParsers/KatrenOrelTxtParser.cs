using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class KatrenOrelTxtParser : BaseIndexingParser, IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			base.Parse(file, document);
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			if (Path.GetExtension(file).ToLower() != ".txt")
				return false;
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length < 4)
					return false;
				if (!header[3].ToLower().Equals("зао нпк катрен") &&
					!header[3].ToLower().Equals("ооо \"биолайн\"") &&
					!header[3].ToLower().Equals("роста-тюменский филиал") &&
					!header[3].ToLower().Equals("зао \"надежда-фарм\" тамбовский ф-л") &&
					!header[3].ToLower().Equals("ооо \"норман-плюс\""))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (GetDecimal(body[6]) == null)
					return false;
			}
			return true;
		}
	}
}