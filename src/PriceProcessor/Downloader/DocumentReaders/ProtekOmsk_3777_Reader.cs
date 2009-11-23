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
		public ProtekOmsk_3777_Reader()
		{
			excludeExtentions = new string[] { };
		}

		public override List<ulong> GetClientCodes(MySqlConnection connection, ulong supplierId, string archFileName, string currentFileName)
		{
			var list = new List<ulong>();
			string SQL = GetFilterSQLHeader() + Environment.NewLine + SqlGetClientAddressId(true, false, true) + 
				Environment.NewLine + GetFilterSQLFooter();

			string FirmClientCode;
			try
			{
				FirmClientCode = Path.GetFileName(currentFileName).Split('_')[0];
			}
			catch(Exception ex)
			{
				throw new Exception("Не получилось сформировать SupplierDeliveryId(FirmClientCode2) из имени накладной.", ex);
			}

			var ds = MySqlHelper.ExecuteDataset(
				connection, 
				SQL, 
				new MySqlParameter("?SupplierId", supplierId), 
				new MySqlParameter("?SupplierDeliveryId", FirmClientCode));

			foreach (DataRow drApteka in ds.Tables[0].Rows)
				list.Add(Convert.ToUInt64(drApteka["AddressId"]));

			if (list.Count == 0)
				throw new Exception("Не удалось найти клиентов с SupplierClientId(FirmClientCode) = " + FirmClientCode + ".");

			return list;
		}
	}
}
