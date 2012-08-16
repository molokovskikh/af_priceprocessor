namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class BiolainParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			base.SetIndexes();
			SupplierCostWithoutNdsIndex = 6;
			SupplierCostIndex = 7;
			NdsIndex = 8;
			SupplierPriceMarkupIndex = 9;
			SerialNumberIndex = 10;
			PeriodIndex = 11;
			CertificatesIndex = 13;
			RegistryCostIndex = 17;
			VitallyImportantIndex = 19;
		}

		public static bool CheckFileFormat(string file)
		{
			return CheckByHeaderPart(file, new[] { "ооо \"биолайн\"" });
		}
	}
}