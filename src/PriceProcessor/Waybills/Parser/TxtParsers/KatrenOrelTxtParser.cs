using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KatrenOrelTxtParser : BaseIndexingParser
	{
		/*public static bool CheckFileFormat(string file)
		{
			return CheckByHeaderPart(file, new [] {
				"зао нпк катрен",
				"роста-тюменский филиал",
				"зао \"надежда-фарм\" тамбовский ф-л",
				"ооо \"норман-плюс\""});
		}*/
		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if ((header.Length != 7) || !header[3].ToLower().Contains("зао нпк катрен"))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (GetDecimal(body[6]) == null)
					return false;
				if (GetDateTime(body[16]) != null)
					return false;

			}
			return true;
		}

	}
}