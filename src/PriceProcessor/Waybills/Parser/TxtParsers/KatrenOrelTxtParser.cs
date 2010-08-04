namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KatrenOrelTxtParser : BaseIndexingParser
	{
		public static bool CheckFileFormat(string file)
		{
			return CheckByHeaderPart(file, new [] {
				"зао нпк катрен",
				"роста-тюменский филиал",
				"зао \"надежда-фарм\" тамбовский ф-л",
				"ооо \"норман-плюс\""});
		}
	}
}