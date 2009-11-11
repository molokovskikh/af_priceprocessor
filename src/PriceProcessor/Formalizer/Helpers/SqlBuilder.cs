using System;
using System.Data;
using System.Globalization;
using System.Text;

namespace Inforoom.PriceProcessor.Formalizer.Helpers
{
	public class SqlBuilder
	{
		public static string StringToMySqlString(string s)
		{
			s = s.Replace("\\", "\\\\");
			s = s.Replace("\'", "\\\'");
			s = s.Replace("\"", "\\\"");
			s = s.Replace("`", "\\`");
			return s;
		}

		private static void AddTextParameter(string paramName, DataRow dr, StringBuilder sb)
		{
			if (dr[paramName] is DBNull)
				sb.Append("''");
			else
				sb.AppendFormat("'{0}'", StringToMySqlString(dr[paramName].ToString()));
		}

		public static void InsertCorePosition(DataRow drCore, StringBuilder sb, DataRow drNewProducerSynonym)
		{
			var producerSynonymId = drCore["SynonymFirmCrCode"];
			if (drNewProducerSynonym != null)
				producerSynonymId = drNewProducerSynonym["SynonymFirmCrCode"];

			sb.AppendLine("insert into farm.Core0 (" +
				"PriceCode, ProductId, CodeFirmCr, SynonymCode, SynonymFirmCrCode, " +
					"Period, Junk, Await, MinBoundCost, " +
						"VitallyImportant, RequestRatio, RegistryCost, " +
							"MaxBoundCost, OrderCost, MinOrderCount, " +
								"Code, CodeCr, Unit, Volume, Quantity, Note, Doc) values ");
			sb.Append("(");
			sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, ",
				drCore["PriceCode"],
				drCore["ProductId"],
				Convert.IsDBNull(drCore["CodeFirmCr"]) ? "null" : drCore["CodeFirmCr"].ToString(),
				drCore["SynonymCode"],
				Convert.IsDBNull(producerSynonymId) ? "null" : producerSynonymId.ToString());
			sb.AppendFormat("'{0}', ", (drCore["Period"] is DBNull) ? String.Empty : drCore["Period"].ToString());
			sb.AppendFormat("{0}, ", drCore["Junk"]);
			sb.AppendFormat("'{0}', ", drCore["Await"]);
			sb.AppendFormat("{0}, ", (drCore["MinBoundCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["MinBoundCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", drCore["VitallyImportant"]);
			sb.AppendFormat("{0}, ", (drCore["RequestRatio"] is DBNull) ? "null" : drCore["RequestRatio"].ToString());
			sb.AppendFormat("{0}, ", (drCore["RegistryCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["RegistryCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", (drCore["MaxBoundCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["MaxBoundCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", (drCore["OrderCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["OrderCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", (drCore["MinOrderCount"] is DBNull) ? "null" : drCore["MinOrderCount"].ToString());
			AddTextParameter("Code", drCore, sb);
			sb.Append(", ");

			AddTextParameter("CodeCr", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Unit", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Volume", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Quantity", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Note", drCore, sb);
			sb.Append(", ");

			AddTextParameter("Doc", drCore, sb);

			sb.AppendLine(");");
			sb.AppendLine("set @LastCoreID = last_insert_id();");
		}
	}
}