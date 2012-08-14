using System;
using System.Data;
using Inforoom.Downloader;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Downloader
{
	public class DownloadLogEntity
	{
		private static readonly ILog _logger = LogManager.GetLogger(typeof(DownloadLogEntity));

		private static MySqlCommand CreateLogCommand()
		{
			var command = new MySqlCommand(@"
insert into logs.downlogs (LogTime, Host, PriceItemId, Addition, ShortErrorMessage, SourceTypeId, ResultCode, ArchFileName, ExtrFileName) 
VALUES (now(), ?Host, ?PriceItemId, ?Addition, ?ShortErrorMessage, ?SourceTypeId, ?ResultCode, ?ArchFileName, ?ExtrFileName); select last_insert_id()");
			command.Parameters.AddWithValue("?Host", Environment.MachineName);
			command.Parameters.Add("?PriceItemId", MySqlDbType.UInt64);
			command.Parameters.Add("?Addition", MySqlDbType.VarString);
			command.Parameters.Add("?ResultCode", MySqlDbType.Byte);
			command.Parameters.Add("?ArchFileName", MySqlDbType.VarString);
			command.Parameters.Add("?ExtrFileName", MySqlDbType.VarString);
			command.Parameters.Add("?ShortErrorMessage", MySqlDbType.VarString);
			command.Parameters.Add("?SourceTypeId", MySqlDbType.UInt64);
			return command;
		}

		public static void Log(ulong sourceTypeId, string addition)
		{
			Log(sourceTypeId, null, addition, null, DownPriceResultCode.ErrorDownload, null, null, null);
		}

		public static void Log(ulong sourceTypeId, ulong priceItemId, string addition)
		{
			Log(sourceTypeId, priceItemId, addition, null, DownPriceResultCode.ErrorDownload, null, null, null);
		}

		public static void Log(ulong sourceTypeId, ulong priceItemId, string addition, string shortErrorMessage)
		{
			Log(sourceTypeId, priceItemId, addition, shortErrorMessage, DownPriceResultCode.ErrorDownload, null, null, null);
		}

		public static UInt64 Log(ulong sourceTypeId, ulong? currPriceItemId, string addition, DownPriceResultCode resultCode, string archFileName, string extrFileName)
		{
			return Log(sourceTypeId, currPriceItemId, addition, null, resultCode, archFileName, extrFileName, null);
		}

		public static UInt64 Log(ulong sourceTypeId, ulong? currPriceItemId, string addition, string shortErrorMessage,
			DownPriceResultCode resultCode, string archFileName, string extrFileName, MySqlConnection connection)
		{
			if (!String.IsNullOrEmpty(addition))
				if (currPriceItemId.HasValue)
					using (NDC.Push("." + currPriceItemId))
						_logger.InfoFormat("Logging.Addition : {0}", addition);
				else
					_logger.InfoFormat("Logging.Addition : {0}", addition);
			var command = CreateLogCommand();
			command.Parameters["?PriceItemId"].Value = currPriceItemId;
			command.Parameters["?Addition"].Value = addition;
			command.Parameters["?ResultCode"].Value = Convert.ToByte(resultCode);
			command.Parameters["?ArchFileName"].Value = archFileName;
			command.Parameters["?ExtrFileName"].Value = extrFileName;
			command.Parameters["?ShortErrorMessage"].Value = String.IsNullOrEmpty(shortErrorMessage) ? Convert.DBNull : shortErrorMessage;
			command.Parameters["?SourceTypeId"].Value = sourceTypeId;

			bool NeedLogging = true;
			//Если это успешная загрузка, то сбрасываем все ошибки
			//Если это ошибка, то если дополнение в словаре и совпадает, то запрещаем логирование, в другом случае добавляем или обновляем
			ulong tmpCurrPriceItemId = (currPriceItemId.HasValue) ? currPriceItemId.Value : 0;
			if (resultCode == DownPriceResultCode.ErrorDownload) {
				if (ErrorPriceLogging.ErrorMessages.ContainsKey(tmpCurrPriceItemId)) {
					if (ErrorPriceLogging.ErrorMessages[tmpCurrPriceItemId] == addition)
						NeedLogging = false;
					else
						ErrorPriceLogging.ErrorMessages[tmpCurrPriceItemId] = addition;
				}
				else
					ErrorPriceLogging.ErrorMessages.Add(tmpCurrPriceItemId, addition);
			}
			else if (ErrorPriceLogging.ErrorMessages.ContainsKey(tmpCurrPriceItemId))
				ErrorPriceLogging.ErrorMessages.Remove(tmpCurrPriceItemId);

			if (NeedLogging) {
				var owneConnection = false;
				if (connection == null) {
					owneConnection = true;
					connection = new MySqlConnection(Literals.ConnectionString());
				}
				try {
					if (connection.State == ConnectionState.Closed)
						connection.Open();
					command.Connection = connection;
					return Convert.ToUInt64(command.ExecuteScalar());
				}
				finally {
					if (owneConnection)
						connection.Close();
				}
			}
			return 0;
		}
	}
}