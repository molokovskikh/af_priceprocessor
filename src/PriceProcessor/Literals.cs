using System;
using System.Configuration;

namespace Inforoom.PriceProcessor
{
	public class Literals
	{
		/*public static string ConnectionString()
		{
			return ConfigurationManager.ConnectionStrings["DB"].ConnectionString;
		}*/

#if DEBUG
        public static bool IsIntegration()
        {
            return String.Equals(System.Environment.MachineName, "devsrv", StringComparison.OrdinalIgnoreCase)
                && ConfigurationManager.ConnectionStrings["integration"] != null;
        }
#endif


        public static string GetConnectionName()
        {
#if (DEBUG)
            if (IsIntegration())
                return "integration";
            else
                return "Local";
#else
			return "Main";
#endif
        }

        public static string ConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[GetConnectionName()].ConnectionString;
        }
	}
}
