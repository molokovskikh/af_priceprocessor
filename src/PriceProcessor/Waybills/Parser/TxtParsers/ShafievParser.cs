namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class ShafievParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			CommentMark = "-";
			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			ProducerCostWithoutNdsIndex = 5;
			SupplierCostWithoutNdsIndex = 6;
			NdsIndex = 7;
			SerialNumberIndex = 9;
			PeriodIndex = 10;
			CertificatesIndex = 12;
			RegistryCostIndex = 16;
			VitallyImportantIndex = 19;
			NdsAmountIndex = 20;
		}

		public static bool CheckFileFormat(string file)
		{
			return CheckByHeaderPart(file, new[] { "Индивидуальный Предприниматель Шафиев Наиль Энверо", "ООО \"МОРОН\"", "ООО \"Ориола\"" }, "-");
		}
	}
}