using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Queries;
using MySql.Data.MySqlClient;
using System.IO;
using System.Data;

namespace Inforoom.Downloader.DocumentReaders
{
	public class ProtekOmsk_3777_Reader : BaseDocumentReader
	{
		public override List<ulong> ParseAddressIds(MySqlConnection connection, ulong supplierId, string archFileName, string currentFileName)
		{
			string firmClientCode;
			try {
				firmClientCode = Path.GetFileName(currentFileName).Split('_')[0];
			}
			catch (Exception ex) {
				throw new Exception("Не получилось сформировать SupplierDeliveryId(FirmClientCode2) из имени накладной.", ex);
			}

			var list = AddressIdQuery.GetAddressIds(supplierId, firmClientCode);
			if (list.Count == 0)
				throw new Exception("Не удалось найти клиентов с SupplierClientId(FirmClientCode) = " + firmClientCode + ".");

			return list;
		}
	}
}