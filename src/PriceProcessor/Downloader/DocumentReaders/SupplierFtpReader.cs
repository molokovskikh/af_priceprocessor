using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.Downloader.DocumentReaders;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Downloader.DocumentReaders
{
	public class SupplierFtpReader : BaseDocumentReader
	{
		public SupplierFtpReader()
		{
			excludeExtentions = new string[] { };
		}

		public override List<ulong> GetClientCodes(MySqlConnection connection, ulong supplierId, string archFileName, string currentFileName)
		{
			var sql = SqlGetClientAddressId(false, true) +
				Environment.NewLine + GetFilterSQLFooter();

			string supplierDeliveryId;
			try {
				supplierDeliveryId = Path.GetFileName(currentFileName).Split('_')[0];
			}
			catch (Exception ex) {
				throw new Exception("Не получилось сформировать код доставки из имени файла накладной.", ex);
			}

			var ds = MySqlHelper.ExecuteDataset(
				connection,
				sql,
				new MySqlParameter("?SupplierId", supplierId),
				new MySqlParameter("?SupplierDeliveryId", supplierDeliveryId));

			return ds.Tables[0].AsEnumerable().Select(r => Convert.ToUInt64(r["AddressId"])).ToList();
		}
	}
}