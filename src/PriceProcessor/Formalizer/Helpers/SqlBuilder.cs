using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.MySql;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;

namespace Inforoom.PriceProcessor.Formalizer.Helpers
{
	public class FieldMap
	{
		private FieldInfo _coreField;
		private FieldInfo _newCoreField;

		public FieldMap(FieldInfo info)
		{
			Name = info.Name;
			_coreField = info;
			_newCoreField = typeof(NewCore).GetFields().Single(f => f.Name == Name);
		}

		public string Name { get; private set; }

		public bool Equal(NewCore core)
		{
			return Equals(_coreField.GetValue(core.ExistsCore), GetValue(core));
		}

		public object GetValue(NewCore core)
		{
			var value = _newCoreField.GetValue(core);
			if (value == null)
				return "";
			return value;
		}

		public void SetValue(object value, Core.Core core)
		{
			_coreField.SetValue(core, value);
		}

		public override string ToString()
		{
			return Name;
		}

		private static FieldMap[] GetCoreFieldMaps()
		{
			return typeof(Core.Core).GetFields().Where(f => f.Name != "Costs").Select(f => new FieldMap(f)).ToArray();
		}

		private static FieldMap[] _fieldMaps;

		public static FieldMap[] CoreFieldMaps
		{
			get
			{
				if (_fieldMaps == null)
					_fieldMaps = GetCoreFieldMaps();
				return _fieldMaps;
			}
		}

		public Type Type
		{
			get { return _coreField.FieldType; }
		}
	}

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

		public static string InsertCoreCommand(PriceFormalizationInfo info, NewCore core)
		{
			var command = new StringBuilder();
			command.Append("insert into farm.Core0(PriceCode,")
				.Append(FieldMap.CoreFieldMaps.Select(m => m.Name).Implode())
				.Append(") values (")
				.Append(info.PriceCode + ",")
				.Append(FieldMap.CoreFieldMaps.Select(m => ToSql(m.GetValue(core))).Implode())
				.Append(");")
				.Append("set @LastCoreID = last_insert_id();");
			return command.ToString();
		}

		public static string UpdateCoreCommand(NewCore core)
		{
			var fields = FieldMap.CoreFieldMaps.Where(m => !m.Equal(core)).Select(m => String.Format("{0} = {1}", m.Name, ToSql(m.GetValue(core)))).Implode();
			if (fields.Length == 0)
				return null;

			return String.Format("update farm.Core0 set {0} where Id = {1};\r\n", fields, core.ExistsCore.Id);
		}

		public static string InsertCostsCommand(NewCore core)
		{
			var command = new StringBuilder()
				.AppendLine("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost, RequestRatio, MinOrderSum, MinOrderCount) values ")
				.Append(core.Costs.Where(c => c.Value > 0).Select(c => String.Format("(@LastCoreID, {0}, {1}, {2}, {3}, {4})",
					c.Description.Id,
					ToSql(c.Value),
					ToSql(c.RequestRatio),
					ToSql(c.MinOrderSum),
					ToSql(c.MinOrderCount))).Implode())
				.AppendLine(";");
			return command.ToString();
		}

		public static string UpdateCostsCommand(NewCore core)
		{
			var command = new StringBuilder();
			if(core.Costs != null)
				foreach (var cost in core.Costs) {
					Cost existsCost = null;
					if (core.ExistsCore.Costs != null)
						existsCost = core.ExistsCore.Costs.FirstOrDefault(c => c.Description.Id == cost.Description.Id);
					if (existsCost == null) {
						command.AppendFormat("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost, RequestRatio, MinOrderSum, MinOrderCount) values ({0}, {1}, {2}, {3}, {4}, {5});",
							core.ExistsCore.Id,
							cost.Description.Id,
							ToSql(cost.Value),
							ToSql(cost.RequestRatio),
							ToSql(cost.MinOrderSum),
							ToSql(cost.MinOrderCount));
					}
					else if (cost.Value != existsCost.Value) {
						command.AppendFormat("update farm.CoreCosts set Cost = {2}, RequestRatio = {3}, MinOrderSum = {4}, MinOrderCount = {5} where Core_Id = {0} and PC_CostCode = {1};",
							core.ExistsCore.Id,
							cost.Description.Id,
							ToSql(cost.Value),
							ToSql(cost.RequestRatio),
							ToSql(cost.MinOrderSum),
							ToSql(cost.MinOrderCount));
					}
				}
			if (core.ExistsCore.Costs != null) {
				var costsToDelete = core.ExistsCore.Costs
					.Where(c => !core.Costs.Any(nc => nc.Description.Id == c.Description.Id))
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
			return "'" + StringToMySqlString(value) + "'";
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