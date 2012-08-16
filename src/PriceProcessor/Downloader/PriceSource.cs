using System;
using System.Data;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor;
using MySql.Data.MySqlClient;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.Downloader
{
	public class PriceSource
	{
		public PriceSource()
		{
		}

		public PriceSource(DataRow row)
		{
			PriceItemId = Convert.ToUInt32(row[SourcesTableColumns.colPriceItemId]);
			PricePath = row[SourcesTableColumns.colPricePath].ToString().Trim();
			PriceMask = row[SourcesTableColumns.colPriceMask].ToString();

			HttpLogin = row[SourcesTableColumns.colHTTPLogin].ToString();
			HttpPassword = row[SourcesTableColumns.colHTTPPassword].ToString();

			FtpDir = row[SourcesTableColumns.colFTPDir].ToString();
			FtpLogin = row[SourcesTableColumns.colFTPLogin].ToString();
			FtpPassword = row[SourcesTableColumns.colFTPPassword].ToString();
			FtpPassiveMode = Convert.ToByte(row[SourcesTableColumns.colFTPPassiveMode]) == 1;

			FirmCode = row[SourcesTableColumns.colFirmCode];

			ArchivePassword = row["ArchivePassword"].ToString();

			SourceTypeId = Convert.ToUInt64(row["SourceTypeId"]);

			if (!(row["LastDownload"] is DBNull))
				PriceDateTime = Convert.ToDateTime(row["LastDownload"]);

			if (!(row["RequestInterval"] is DBNull))
				RequestInterval = Convert.ToInt32(row["RequestInterval"]);

			if (!(row["LastSuccessfulCheck"] is DBNull))
				LastSuccessfulCheck = Convert.ToDateTime(row["LastSuccessfulCheck"]);
		}

		public ulong SourceTypeId { get; set; }

		public uint PriceItemId { get; set; }
		public string PricePath { get; set; }
		public string PriceMask { get; set; }

		public string HttpLogin { get; set; }
		public string HttpPassword { get; set; }

		public string FtpDir { get; set; }
		public string FtpLogin { get; set; }
		public string FtpPassword { get; set; }
		public bool FtpPassiveMode { get; set; }

		public object FirmCode { get; set; }

		public DateTime PriceDateTime { get; set; }
		public DateTime LastSuccessfulCheck { get; set; }
		public string ArchivePassword { get; set; }

		public int RequestInterval { get; set; }

		public bool IsReadyForDownload()
		{
			var now = SystemTime.Now();
			if (now < LastSuccessfulCheck)
				return true;
			return (now - LastSuccessfulCheck).TotalSeconds >= RequestInterval;
		}

		public void UpdateLastCheck()
		{
			LastSuccessfulCheck = DateTime.Now;
			With.Connection(c => {
				MySqlHelper.ExecuteNonQuery(c, @"
update farm.Sources src
	join usersettings.PriceItems pim on src.Id = pim.SourceId
set src.LastSuccessfulCheck = ?LastSuccessfulCheck
where pim.Id = ?PriceItemId",
					new MySqlParameter("?PriceItemId", PriceItemId),
					new MySqlParameter("?LastSuccessfulCheck", LastSuccessfulCheck));
			});
		}
	}
}