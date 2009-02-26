using System.Configuration;

namespace Inforoom.PriceProcessor
{
	public class Literals
	{
		public static string ConnectionString()
		{
			return ConfigurationManager.ConnectionStrings["DB"].ConnectionString;
		}
	}
}
