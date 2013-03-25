using System;
using System.Collections.Generic;
using System.IO;
using Inforoom.Downloader.DocumentReaders;
using Inforoom.PriceProcessor.Queries;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Downloader.DocumentReaders
{
	public class SupplierFtpReader : BaseDocumentReader
	{
		public SupplierFtpReader()
		{
			excludeExtentions = new string[] { };
		}

		public override List<ulong> ParseAddressIds(MySqlConnection connection, ulong supplierId, string archFileName, string currentFileName)
		{
			string supplierDeliveryId;
			try {
				supplierDeliveryId = Path.GetFileName(currentFileName).Split('_')[0];
			}
			catch (Exception ex) {
				throw new Exception("Не получилось сформировать код доставки из имени файла накладной.", ex);
			}

			return AddressIdQuery.GetAddressIds(supplierId, supplierDeliveryId);
		}
	}
}