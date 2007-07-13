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
		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			List<ulong> list = new List<ulong>();
			string SQL = GetFilterSQLHeader() + Environment.NewLine + "and i.FirmClientCode = ?FirmClientCode" + Environment.NewLine + GetFilterSQLFooter();

			string FirmClientCode;
			try
			{
				FirmClientCode = Path.GetFileName(CurrentFileName).Split('_')[0];
			}
			catch(Exception ex)
			{
				throw new Exception("Не получилось сформировать FirmClientCode из имени накладной.", ex);
			}

			DataSet ds = MySqlHelper.ExecuteDataset(
				Connection, 
				SQL, 
				new MySqlParameter("?FirmCode", FirmCode), 
				new MySqlParameter("?FirmClientCode", FirmClientCode));

			foreach (DataRow drApteka in ds.Tables[0].Rows)
				list.Add(Convert.ToUInt64(drApteka["ClientCode"]));

			if (list.Count == 0)
				throw new Exception("Не удалось найти клиентов с FirmClientCode = " + FirmClientCode + ".");

			return list;
		}
	}
}
