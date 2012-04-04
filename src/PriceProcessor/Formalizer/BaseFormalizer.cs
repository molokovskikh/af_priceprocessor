using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class BaseFormalizer
	{
		protected PriceFormalizationInfo _priceInfo;
		protected string _fileName;
		protected DataTable _data;

		public bool Downloaded { get; set; }
		public string InputFileName { get; set; }
		public int formCount { get; protected set; }
		public int unformCount { get; protected set; }
		public int zeroCount { get; protected set; }
		public int forbCount { get; protected set; }

		public int maxLockCount
		{
			get { return 0; }
		}

		public long priceCode
		{
			get { return _priceInfo.PriceCode; }
		}

		public long firmCode
		{
			get { return _priceInfo.FirmCode; }
		}

		public string firmShortName
		{
			get { return _priceInfo.FirmShortName; }
		}

		public string priceName
		{
			get { return _priceInfo.PriceName; }
		}

		protected static void UpdateIntersection(MySqlCommand command, PriceCost cost, List<Customer> customers)
		{
			foreach (var customer in customers) {
				command.Parameters.Clear();
				var filterSql = new List<string>();
				if (!String.IsNullOrEmpty(customer.SupplierClientId)) {
					filterSql.Add("i.SupplierClientId = ?supplierClientId");
					command.Parameters.AddWithValue("?supplierClientId", customer.SupplierClientId);
				}

				if (!String.IsNullOrEmpty(customer.SupplierPayerId)) {
					filterSql.Add("i.SupplierPayerId = ?supplierPayerId");
					command.Parameters.AddWithValue("?supplierPayerId", customer.SupplierPayerId);
				}

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
					setSql.Add("i.CostId = ?costId");
					command.Parameters.AddWithValue("?costId", null);
				}

				if (setSql.Count > 0)
				{
					command.CommandText = String.Format(@"
update Customers.Intersection i
set {0}
where {1} and i.PriceId = ?priceId", setSql.Implode(), filterSql.Implode(" and "));
					command.Parameters.AddWithValue("?priceId", cost.Price.Id);
					command.ExecuteNonQuery();
				}

				UpdateAddressIntersection(command, cost, customer, filterSql);
			}
		}

		private static void UpdateAddressIntersection(MySqlCommand command, PriceCost cost, Customer customer, List<string> intersectionFilter)
		{
			foreach (var address in customer.Addresses) {
				command.Parameters.Clear();

				var filterSql = new List<string>();
				if (!String.IsNullOrEmpty(address.SupplierAddressId)) {
					filterSql.Add("ai.SupplierDeliveryId = ?supplierDeliveryId");
					command.Parameters.AddWithValue("supplierDeliveryId", address.SupplierAddressId);
				}

				var setSql = new List<string>();
				if (address.MinReq.HasValue) {
					setSql.Add("ai.MinReq = ?minReq");
					command.Parameters.AddWithValue("minReq", address.MinReq.Value);
				}

				if (address.ControlMinReq.HasValue) {
					setSql.Add("ai.ControlMinReq = ?controlMinReq");
					command.Parameters.AddWithValue("controlMinReq", address.ControlMinReq.Value);
				}

				if (filterSql.Count == 0 && setSql.Count == 0)
					continue;

				command.CommandText = String.Format( @"
update Customers.Intersection i
join Customers.AddressIntersection ai on ai.IntersectionId = i.Id
set {0}
where {1} and {2} and i.PriceId = ?priceId",
					setSql.Implode(),
					filterSql.Implode(" and "),
					intersectionFilter.Implode(" and "));
				command.Parameters.AddWithValue("?priceId", cost.Price.Id);
				command.ExecuteNonQuery();
			}
		}

		protected void FormalizePrice(IReader reader, PriceCost cost)
		{
			var priceInfo = _data.Rows[0];

			var info = new PriceFormalizationInfo(priceInfo);

			info.IsUpdating = true;
			info.CostCode = cost.Id;
			info.PriceItemId = cost.PriceItemId;
			info.PriceCode = cost.Price.Id;

			var parser = new BasePriceParser2(reader, info);
			parser.Downloaded = Downloaded;
			parser.Formalize();
			formCount += parser.Stat.formCount;
			forbCount += parser.Stat.forbCount;
			unformCount += parser.Stat.unformCount;
			zeroCount += parser.Stat.zeroCount;
		}
	}
}