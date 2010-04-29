using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;

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
			_newCoreField = typeof (NewCore).GetFields().Where(f => f.Name == Name).Single();
		}

		public string Name { get; private set;}

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

		public override string ToString()
		{
			return Name;
		}
	}

	public class SqlBuilder
	{
		private static FieldMap[] _fieldMaps;

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
							"MaxBoundCost, OrderCost, MinOrderCount, ProducerCost, Nds, " +
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
			sb.AppendFormat("{0}, ", (drCore["ProducerCost"] is DBNull) ? "null" : Convert.ToDecimal(drCore["ProducerCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
			sb.AppendFormat("{0}, ", (drCore["Nds"] is DBNull) ? "null" : Convert.ToUInt32(drCore["Nds"]).ToString(CultureInfo.InvariantCulture.NumberFormat));
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

		public static void InsertCoreCosts(StringBuilder builder, List<CoreCost> coreCosts)
		{
			builder.AppendLine("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost) values ");
			var firstInsert = true;
			foreach (var cost in coreCosts)
			{
				if (cost.cost.HasValue && cost.cost > 0)
				{
					if (!firstInsert)
						builder.Append(", ");
					firstInsert = false;
					builder.AppendFormat("(@LastCoreID, {0}, {1}) ", cost.costCode, (cost.cost.HasValue && (cost.cost > 0)) ? cost.cost.Value.ToString(CultureInfo.InvariantCulture.NumberFormat) : "null");
				}
			}
			builder.AppendLine(";");
		}

		public static string InsertCoreCommand(PriceFormalizationInfo info, NewCore core)
		{
			var command = new StringBuilder();
			command.Append("insert into farm.Core0("
				+ "PriceCode,"
				+ "ProductId,"
				+ "CodeFirmCr,"
				+ "SynonymCode,"
				+ "SynonymFirmCrCode,"
				+ "Period, Junk, Await, MinBoundCost, VitallyImportant, RequestRatio,"
				+ "RegistryCost, MaxBoundCost, OrderCost, MinOrderCount, "
				+ "Code,"
				+ "CodeCr,"
				+ "Unit,"
				+ "Volume,"
				+ "Quantity,"
				+ "Note,"
				+ "Doc"
				+") values (");
			var invariantCulture = CultureInfo.InvariantCulture;
			command
				.Append(info.PriceCode + ",")
				.Append(core.ProductId.ToString(invariantCulture) + ",")
				.Append(GetNullableValue(core.CodeFirmCr) + ",")
				.Append(core.SynonymCode.ToString(invariantCulture) + ",")
				.Append(GetNullableValue(core.SynonymFirmCrCode) + ",")
				.Append(GetStringValue(core.Period) + ",")
				.Append(GetBoolValue(core.Junk) + ",")
				.Append(GetBoolValue(core.Await) + ",")
				.Append(GetDecimalValue(core.MinBoundCost) + ",")
				.Append(GetBoolValue(core.VitallyImportant) + ",")
				.Append(GetNullableValue(core.RequestRatio) + ",")
				.Append(GetDecimalValue(core.RegistryCost) + ",")
				.Append(GetDecimalValue(core.MaxBoundCost) + ",")
				.Append(GetDecimalValue(core.OrderCost) + ",")
				.Append(GetNullableValue(core.MinOrderCount) + ",")
				.Append(GetStringValue(core.Code) + ",")
				.Append(GetStringValue(core.CodeCr) + ",")
				.Append(GetStringValue(core.Unit) + ",")
				.Append(GetStringValue(core.Volume) + ",")
				.Append(GetStringValue(core.Quantity) + ",")
				.Append(GetStringValue(core.Note) + ",")
				.Append(GetStringValue(core.Doc))
				.Append(");")
				.Append("set @LastCoreID = last_insert_id();");
			Console.WriteLine(command);
			return command.ToString();
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

		public static string InsertCostsCommand(NewCore core)
		{
			var command = new StringBuilder()
				.AppendLine("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost) values ")
				.Append(String.Join(",", core.Costs.Where(c => c.Value > 0).Select(c => String.Format("(@LastCoreID, {0}, {1})", c.Description.Id, c.Value.ToString(CultureInfo.InvariantCulture))).ToArray()))
				.AppendLine(";");
			return command.ToString();
		}

		public static string UpdateCoreCommand(NewCore core)
		{
			if (_fieldMaps == null)
				_fieldMaps = InitFieldMap();

			var fields = String.Join(", ", _fieldMaps.Where(m => !m.Equal(core)).Select(m => String.Format("{0} = {1}", m.Name, ToSql(m.GetValue(core)))).ToArray());
			if (fields.Length == 0)
				return null;

			return String.Format("update farm.Core0 set {0} where Id = {1};\r\n", fields, core.ExistsCore.Id);
		}

		private static string ToSql(object value)
		{
			if (value is uint)
				return GetNullableValue((uint) value);
			if (value is bool)
				return GetBoolValue((bool) value);
			if (value is decimal)
				return GetDecimalValue((decimal) value);
			if (value is string)
				return GetStringValue((string) value);
			throw new Exception(String.Format("Не знаю как преобразовать {0}", value));
		}

		private static FieldMap[] InitFieldMap()
		{
			return typeof (Core).GetFields().Where(f => f.Name != "Costs").Select(f => new FieldMap(f)).ToArray();
		}

		public static string UpdateCostsCommand(NewCore core)
		{
			var command = new StringBuilder();
			foreach(var cost in core.Costs)
			{
				var existsCost = core.ExistsCore.Costs.FirstOrDefault(c => c.Description.Id == cost.Description.Id);
				if (existsCost == null)
				{
					command.AppendFormat("insert into farm.CoreCosts (Core_ID, PC_CostCode, Cost) values ({0}, {1}, {2});",
						core.ExistsCore.Id, cost.Description.Id, GetDecimalValue(cost.Value));
				}
				else if (cost.Value != existsCost.Value)
				{
					command.AppendFormat("update farm.CoreCosts set Cost = {2} where Core_Id = {0} and PC_CostCode = {1};", 
						core.ExistsCore.Id, cost.Description.Id, GetDecimalValue(cost.Value));
				}
			}
			var costsToDelete = core.ExistsCore.Costs
				.Where(c => !core.Costs.Any(nc => nc.Description.Id == c.Description.Id))
				.Select(c => c.Description.Id.ToString()).ToArray();
			if (costsToDelete.Length > 0)
				command.AppendFormat("delete from farm.CoreCosts where Core_Id = {0} and PC_CostCode in ({1});", core.ExistsCore.Id, String.Join(", ", costsToDelete));

			return command.ToString();
		}
	}
}