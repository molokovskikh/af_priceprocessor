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
			var list = new List<ulong>();
			string SQL = GetFilterSQLHeader() + Environment.NewLine +
				" and (i.FirmClientCode2 = ?SupplierDeliveryId) " +
				SqlGetClientAddressId(true, false, true) +
				Environment.NewLine + GetFilterSQLFooter();

			string supplierDeliveryId;
			try
			{
				supplierDeliveryId = Path.GetFileName(currentFileName).Split('_')[0];
			}
			catch (Exception ex)
			{
				throw new Exception("Не получилось сформировать код доставки из имени файла накладной.", ex);
			}

			var ds = MySqlHelper.ExecuteDataset(
				connection,
				SQL,
				new MySqlParameter("?SupplierId", supplierId),
				new MySqlParameter("?SupplierDeliveryId", supplierDeliveryId));

			foreach (DataRow drApteka in ds.Tables[0].Rows)
				list.Add(Convert.ToUInt64(drApteka["AddressId"]));

			return list;
		}
	}
}
