using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer.Core;

namespace Inforoom.PriceProcessor.Formalizer.Helpers
{
	public class SqlBuilder
	{
		public static string UpdateOfferSql(PriceFormalizationInfo info, NewCore core)
		{
			var command = new StringBuilder();
			command.Append("insert into farm.Core0(PriceCode,")
				.Append(Mapping.OfferMapping.Select(m => m.Name).Implode())
				.Append(") values (")
				.Append(info.PriceCode + ",")
				.Append(Mapping.OfferMapping.Select(m => ToSql(m.GetValue(core))).Implode())
				.Append(");")
				.Append("set @LastCoreID = last_insert_id();");
			return command.ToString();
		}

		public static string InsertCostSql(NewCore core)
		{
			if (core.Costs == null)
				return "";

			var costs = core.Costs.Where(c => c.Value > 0).ToArray();
			if (costs.Length == 0)
				return "";

			var command = new StringBuilder()
				.AppendFormat("insert into farm.CoreCosts (Core_ID, PC_CostCode, {0}) values ", Mapping.CostMapping.Select(m => m.Name).Implode())
				.Append(costs.Select(c => String.Format("(@LastCoreID, {0}, {1})",
					c.Description.Id,
					Mapping.CostMapping.Select(m => ToSql(m.GetValue(c))).Implode()))
					.Implode())
				.AppendLine(";");
			return command.ToString();
		}

		public static string UpdateOfferSql(NewCore core)
		{
			var fields = BuildSetSql(core, core.ExistsCore, Mapping.OfferMapping);
			if (fields.Length == 0)
				return "";

			return String.Format("update farm.Core0 set {0} where Id = {1};\r\n", fields, core.ExistsCore.Id);
		}

		public static string UpdateCostSql(ExistsCore offer, Cost current, Cost old)
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

		public static string UpdateCostsCommand(NewCore core)
		{
			var command = new StringBuilder();
			var costs = (core.Costs ?? new Cost[0]).Where(c => c.Value > 0).ToArray();

			foreach (var cost in costs) {
				Cost existsCost = null;
				if (core.ExistsCore.Costs != null)
					existsCost = core.ExistsCore.Costs.FirstOrDefault(c => c.Description.Id == cost.Description.Id);
				if (existsCost == null) {
					command.AppendFormat("insert into farm.CoreCosts (Core_ID, PC_CostCode, ")
						.Append(Mapping.CostMapping.Select(m => m.Name).Implode())
						.Append(") values (")
						.AppendFormat("{0}, {1},", core.ExistsCore.Id, cost.Description.Id)
						.Append(Mapping.CostMapping.Select(m => ToSql(m.GetValue(cost))).Implode())
						.Append(");");
				}
				else {
					command.Append(UpdateCostSql(core.ExistsCore, cost, existsCost));
				}
			}

			if (core.ExistsCore.Costs != null) {
				var costsToDelete = core.ExistsCore.Costs
					.Where(c => costs.All(nc => nc.Description.Id != c.Description.Id))
					.Select(c => c.Description.Id.ToString()).ToArray();
				if (costsToDelete.Length > 0)
					command.AppendFormat("delete from farm.CoreCosts where Core_Id = {0} and PC_CostCode in ({1});", core.ExistsCore.Id, String.Join(", ", costsToDelete));
			}

			return command.ToString();
		}

		private static string ToSql(object value)
		{
			if (value is uint)
				return GetNullableValue((uint)value);
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

		public static string GetDecimalValue(decimal value)
		{
			if (value == 0)
				return "null";

			return value.ToString(CultureInfo.InvariantCulture);
		}
	}
}