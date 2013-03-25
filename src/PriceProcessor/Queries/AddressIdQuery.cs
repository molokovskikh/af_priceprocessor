using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Common.MySql;
using Inforoom.Downloader.DocumentReaders;
using MySql.Data.MySqlClient;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.PriceProcessor.Queries
{
	public class AddressIdQuery
	{
		public string SupplierDeliveryId;
		public string SupplierClientId;

		private uint supplierId;
		private bool includeClientId;

		public AddressIdQuery(uint supplierId, bool includeClientId = true)
		{
			this.supplierId = supplierId;
			this.includeClientId = includeClientId;
		}

		public List<uint> Query()
		{
			var parametrs = new List<MySqlParameter> {
				new MySqlParameter("?SupplierId", supplierId),
				new MySqlParameter("?SupplierDeliveryId", SupplierDeliveryId)
			};

			if (includeClientId)
				parametrs.Add(new MySqlParameter("?SupplierClientId", SupplierClientId));

			var sql = SqlGetClientAddressId(includeClientId, true);
			var ds = With.Connection(c => MySqlHelper.ExecuteDataset(
				c,
				sql,
				parametrs.ToArray()));

			return ds.Tables[0].AsEnumerable().Select(r => Convert.ToUInt32(r["AddressId"])).ToList();
		}

		public static List<ulong> GetAddressIds(ulong supplierId, string supplierDeliveryId)
		{
			var parametrs = new[] {
				new MySqlParameter("?SupplierId", supplierId),
				new MySqlParameter("?SupplierDeliveryId", supplierDeliveryId)
			};
			var sql = SqlGetClientAddressId(false, true);
			var ds = With.Connection(c => MySqlHelper.ExecuteDataset(
				c,
				sql,
				parametrs));

			return ds.Tables[0].AsEnumerable().Select(r => Convert.ToUInt64(r["AddressId"])).ToList();
		}

		public static string SqlGetClientAddressId(bool filterBySupplierClientId, bool filterBySupplierDeliveryId)
		{
			var sqlSupplierClientId = String.Empty;
			var sqlSupplierDeliveryId = String.Empty;

			if (filterBySupplierClientId)
				sqlSupplierClientId = " FutureInter.SupplierClientId = ?SupplierClientId ";
			if (filterBySupplierDeliveryId)
				sqlSupplierDeliveryId = " AddrInter.SupplierDeliveryId = ?SupplierDeliveryId ";

			var sqlCondition = sqlSupplierClientId;
			if (filterBySupplierClientId && filterBySupplierDeliveryId)
				sqlCondition = String.Format(" {0} AND {1} ", sqlSupplierClientId, sqlSupplierDeliveryId);
			else
				sqlCondition += sqlSupplierDeliveryId;

			var sqlQuery = @"
SELECT
    Addr.Id as AddressId
FROM
	Customers.Addresses Addr
JOIN Customers.AddressIntersection AddrInter ON AddrInter.AddressId = Addr.Id
JOIN usersettings.PricesData pd ON pd.FirmCode = ?SupplierId
JOIN Customers.Intersection FutureInter ON FutureInter.Id = AddrInter.IntersectionId AND FutureInter.PriceId = pd.PriceCode
WHERE"
				+ sqlCondition + Environment.NewLine + GetFilterSQLFooter();
			return sqlQuery;
		}

		private static string GetFilterSQLFooter()
		{
			return @"
GROUP BY AddressId";
		}
	}
}