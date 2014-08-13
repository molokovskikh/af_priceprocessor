using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KatrenOrelTxtParser : BaseIndexingParser
	{
		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headerCaption = reader.ReadLine();
				if (headerCaption == null)
					return false;
				if (!headerCaption.Equals("[header]", StringComparison.InvariantCultureIgnoreCase))
					return false;
				var headerLine = reader.ReadLine();
				if (headerLine == null)
					return false;
				var header = headerLine.Split(';');
				if (header.Length != 7 || header[3].IndexOf("зао нпк катрен", StringComparison.CurrentCultureIgnoreCase) < 0)
					return false;

				var bodyCaption = reader.ReadLine();
				if (bodyCaption == null)
					return false;
				if (!bodyCaption.Equals("[body]", StringComparison.InvariantCultureIgnoreCase))
					return false;

				var bodyLine = reader.ReadLine();
				if (bodyLine == null)
					return false;
				var body = bodyLine.Split(';');
				if (body.Length < 11)
					return false;
				if (GetDecimal(body[6]) == null)
					return false;

				//пытаемся разобрать дату по шаблону иначе слишком много неопределенности
				//мы можем маленькое число интерпретировать как дату пример 9.28
				DateTime period;
				if (!DateTime.TryParseExact(body[10], "dd.MM.yyyy", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out period))
					return false;
			}
			return true;
		}
	}
}