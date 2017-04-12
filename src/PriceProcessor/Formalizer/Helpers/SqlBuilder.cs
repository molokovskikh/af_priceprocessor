using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Formalizer.Helpers
{
	public class SqlBuilder
	{
		public static string InsertOfferSql(PriceFormalizationInfo info, NewOffer offer)
		{
			var command = new StringBuilder();
			command.Append("insert into farm.Core0(PriceCode,")
				.Append(Mapping.OfferMapping.Select(m => m.Name).Implode())
				.Append(") values (")
				.Append(info.PriceCode + ",")
				.Append(Mapping.OfferMapping.Select(m => ToSql(m.GetValue(offer))).Implode())
				.Append(");")
				.Append("set @LastCoreID = last_insert_id();");
			return command.ToString();
		}

		public static string InsertCostSql(NewOffer offer, FormLog stat)
		{
			if (offer.Costs == null)
				return "";

			var costs = offer.Costs.Where(c => c.Value > 0).ToArray();
			if (costs.Length == 0)
				return "";

			stat.InsertCostCount += costs.Length;

			var command = new StringBuilder()
				.AppendFormat("insert into farm.CoreCosts (Core_ID, PC_CostCode, {0}) values ", Mapping.CostMapping.Select(m => m.Name).Implode())
				.Append(costs.Select(c => String.Format("(@LastCoreID, {0}, {1})",
					c.Description.Id,
					Mapping.CostMapping.Select(m => ToSql(m.GetValue(c))).Implode()))
					.Implode())
				.AppendLine(";");
			return command.ToString();
		}

		public static string UpdateOfferSql(NewOffer offer, FormLog stat)
		{
			var fields = BuildSetSql(offer, offer.ExistsOffer, Mapping.OfferMapping);
			if (fields.Length == 0)
				return "";
			stat.UpdateCoreCount++;
			return String.Format("update farm.Core0 set {0} where Id = {1};\r\n", fields, offer.ExistsOffer.Id);
		}

		public static string UpdateCostSql(ExistsOffer offer, Cost current, Cost old)
		{
			var fields = BuildSetSql(current, old, Mapping.CostMapping);
			if (fields.Length == 0)
				return "";

			return String.Format("update farm.CoreCosts set {0} where Core_Id = {1} and PC_CostCode = {2};",
				fields,
				offer.Id,
				current.Description.Id);
		}

		private static string BuildSetSql(object current, object old, Mapping[] mapping)
		{
			return mapping
				.Where(m => !m.Equal(old, current))
				.Select(m => String.Format("{0} = {1}", m.Name, ToSql(m.GetValue(current))))
				.Implode();
		}

		public static string UpdateCostsCommand(NewOffer offer, FormLog stat)
		{
			var command = new StringBuilder();
			var costs = (offer.Costs ?? new Cost[0]).Where(c => c.Value > 0).ToArray();

			foreach (var cost in costs) {
				Cost existsCost = null;
				if (offer.ExistsOffer.Costs != null)
					existsCost = offer.ExistsOffer.Costs.FirstOrDefault(c => c.Description.Id == cost.Description.Id);
				if (existsCost == null) {
					stat.InsertCostCount++;
					command.AppendFormat("insert into farm.CoreCosts (Core_ID, PC_CostCode, ")
						.Append(Mapping.CostMapping.Select(m => m.Name).Implode())
						.Append(") values (")
						.AppendFormat("{0}, {1},", offer.ExistsOffer.Id, cost.Description.Id)
						.Append(Mapping.CostMapping.Select(m => ToSql(m.GetValue(cost))).Implode())
						.Append(");");
				}
				else {
					var cmd = UpdateCostSql(offer.ExistsOffer, cost, existsCost);
					if (!String.IsNullOrEmpty(cmd)) {
						stat.UpdateCostCount++;
						command.Append(cmd);
					}
				}
			}

			if (offer.ExistsOffer.Costs != null) {
				var costsToDelete = offer.ExistsOffer.Costs
					.Where(c => costs.All(nc => nc.Description.Id != c.Description.Id))
					.Select(c => c.Description.Id.ToString()).ToArray();
				if (costsToDelete.Length > 0) {
					stat.DeleteCostCount += costsToDelete.Length;
					command.AppendFormat("delete from farm.CoreCosts where Core_Id = {0} and PC_CostCode in ({1});", offer.ExistsOffer.Id, String.Join(", ", costsToDelete));
				}
			}

			return command.ToString();
		}

		private static string ToSql(object value)
		{
			if (value is uint)
				return GetNullableValue((uint)value);
			if (value is ulong)
				return GetNullableValue((ulong)value);
			if (value is bool)
				return GetBoolValue((bool)value);
			if (value is decimal)
				return GetDecimalValue((decimal)value);
			if (value is string)
				return GetStringValue((string)value);
			if (value is DateTime)
				return GetDateTimeValue((DateTime)value);
			throw new Exception(String.Format("Не знаю как преобразовать {0}", value));
		}

		private static string GetDateTimeValue(DateTime value)
		{
			if (value == DateTime.MinValue)
				return "null";
			return "'" + value.ToString(MySqlConsts.MySQLDateFormat) + "'";
		}

		public static string GetBoolValue(bool value)
		{
			return value ? "1" : "0";
		}

		public static string GetStringValue(string value)
		{
			if (value == null)
				return "''";
			return "'" + Utils.StringToMySqlString(value) + "'";
		}

		public static string GetNullableValue(uint value)
		{
			if (value == 0)
				return "null";

			return value.ToString(CultureInfo.InvariantCulture);
		}

		public static string GetNullableValue(ulong value)
		{
			if (value == 0)
				return "null";

			return value.ToString(CultureInfo.InvariantCulture);
		}

		public static string GetDecimalValue(decimal value)
		{
			if (value == 0)
				return "null";

			return value.ToString(CultureInfo.InvariantCulture);
		}
	}
}