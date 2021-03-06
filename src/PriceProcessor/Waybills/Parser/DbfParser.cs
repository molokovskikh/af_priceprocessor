﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;
using NPOI.HSSF.Model;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class DbfParser
	{
		private static Regex spaceReg = new Regex(@"\s");
		private List<Action<DocumentLine, DataRow>> _lineActions = new List<Action<DocumentLine, DataRow>>();
		private List<Action<Document, DataRow>> _headerActions = new List<Action<Document, DataRow>>();
		private List<Action<Invoice, DataRow>> _invoiceActions = new List<Action<Invoice, DataRow>>();

		public Dictionary<PropertyInfo, string[]> LineMap = new Dictionary<PropertyInfo, string[]>();

		private static PropertyInfo GetInfo<T>(Expression<Func<T, object>> expression)
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

		private PropertyInfo CollectAction<T>(Expression<Func<T, object>> ex, string[] names, List<Action<T, DataRow>> actions)
		{
			var propertyInfo = GetInfo(ex);
			actions.Add((line, dataRow) => {
				foreach (var name in names) {
					if (!dataRow.Table.Columns.Contains(name))
						continue;
					var value = dataRow[name];
					if (value is DBNull)
						continue;


					object newvalue;
					try {
						newvalue = ConvertIfNeeded(value, propertyInfo.PropertyType);
					}
					catch (OverflowException e) {
						//Логируем ошибку переполнения поля
						var typename = propertyInfo.PropertyType.FullName;
						var docline = line as DocumentLine;
						var invoice = line as Invoice;
						var clientcode = docline != null ? docline.Document.ClientCode.ToString() : invoice.Document.ClientCode.ToString();
						var err = string.Format("Клиент: {2} Ошибка переполнения типа {0} для значения {1}", typename, value, clientcode);
						var logger = log4net.LogManager.GetLogger(GetType());
						logger.Warn(err, e);

						//Если тип данных Nullable, то мы считаем, что значение по умолчанию будет null
						if (propertyInfo.PropertyType.IsNullable())
							newvalue = null;
						else
							throw;
					}
					propertyInfo.SetValue(line, newvalue, new object[0]);

					break;
				}
			});
			return propertyInfo;
		}

		public DbfParser Invoice(Expression<Func<Invoice, object>> ex, params string[] names)
		{
			CollectAction(ex, names, _invoiceActions);
			return this;
		}

		public DbfParser Line(Expression<Func<DocumentLine, object>> ex, params string[] names)
		{
			var property = CollectAction(ex, names, _lineActions);
			try {
				LineMap.Add(property, names);
			}
			catch(ArgumentException e) {
				throw new ArgumentException(String.Format("Ключи {0}", property), e);
			}
			return this;
		}

		public static object ConvertIfNeeded(object value, Type type)
		{
			if (Convert.IsDBNull(value))
				return null;

			if (type == typeof(uint) || type == typeof(uint?))
				return ParseHelper.GetUInt(value.ToString());

			if (type == typeof(ulong?)) {
				return NullableConvert.ToUInt64(spaceReg.Replace(value.ToString(), ""));
			}

			if (type == typeof(int) || type == typeof(int?))
				return ParseHelper.GetInt(value.ToString());

			if (type == typeof(decimal) || type == typeof(decimal?))
				return ParseHelper.GetDecimal(value.ToString());

			if (type == typeof(string)) {
				DateTime res;
				var dtFormats = new string[] { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy H:mm:ss", "dd.MM.yy", "dd/MM/yy" };
				if (DateTime.TryParseExact(value.ToString(),
					dtFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out res))
					return res.ToShortDateString();
				return Convert.ToString(value);
			}

			if (type == typeof(DateTime) || type == typeof(DateTime?)) {
				DateTime res;
				if (DateTime.TryParse(value.ToString(), out res))
					return Convert.ToDateTime(value);
				if (DateTime.TryParseExact(value.ToString(),
					"yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out res))
					return res;
				if (type == typeof(DateTime))
					return DateTime.Now;
				return null;
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
				return ParseHelper.GetBoolean(value.ToString());
			}
			throw new Exception($"Преобразование для {type} не реализовано");
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