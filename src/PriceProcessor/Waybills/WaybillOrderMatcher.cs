using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Waybills.Models;
using MySql.Data.MySqlClient;
using log4net;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillOrderMatcher
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(WaybillOrderMatcher));

		/// <summary>
		/// Не обрабатывает исключения, используй SafeComparisonWithOrders
		/// </summary>
		public static void ComparisonWithOrders(Document document, IList<OrderHead> orders)
		{
			if (document == null)
				return;

			var documentLines = document.Lines;
			foreach (var lineGroup in documentLines.GroupBy(d => d.OrderId)) {
				var currentOrders = orders;
				if (lineGroup.Key != null)
					currentOrders = orders.Where(o => o.Id == lineGroup.Key.Value).ToList();

				ComparisonWithOrders(lineGroup.ToList(), currentOrders);
			}
		}

		private static void ComparisonWithOrders(IList<DocumentLine> documentLines, IList<OrderHead> orders)
		{
			var orderItems = orders.SelectMany(o => o.Items);
			var codeLookup = orderItems
				.Where(o => !String.IsNullOrEmpty(o.Code))
				.ToLookup(o => o.Code.Trim().ToLower());

			var synonymLookup = orderItems.Where(o => o.ProductSynonym != null)
				.ToLookup(o => GetLookupKey(
					o.ProductSynonym.Synonym,
					o.ProducerSynonym != null ? o.ProducerSynonym.Synonym : ""));

			foreach (var documentLine in documentLines) {
				var items = Enumerable.Empty<OrderItem>();

				if (!String.IsNullOrEmpty(documentLine.Code)) {
					var key = documentLine.Code.Trim().ToLower();
					items = codeLookup[key];
				}

				if (!items.Any() && !String.IsNullOrEmpty(documentLine.Product)) {
					var synonymKey = GetLookupKey(documentLine.Product, documentLine.Producer);
					items = synonymLookup[synonymKey];
				}

				items.Each(i => documentLine.OrderItems.Add(i));
			}
		}

		public static string GetLookupKey(params string[] part)
		{
			var reg = new Regex(@"\s");
			var items = part
				.Where(p => !String.IsNullOrEmpty(p))
				.Select(p => p.Trim().ToLower())
				.Select(p => reg.Replace(p, ""));
			return String.Join("", items);
		}

		public static void SafeComparisonWithOrders(Document document, IList<OrderHead> orders)
		{
			try {
				ComparisonWithOrders(document, orders);
			}
			catch (Exception e) {
				_log.Error(String.Format("Ошибка при сопоставлении заказов накладной {0}", document.Id), e);
			}
		}
	}
}