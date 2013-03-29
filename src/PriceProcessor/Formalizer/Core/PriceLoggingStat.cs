namespace Inforoom.PriceProcessor.Formalizer.Core
{
	public class PriceLoggingStat
	{
		//Кол-во успешно формализованных
		public int formCount;
		//Кол-во "нулей"
		public int zeroCount;
		//Кол-во нераспознанных событий
		public int unformCount;
		//Кол-во нераспознаных по всей форме
		public int unrecCount;
		//Кол-во "запрещенных" позиций
		public int forbCount;
		//Максимальное кол-во рестартов транзакций при применении прайс-листа в базу данных
		public int maxLockCount;
	}
}