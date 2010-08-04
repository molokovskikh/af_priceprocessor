namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class ShafievParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			ProducerCostIndex = 5;
			SupplierCostWithoutNdsIndex = 6;
			NdsIndex = 7;
			SerialNumberIndex = 9;
			PeriodIndex = 10;
			CertificatesIndex = 12;
			RegistryCostIndex = 17;
		}

		public static bool CheckFileFormat(string file)
		{
			return CheckByHeaderPart(file, new [] {"�������������� ��������������� ������ ����� ������"});
		}
	}
}