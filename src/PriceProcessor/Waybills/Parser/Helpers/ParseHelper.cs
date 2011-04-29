using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.Helpers
{
	public class ParseHelper
	{
		public static decimal? GetDecimal(string val)
		{
			decimal value;
			if (!String.IsNullOrEmpty(val) && decimal.TryParse(val, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
				return value;
			if (!String.IsNullOrEmpty(val) && decimal.TryParse(val, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
				return value;
			return null;
		}

		public static uint? GetUInt(string val)
		{
			var value = GetDecimal(val);
			if (value.HasValue)
				return Convert.ToUInt32(value);
			uint value2;
			if (!String.IsNullOrEmpty(val) && uint.TryParse(val, out value2))
				return value2;
			return null;
		}

		public static int? GetInt(string val)
		{
			var value = GetDecimal(val);
			if (value.HasValue)
				return Convert.ToInt32(value);
			int value2;
			if (!String.IsNullOrEmpty(val) && int.TryParse(val, out value2))
				return value2;
			return null;
		}

		public static bool? GetBoolean(string val)
		{
			int value;
			if (!String.IsNullOrEmpty(val) && int.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
				return (value != 0);
			return null;
		}

		public static DateTime? GetDateTime(string val)
		{
			DateTime value;
			if (!String.IsNullOrEmpty(val) && DateTime.TryParse(val, out value))
				return value;
			return null;
		}

		public static string GetString(string val)
		{
			if (!String.IsNullOrEmpty(val))
				return val.Trim();
			return null;
		}
	}
}
