using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class DbfParser
	{
		private List<Action<DocumentLine, DataRow>> _lineActions = new List<Action<DocumentLine, DataRow>>();
		private List<Action<Document, DataRow>> _headerActions = new List<Action<Document, DataRow>>();
		private List<Action<Invoice, DataRow>> _invoiceActions = new List<Action<Invoice, DataRow>>();

		private static PropertyInfo GetInfo(Expression<Func<DocumentLine, object>> expression)
		{
			if (expression.Body.NodeType == ExpressionType.Convert) {
				var ex = (UnaryExpression)expression.Body;
				return (PropertyInfo)((MemberExpression)ex.Operand).Member;
			}
			if (expression.Body.NodeType == ExpressionType.MemberAccess) {
				return (PropertyInfo)(((MemberExpression)expression.Body).Member);
			}
			throw new Exception("Неизвестный тип выражения");
		}

		private static PropertyInfo GetInfo(Expression<Func<Document, object>> expression)
		{
			if (expression.Body.NodeType == ExpressionType.Convert) {
				var ex = (UnaryExpression)expression.Body;
				return (PropertyInfo)((MemberExpression)ex.Operand).Member;
			}
			if (expression.Body.NodeType == ExpressionType.MemberAccess) {
				return (PropertyInfo)(((MemberExpression)expression.Body).Member);
			}
			throw new Exception("Неизвестный тип выражения");
		}

		private static PropertyInfo GetInfo(Expression<Func<Invoice, object>> expression)
		{
			if (expression.Body.NodeType == ExpressionType.Convert) {
				var ex = (UnaryExpression)expression.Body;
				return (PropertyInfo)((MemberExpression)ex.Operand).Member;
			}
			if (expression.Body.NodeType == ExpressionType.MemberAccess) {
				return (PropertyInfo)(((MemberExpression)expression.Body).Member);
			}
			throw new Exception("Неизвестный тип выражения");
		}


		// Если fieldName - это поле, из которого будет читаться ProviderDocumentId,
		// то этот метод делает проверку на то, что в поле ProviderDocumentId содержатся не порядковые номера
		// Если там НЕ порядковые номера, вернется true, иначе false
		private bool CheckProviderDocumentId(PropertyInfo propertyInfo, DataRow row, string fieldName)
		{
			// Если это свойство - не ProviderDocumentId, то говорим что все хорошо
			if (!(propertyInfo.PropertyType == typeof(string) && propertyInfo.Name.Equals("ProviderDocumentId", StringComparison.InvariantCultureIgnoreCase)))
				return true;

			var rows = row.Table.Rows;
			var value = row[fieldName];
			Decimal intValue = 0;

			// Если не удалось преобразовать к числу, значит это не порядковый номер, выходим
			if (!Decimal.TryParse(value.ToString(), out intValue))
				return true;

			// Если кол-во строк больше 1 и значение во 2-ой строке НЕ совпадает с значением в первой строке
			// значит считаем что в поле ProviderDocumentId содержатся порядковые номера. Выходим
			if ((rows.Count > 1) && !rows[rows.IndexOf(row) + 1][fieldName].Equals(value))
				return false;

			// Если всего одна строка, и значение поля ProviderDocumentId в ней равен 1, то считаем что это порядковый номер
			if (rows.Count.Equals(1) && intValue.Equals(1))
				return false;

			// Если дошли сюда, то предполагаем что во всех строках ProviderDocumentId одинаков и НЕ содержит порядковые номера строк
			return true;
		}

		public DbfParser DocumentHeader(Expression<Func<Document, object>> ex, params string[] names)
		{
			var propertyInfo = GetInfo(ex);
			_headerActions.Add((line, dataRow) => {
				var found = false;
				foreach (var name in names) {
					if (!dataRow.Table.Columns.Contains(name))
						continue;
					var value = dataRow[name];
					if (!CheckProviderDocumentId(propertyInfo, dataRow, name))
						continue;
					propertyInfo.SetValue(line, ConvertIfNeeded(value, propertyInfo.PropertyType), new object[0]);
					found = true;
					break;
				}
				if (!found) {
					if (propertyInfo.PropertyType == typeof(string) &&
						propertyInfo.Name.Equals("ProviderDocumentId", StringComparison.InvariantCultureIgnoreCase))
						propertyInfo.SetValue(line, Document.GenerateProviderDocumentId(), new object[0]);
					else if ((propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(DateTime?))
						&& propertyInfo.Name.Equals("DocumentDate", StringComparison.InvariantCultureIgnoreCase))
						propertyInfo.SetValue(line, DateTime.Now, new object[0]);
				}
			});
			return this;
		}

		/// <summary>
		/// Выбирает данные из столбца, в котором значения не null
		/// </summary>
		public DbfParser DocumentInvoiceIfNull(Expression<Func<Invoice, object>> ex, params string[] names)
		{
			var propertyInfo = GetInfo(ex);
			_invoiceActions.Add((line, dataRow) => {
				foreach (var name in names) {
					if (!dataRow.Table.Columns.Contains(name))
						continue;
					var value = dataRow[name];
					if(value == DBNull.Value)
						continue;
					propertyInfo.SetValue(line, ConvertIfNeeded(value, propertyInfo.PropertyType), new object[0]);
					break;
				}
			});
			return this;
		}

		public DbfParser DocumentInvoice(Expression<Func<Invoice, object>> ex, params string[] names)
		{
			var propertyInfo = GetInfo(ex);
			_invoiceActions.Add((line, dataRow) => {
				foreach (var name in names) {
					if (!dataRow.Table.Columns.Contains(name))
						continue;
					var value = dataRow[name];
					propertyInfo.SetValue(line, ConvertIfNeeded(value, propertyInfo.PropertyType), new object[0]);
					break;
				}
			});
			return this;
		}

		public DbfParser DocumentInvoice(Expression<Func<DocumentLine, bool>> expression)
		{
			if (expression.Body.NodeType == ExpressionType.Call) {
				var ex = (MethodCallExpression)expression.Body;
				var methodInfo = ex.Method;
				var argumentType = methodInfo.GetParameters()[0].ParameterType;
				PropertyInfo argument = null;
				foreach (var arg in ex.Arguments) {
					var op = ((UnaryExpression)((UnaryExpression)arg).Operand).Operand;
					argument = ((PropertyInfo)((MemberExpression)op).Member);
					break;
				}
				if (argument != null)
					_invoiceActions.Add((line, dataRow) =>
						methodInfo.Invoke(line, new[] { ConvertIfNeeded(argument.GetValue(line, new object[] { }), argumentType) }));
			}
			return this;
		}


		public DbfParser Line(Expression<Func<DocumentLine, object>> ex, params string[] names)
		{
			var propertyInfo = GetInfo(ex);
			_lineActions.Add((line, dataRow) => {
				foreach (var name in names) {
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
			if (expression.Body.NodeType == ExpressionType.Call) {
				var ex = (MethodCallExpression)expression.Body;
				var methodInfo = ex.Method;
				var argumentType = methodInfo.GetParameters()[0].ParameterType;
				PropertyInfo argument = null;
				foreach (var arg in ex.Arguments) {
					var op = ((UnaryExpression)((UnaryExpression)arg).Operand).Operand;
					argument = ((PropertyInfo)((MemberExpression)op).Member);
					break;
				}
				if (argument != null)
					_lineActions.Add((line, dataRow) =>
						methodInfo.Invoke(line, new[] { ConvertIfNeeded(argument.GetValue(line, new object[] { }), argumentType) }));
			}
			return this;
		}

		protected static object ConvertIfNeeded(object value, Type type)
		{
			if (Convert.IsDBNull(value))
				return null;
			if (type == typeof(uint) || type == typeof(uint?)) {
				if (value is String)
					return ParseHelper.GetUInt((String)value);
				return Convert.ToUInt32(value);
			}
			if (type == typeof(int) || type == typeof(int?)) {
				if (value is String)
					return ParseHelper.GetInt((String)value);
				return Convert.ToInt32(value);
			}
			if (type == typeof(decimal) || type == typeof(decimal?)) {
				if (value is String)
					return ParseHelper.GetDecimal((String)value);
				return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
			}
			if (type == typeof(string)) {
				DateTime res;
				if (DateTime.TryParseExact(value.ToString(),
					"dd.MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out res))
					return Convert.ToString(value);
				if (DateTime.TryParse(value.ToString(), out res))
					return Convert.ToDateTime(value).ToShortDateString();
				return Convert.ToString(value);
			}
			if (type == typeof(DateTime) || type == typeof(DateTime?)) {
				DateTime res;
				if (DateTime.TryParse(value.ToString(), out res))
					return Convert.ToDateTime(value);
				if (DateTime.TryParseExact(value.ToString(),
					"yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out res))
					return res;
				return Convert.ToDateTime(value);
			}
			if (type == typeof(bool) || type == typeof(bool?)) {
				bool res;
				if (Boolean.TryParse(value.ToString(), out res))
					return res;
				var lowerValue = value.ToString().ToLower();
				if (lowerValue == "нет")
					return false;
				if (lowerValue == "да")
					return true;
				return Convert.ToBoolean(Convert.ToInt32(value));
			}
			throw new Exception("Преобразование для этого типа не реализовано");
		}

		public void ToDocument(Document document, DataTable table)
		{
			if (table.Rows.Count == 0)
				return;
			foreach (var action in _headerActions)
				action(document, table.Rows[0]);

			if (_invoiceActions.Count != 0) {
				var invoice = document.SetInvoice();
				foreach (var action in _invoiceActions) {
					action(invoice, table.Rows[0]);
				}
			}

			foreach (var row in table.Rows.Cast<DataRow>()) {
				var line = document.NewLine();
				foreach (var action in _lineActions)
					action(line, row);
			}
		}
	}
}