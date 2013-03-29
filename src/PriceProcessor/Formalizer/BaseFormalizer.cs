using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class BaseFormalizer
	{
		protected string _fileName;
		protected DataTable _data;

		public BaseFormalizer(string filename, PriceFormalizationInfo data)
		{
			Info = data;
			_fileName = filename;
			_data = data.FormRulesData;
		}

		public bool Downloaded { get; set; }
		public string InputFileName { get; set; }

		public PriceLoggingStat Stat { get; protected set; }
		public PriceFormalizationInfo Info { get; protected set; }

		protected void UpdateIntersection(MySqlCommand command, List<Customer> customers, List<CostDescription> costs)
		{
			foreach (var customer in customers) {
				command.Parameters.Clear();
				var filterSql = new List<string>();
				AppendFilter(command, filterSql, customer);

				if (filterSql.Count == 0)
					continue;

				var setSql = new List<string>();
				if (customer.Available.HasValue) {
					setSql.Add("i.AvailableForClient = ?AvailableForClient");
					command.Parameters.AddWithValue("AvailableForClient", customer.Available.Value);
				}

				if (customer.PriceMarkup.HasValue) {
					setSql.Add("i.PriceMarkup = ?priceMarkup");
					command.Parameters.AddWithValue("?priceMarkup", customer.PriceMarkup.Value);
				}

				if (!String.IsNullOrEmpty(customer.CostId)) {
					var cost = costs.FirstOrDefault(c => c.Name.Match(customer.CostId));
					if (cost != null && cost.Id > 0) {
						setSql.Add("i.CostId = ?costId");
						command.Parameters.AddWithValue("?costId", cost.Id);
					}
				}

				if (setSql.Count > 0) {
					command.CommandText = String.Format(@"
drop temporary table if exists for_update;
create temporary table for_update engine=memory
select ClientId, RegionId
from Customers.Intersection i
where {1} and i.PriceId = ?priceId;

update Customers.Intersection i
join for_update u on u.ClientId = i.ClientId and u.RegionId = i.RegionId
set {0}
where i.PriceId = ?priceId;

drop temporary table if exists for_update;",
						setSql.Implode(), filterSql.Implode(" and "));
					command.Parameters.AddWithValue("?priceId", Info.PriceCode);
					command.ExecuteNonQuery();
				}

				UpdateAddressIntersection(command, customer, filterSql);
			}
		}

		private void UpdateAddressIntersection(MySqlCommand command, Customer customer, List<string> intersectionFilter)
		{
			foreach (var address in customer.Addresses) {
				command.Parameters.Clear();

				var filterSql = new List<string>();
				if (!String.IsNullOrEmpty(address.SupplierAddressId)) {
					filterSql.Add("ai.SupplierDeliveryId = ?supplierDeliveryId");
					command.Parameters.AddWithValue("supplierDeliveryId", address.SupplierAddressId);
				}
				AppendFilter(command, filterSql, customer);

				if (filterSql.Count == 0)
					continue;

				var setSql = new List<string>();
				if (address.MinReq.HasValue) {
					setSql.Add("ai.MinReq = ?minReq");
					command.Parameters.AddWithValue("minReq", address.MinReq.Value);
				}

				if (address.ControlMinReq.HasValue) {
					setSql.Add("ai.ControlMinReq = ?controlMinReq");
					command.Parameters.AddWithValue("controlMinReq", address.ControlMinReq.Value);
				}

				if (setSql.Count == 0)
					continue;

				command.CommandText = String.Format(@"
update Customers.Intersection i
join Customers.AddressIntersection ai on ai.IntersectionId = i.Id
set {0}
where {1} and {2} and i.PriceId = ?priceId",
					setSql.Implode(),
					filterSql.Implode(" and "),
					intersectionFilter.Implode(" and "));
				command.Parameters.AddWithValue("?priceId", Info.PriceCode);
				command.ExecuteNonQuery();
			}
		}

		private static void AppendFilter(MySqlCommand command, List<string> filterSql, Customer customer)
		{
			if (!String.IsNullOrEmpty(customer.SupplierClientId)) {
				filterSql.Add("i.SupplierClientId = ?supplierClientId");
				command.Parameters.AddWithValue("?supplierClientId", customer.SupplierClientId);
			}

			if (!String.IsNullOrEmpty(customer.SupplierPaymentId)) {
				filterSql.Add("i.SupplierPaymentId = ?supplierPaymentId");
				command.Parameters.AddWithValue("?supplierPaymentId", customer.SupplierPaymentId);
			}
		}

		protected void FormalizePrice(IReader reader)
		{
			var parser = new BasePriceParser(reader, Info);
			parser.Downloaded = Downloaded;
			parser.Formalize();
			Stat = parser.Stat;
		}
	}
}