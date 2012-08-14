using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.IO;
using System.Data;

namespace Inforoom.Downloader.DocumentReaders
{
	public class ProtekOmsk_3777_Reader : BaseDocumentReader
	{
		public override List<ulong> GetClientCodes(MySqlConnection connection, ulong supplierId, string archFileName, string currentFileName)
		{
			var list = new List<ulong>();
			
			var sql = SqlGetClientAddressId(false, true) +
				Environment.NewLine + GetFilterSQLFooter();

			string firmClientCode;
			try
			{
				firmClientCode = Path.GetFileName(currentFileName).Split('_')[0];
			}
			catch(Exception ex)
			{
				throw new Exception("Не получилось сформировать SupplierDeliveryId(FirmClientCode2) из имени накладной.", ex);
			}

			var ds = MySqlHelper.ExecuteDataset(
				connection,
				sql,
				new MySqlParameter("?SupplierId", supplierId),
				new MySqlParameter("?SupplierDeliveryId", firmClientCode));

			ds.Tables[0].AsEnumerable().Select(r => Convert.ToUInt64(r["AddressId"]));
			if (list.Count == 0)
				throw new Exception("Не удалось найти клиентов с SupplierClientId(FirmClientCode) = " + firmClientCode + ".");

			return list;
		}
	}
}
