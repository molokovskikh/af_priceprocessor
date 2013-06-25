using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Helpers;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor
{
	public class PriceProcessItem
	{
		//Скачан ли прайс-лист или переподложен
		public bool Downloaded { get; private set; }

		//путь к прайс-листу: либо папа Inbound на fms или временная папка для скаченных прайсов
		public string FilePath { get; set; }

		//код прайс-листа
		public ulong PriceCode { get; private set; }

		//код цены, может быть null
		public ulong? CostCode { get; private set; }

		//Id из таблицы PriceItems
		public ulong PriceItemId { get; private set; }

		//Дата файла, взятая из сети, ftp или http, чтобы определить: необходимо ли скачивать еще раз данный прайс?
		public DateTime? FileTime { get; set; }

		//Дата создания элемента, чтобы знать: можно ли его брать в обработку или нет
		public DateTime CreateTime { get; /* для теста private */ set; }

		// Время последней загрузки прайса
		public DateTime LocalLastDownload { get; set; }

		private ulong? ParentSynonym { get; set; }

		public PriceProcessItem(bool downloaded, ulong priceCode, ulong? costCode, ulong priceItemId, string filePath, ulong? parentSynonym)
		{
			Downloaded = downloaded;
			PriceCode = priceCode;
			CostCode = costCode;
			PriceItemId = priceItemId;
			FilePath = filePath;
			CreateTime = DateTime.UtcNow;
			ParentSynonym = parentSynonym;
			if (downloaded)
				FileTime = DateTime.Now;
			LocalLastDownload = DateTime.Now;
		}

		public static PriceProcessItem TryToLoadPriceProcessItem(string filename)
		{
			if (String.IsNullOrEmpty(filename))
				return null;

			uint priceItemId = ParseId(filename);
			var isDownloaded = IsDownloadedPrice(filename);

			if (priceItemId == 0)
				return null;

			var drPriceItem = MySqlHelper.ExecuteDataRow(
				Literals.ConnectionString(),
				@"select distinct
  pc.PriceCode as PriceCode,
  if(pd.CostType = 1, pc.CostCode, null) CostCode,
  pc.PriceItemId,
  pd.ParentSynonym,
  pi.LastDownload
from (usersettings.pricescosts pc, usersettings.pricesdata pd)
	join usersettings.priceitems pi on pc.PriceItemId = pi.Id
where pc.PriceItemId = ?PriceItemId
	  and ((pd.CostType = 1) or (exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pd.PriceCode and prd.BaseCost=pc.CostCode)))
	  and pd.PriceCode = pc.PriceCode
group by pi.Id",
				new MySqlParameter("?PriceItemId", priceItemId));
			if (drPriceItem == null)
				return null;

			var priceCode = Convert.ToUInt64(drPriceItem["PriceCode"]);
			var costCode = (drPriceItem["CostCode"] is DBNull) ? null : (ulong?)Convert.ToUInt64(drPriceItem["CostCode"]);
			var parentSynonym = (drPriceItem["ParentSynonym"] is DBNull) ? null : (ulong?)Convert.ToUInt64(drPriceItem["ParentSynonym"]);
			var lastDownload = (drPriceItem["LastDownload"] is DBNull) ? DateTime.MinValue : Convert.ToDateTime(drPriceItem["LastDownload"]);
			var item = new PriceProcessItem(isDownloaded, priceCode, costCode, priceItemId, filename, parentSynonym);

			if (isDownloaded)
				item.FileTime = lastDownload;

			return item;
		}

		public static uint ParseId(string filename)
		{
			var name = Path.GetFileNameWithoutExtension(filename);

			if (String.IsNullOrEmpty(name))
				return 0;

			var matches = new[] {
				Regex.Match(name, @"^(\d+)$"),
				Regex.Match(name, @"^d(\d+)_")
			};

			var match = matches.FirstOrDefault(m => m.Success);
			if (match != null) {
				return SafeConvert.ToUInt32(match.Groups[1].Value);
			}
			return 0;
		}

		public static PriceProcessItem GetProcessItem(uint priceItemId)
		{
			var dtRules = PricesValidator.LoadFormRules(priceItemId);
			if (dtRules.Rows.Count == 0)
				return null;
			var mask = priceItemId + dtRules.Rows[0]["FileExtention"].ToString();
			var files = Directory.GetFiles(Settings.Default.BasePath, mask);
			return TryToLoadPriceProcessItem(files.FirstOrDefault());
		}

		public IList<string> GetAllNames()
		{
			IList<string> names;
			var tempPath = Path.GetTempPath() + (int)DateTime.Now.TimeOfDay.TotalMilliseconds + "_" +
				PriceItemId.ToString() + "\\";
			var tempFileName = tempPath + PriceItemId + Path.GetExtension(FilePath);

			if (Directory.Exists(tempPath))
				global::Common.Tools.FileHelper.DeleteDir(tempPath);
			Directory.CreateDirectory(tempPath);
			try {
				try {
					var workPrice = PricesValidator.Validate(FilePath, tempFileName, (uint)PriceItemId);
					workPrice.Downloaded = Downloaded;
					workPrice.InputFileName = FilePath;
					names = workPrice.GetAllNames();
				}
				catch (WarningFormalizeException e) {
					return null;
				}
			}
			finally {
				Directory.Delete(tempPath, true);
			}

			return names;
		}

		public bool IsReadyForProcessing(IEnumerable<PriceProcessThread> processList)
		{
			//Если разница между временем создания элемента в PriceItemList и текущим временем больше 5 секунд, то берем файл в обработку
			var isSeasoned = DateTime.UtcNow.Subtract(CreateTime).TotalSeconds > 5;
			if (!isSeasoned)
				return false;

			if (!processList.Any())
				return true;

			//не запущен ли он уже в работу?
			var isProcessing = processList.Any(thread => thread.ProcessItem == this);
			if (isProcessing)
				return false;

			return true;
		}

		public void CopyToInbound(string extrFileName, MySqlConnection connection, MySqlTransaction transaction)
		{
			var command = new MySqlCommand(@"
update usersettings.PriceItems
set LastDownload = ?FileTime, LocalLastDownload=?LocalLastDownload
where Id = ?Id",
				connection, transaction);
			command.Parameters.AddWithValue("?Id", PriceItemId);
			command.Parameters.AddWithValue("?FileTime", FileTime);
			command.Parameters.AddWithValue("?LocalLastDownload", LocalLastDownload);
			command.ExecuteNonQuery();

			if (File.Exists(FilePath))
				File.Delete(FilePath);
			File.Copy(extrFileName, FilePath);
			PriceItemList.AddItem(this);
		}

		public static bool IsDownloadedPrice(string priceFile)
		{
			return Path.GetFileName(priceFile).StartsWith("d", StringComparison.OrdinalIgnoreCase);
		}
	}
}