using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate;
using log4net;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillOrderMatcher
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(WaybillOrderMatcher));

		/// <summary>
		/// Не обрабатывает исключения, используй SafeComparisonWithOrders
		/// </summary>
		public static void ComparisonWithOrders(Document document, IList<OrderItem> orderItems)
		{
			if (document == null)
				return;

			var documentLines = document.Lines;
			foreach (var lineGroup in documentLines.GroupBy(d => d.OrderId)) {
				var currentOrderItems = orderItems;
				if (lineGroup.Key != null)
					currentOrderItems = orderItems.Where(o => o.Order.Id == lineGroup.Key.Value).ToList();

				ComparisonWithOrders(lineGroup.ToList(), currentOrderItems);
			}
		}

		private static void ComparisonWithOrders(IList<DocumentLine> documentLines, IList<OrderItem> orderItems)
		{
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

		public static void SafeComparisonWithOrders(Document document, IList<OrderItem> orderItems)
		{
			try {
				ComparisonWithOrders(document, orderItems);
			}
			catch (Exception e) {
				_log.Error(String.Format("Ошибка при сопоставлении заказов накладной {0}", document.Id), e);
			}
		}
	}
}