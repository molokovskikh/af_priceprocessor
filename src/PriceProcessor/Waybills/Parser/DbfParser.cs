using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class DbfParser
	{
		private List<Action<DocumentLine, DataRow>> _lineActions = new List<Action<DocumentLine, DataRow>>();
		private List<Action<Document, DataRow>> _headerActions = new List<Action<Document, DataRow>>();

		private static PropertyInfo GetInfo(Expression<Func<DocumentLine, object>> expression)
		{
			if (expression.Body.NodeType == ExpressionType.Convert)
			{
				var ex = (UnaryExpression) expression.Body;
				return (PropertyInfo)((MemberExpression) ex.Operand).Member;
			}
			if (expression.Body.NodeType == ExpressionType.MemberAccess)
			{
				return (PropertyInfo) (((MemberExpression) expression.Body).Member);
			}
			throw new Exception("Неизвестный тип выражения");
		}

		private static PropertyInfo GetInfo(Expression<Func<Document, object>> expression)
		{
			if (expression.Body.NodeType == ExpressionType.Convert)
			{
				var ex = (UnaryExpression)expression.Body;
				return (PropertyInfo)((MemberExpression)ex.Operand).Member;
			}
			if (expression.Body.NodeType == ExpressionType.MemberAccess)
			{
				return (PropertyInfo)(((MemberExpression)expression.Body).Member);
			}
			throw new Exception("Неизвестный тип выражения");
		}

		public DbfParser DocumentHeader(Expression<Func<Document, object>> ex, string name)
		{
			if (String.IsNullOrEmpty(name))
				return this;
			var propertyInfo = GetInfo(ex);
			_headerActions.Add((line, dataRow) => {
				if (dataRow.Table.Columns.Contains(name))
				{
					var value = dataRow[name];
					propertyInfo.SetValue(line, ConvertIfNeeded(value, propertyInfo.PropertyType), new object[0]);
				}
			});
			return this;
		}

		public DbfParser Line(Expression<Func<DocumentLine, object>> ex, params string[] names)
		{
			var propertyInfo = GetInfo(ex);
			_lineActions.Add((line, dataRow) => {
				foreach (var name in names)
				{
					if (!dataRow.Table.Columns.Contains(name))
						continue;
					var value = dataRow[name];
					propertyInfo.SetValue(line, ConvertIfNeeded(value, propertyInfo.PropertyType), new object[0]);
					break;
				}
			});
			return this;
		}

		public DbfParser Line(Expression<Func<DocumentLine, bool>> expression)
		{
			if (expression.Body.NodeType == ExpressionType.Call)
			{
				var ex = (MethodCallExpression) expression.Body;
				var methodInfo = ex.Method;
				var argumentType = methodInfo.GetParameters()[0].ParameterType;
				PropertyInfo argument = null;
				foreach (var arg in ex.Arguments)
				{
					var op = ((UnaryExpression) ((UnaryExpression)arg).Operand).Operand;
					argument = ((PropertyInfo)((MemberExpression)op).Member);
					break;
				}
				if (argument != null)
					_lineActions.Add((line, dataRow) =>
						methodInfo.Invoke(line, new[] { ConvertIfNeeded(argument.GetValue(line, new object[] { }), argumentType) }));
			}
			return this;
		}

		private static object ConvertIfNeeded(object value, Type type)
		{
			if (Convert.IsDBNull(value))
				return null;
			if (type == typeof(uint) || type == typeof(uint?))
				return Convert.ToUInt32(value);
			if (type == typeof(int) || type == typeof(int?))
				return Convert.ToInt32(value);
			if (type == typeof(decimal) || type == typeof(decimal?))
				return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
			if (type == typeof(string))
			{
				DateTime res;
				if (DateTime.TryParse(value.ToString(), out res))
					return Convert.ToDateTime(value).ToShortDateString();
				return Convert.ToString(value);
			}
			if (type == typeof(DateTime) || type == typeof(DateTime?))
			{
				DateTime res;
				if (DateTime.TryParse(value.ToString(), out res))
					return Convert.ToDateTime(value);
				if (DateTime.TryParseExact(value.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out res))
					return res;
				return Convert.ToDateTime(value);
			}
			if (type == typeof(bool) || type == typeof(bool?))
				return Convert.ToBoolean(value);
			throw new Exception("Преобразование для этого типа не реализовано");
		}

		public void ToDocument(Document document, DataTable table)
		{
			if (table.Rows.Count == 0)
				return;
			foreach (var action in _headerActions)
				action(document, table.Rows[0]);

			foreach (var row in table.Rows.Cast<DataRow>())
			{
				var line = document.NewLine();
				foreach (var action in _lineActions)
					action(line, row);
				line.SetValues();
			}
		}
	}
}
