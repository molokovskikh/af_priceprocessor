using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
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
			if (document == null || document.Lines == null) return;
			using (new SessionScope()) {
				if (orders != null) { // заказы переданы отдельно и не связаны с позициями в накладной
					var waybillPositions = document.Lines.Where(l => l != null && !String.IsNullOrEmpty(l.Code)).ToList();
					while (waybillPositions.Count > 0) {
						var line = waybillPositions.First();
						var code = line.Code.Trim().ToLower();
						var waybillLines = waybillPositions.Where(l => l.Code.Trim().ToLower() == code).ToList();
						waybillLines.ForEach(waybillLine => waybillPositions.Remove(waybillLine));
						foreach (var itemW in waybillLines) {
							foreach (var order in orders) {
								var orderLines = order.Items.Where(i => i != null && !String.IsNullOrEmpty(i.Code) && i.Code.Trim().ToLower() == code).ToList();
								orderLines.ForEach(itemOrd => AddToAssociativeTable(itemW.Id, itemOrd.Id));
							}
						}
					}
				}
				else {
					var waybillPositions = document.Lines.Where(l => l != null && l.OrderId != null && !String.IsNullOrEmpty(l.Code)).ToList();
					foreach(var line in waybillPositions) { // номер заказа выставлен для каждой позиции в накладной
						var code = line.Code.Trim().ToLower();
						var order = OrderHead.TryFind(line.OrderId);
						if (order == null) continue;
						var orderLines = order.Items.Where(i => i.Code.Trim().ToLower() == code).ToList();
						orderLines.ForEach(itemOrd => AddToAssociativeTable(line.Id, itemOrd.Id));
					}
				}
			}
		}

		public static void SafeComparisonWithOrders(Document document, IList<OrderHead> orders)
		{
			try {
				ComparisonWithOrders(document,orders);
			}
			catch (Exception e) {
				_log.Error(String.Format("Ошибка при сопоставлении заказов накладной {0}", document.Id), e);
			}
		}

		private static void AddToAssociativeTable(uint docLineId, uint ordLineId)
		{
			With.Connection(c => {
				var command = new MySqlCommand(@"
insert into documents.waybillorders(DocumentLineId, OrderLineId)
values(?DocumentLineId, ?OrderLineId);
", c);
				command.Parameters.AddWithValue("?DocumentLineId", docLineId);
				command.Parameters.AddWithValue("?OrderLineId", ordLineId);
				command.ExecuteNonQuery();
			});
		}
	}
}