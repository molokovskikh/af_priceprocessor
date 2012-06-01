using System;
using System.IO;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer.New;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.Formalizer
{
	public class PriceProcessLogger
	{
		private readonly string _prevErrorMessage = String.Empty;
		
		private readonly PriceProcessItem _processItem;

		private readonly ILog _logger = LogManager.GetLogger(typeof(PriceProcessThread));
		private bool _letterAboutConnectionSended;

		public string CurrentErrorMessage { get; private set; }
		public long FormSecs { get; set; }

		public PriceProcessLogger(string prevErrorMessage, PriceProcessItem priceProcessItem)
		{
			_prevErrorMessage = prevErrorMessage;
			_processItem = priceProcessItem;
		}

		public void SendMySqlFailMessage(string info)
		{
			if (_letterAboutConnectionSended)
				return;

			Mailer.Send(Settings.Default.ServiceMail,
			            Settings.Default.ServiceMail,
			            "!!! Необходимо перезапустить PriceProcessor",
			            String.Format(@"
Необходимо перезапустить PriceProcessor, т.к. в нитке формализации был получен connection, который не возвращает записей при выполнении команд.
Техническая информация:
{0}", info));

			_letterAboutConnectionSended = true;
		}

		public void SuccesLog(IPriceFormalizer p)
		{
			string messageBody = "", messageSubject = "";
			//Формирование заголовков письма и 
			if (null == p)
				SuccesGetBody("Прайс упешно формализован", ref messageSubject, ref messageBody, -1, -1, null);
			else
				SuccesGetBody("Прайс упешно формализован", ref messageSubject, ref messageBody, p.priceCode, p.firmCode, String.Format("{0} ({1})", p.firmShortName, p.priceName));

			string downloadId = null;
			var fileName = Path.GetFileNameWithoutExtension(p.InputFileName);
			if (fileName.IndexOf("_") > -1)
			{
				downloadId = fileName.Substring(fileName.IndexOf("_") + 1, fileName.Length - fileName.IndexOf("_") - 1);
				uint id;
				uint.TryParse(downloadId, out id);
				downloadId = id.ToString();
			}

			LogToDb(command => {
				command.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceItemId, Form, Unform, Zero, Forb, ResultId, TotalSecs, DownloadId) VALUES (NOW(), ?Host, ?PriceItemId, ?Form, ?Unform, ?Zero, ?Forb, ?ResultId, ?TotalSecs, ?DownloadId)";
				command.Parameters.Clear();
				command.Parameters.AddWithValue("?Host", Environment.MachineName);
				command.Parameters.AddWithValue("?PriceItemId", _processItem.PriceItemId);
				command.Parameters.AddWithValue("?Form", p.formCount);
				command.Parameters.AddWithValue("?Unform", p.unformCount);
				command.Parameters.AddWithValue("?Zero", p.zeroCount);
				command.Parameters.AddWithValue("?Forb", p.forbCount);
				command.Parameters.AddWithValue("?ResultId", (p.maxLockCount <= Settings.Default.MinRepeatTranCount) ? FormResults.OK : FormResults.Warrning);
				command.Parameters.AddWithValue("?TotalSecs", FormSecs);
				command.Parameters.AddWithValue("?DownloadId", downloadId);
				command.ExecuteNonQuery();
			});

			if (_prevErrorMessage != String.Empty)
				Mailer.SendToWarningList(messageSubject, messageBody);
		}

		public void ErrodLog(IPriceFormalizer p, Exception ex)
		{
			string messageBody = "", messageSubject = "";
			if (ex is FormalizeException)
				CurrentErrorMessage = ex.Message;
			else
				CurrentErrorMessage = ex.ToString();
			var addition = CurrentErrorMessage;
			_logger.InfoFormat("Error Addition : {0}", addition);

			//Если предыдущее сообщение не отличается от текущего, то не логируем его
			if (_prevErrorMessage == CurrentErrorMessage)
				return;

			//Формирование заголовков письма и 
			if (null != p)
				GetBody("Ошибка формализации", ref addition, ref messageSubject, ref messageBody, p.priceCode, p.firmCode, String.Format("{0} ({1})", p.firmShortName, p.priceName));
			else if (ex is FormalizeException)
				GetBody("Ошибка формализации", ref addition, ref messageSubject, ref messageBody, ((FormalizeException)ex).priceCode, ((FormalizeException)ex).clientCode, ((FormalizeException)ex).FullName);
			else
				GetBody("Ошибка формализации", ref addition, ref messageSubject, ref messageBody, Convert.ToInt64(_processItem.PriceCode), -1, null);

			LogToDb(command => {
				command.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceItemId, Addition, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceItemId, ?Addition, ?ResultId, ?TotalSecs)";
				command.Parameters.Clear();
				command.Parameters.AddWithValue("?PriceItemId", _processItem.PriceItemId);
				command.Parameters.AddWithValue("?Host", Environment.MachineName);
				command.Parameters.AddWithValue("?Addition", addition);
				command.Parameters.AddWithValue("?ResultId", FormResults.Error);
				command.Parameters.AddWithValue("?TotalSecs", FormSecs);
				command.ExecuteNonQuery();
			});
			Mailer.SendToWarningList(messageSubject, messageBody);
		}

		public void ErrodLog(PriceFormalizationInfo priceInfo, Exception ex)
		{
			string messageBody = "", messageSubject = "";
			if (ex is FormalizeException)
				CurrentErrorMessage = ex.Message;
			else
				CurrentErrorMessage = ex.ToString();
			var addition = CurrentErrorMessage;
			_logger.InfoFormat("Error Addition : {0}", addition);

			//Если предыдущее сообщение не отличается от текущего, то не логируем его
			if (_prevErrorMessage == CurrentErrorMessage)
				return;

			//Формирование заголовков письма и 
			if (null != priceInfo)
				GetBody("Ошибка формализации", ref addition, ref messageSubject, ref messageBody, priceInfo.PriceCode, priceInfo.FirmCode, String.Format("{0} ({1})", priceInfo.FirmShortName, priceInfo.PriceName));
			else if (ex is FormalizeException)
				GetBody("Ошибка формализации", ref addition, ref messageSubject, ref messageBody, ((FormalizeException)ex).priceCode, ((FormalizeException)ex).clientCode, ((FormalizeException)ex).FullName);
			else
				GetBody("Ошибка формализации", ref addition, ref messageSubject, ref messageBody, Convert.ToInt64(_processItem.PriceCode), -1, null);

			LogToDb(command => {
				command.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceItemId, Addition, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceItemId, ?Addition, ?ResultId, ?TotalSecs)";
				command.Parameters.Clear();
				command.Parameters.AddWithValue("?PriceItemId", _processItem.PriceItemId);
				command.Parameters.AddWithValue("?Host", Environment.MachineName);
				command.Parameters.AddWithValue("?Addition", addition);
				command.Parameters.AddWithValue("?ResultId", FormResults.Error);
				command.Parameters.AddWithValue("?TotalSecs", FormSecs);
				command.ExecuteNonQuery();
			});
			Mailer.SendToWarningList(messageSubject, messageBody);
		}

		public void WarningLog(FormalizeException e, string addition)
		{
			string messageBody = "", messageSubject = "";
			CurrentErrorMessage = addition;
			_logger.InfoFormat("Warning Addition : {0}", addition);

			if (_prevErrorMessage == CurrentErrorMessage) 
				return;

			//Формирование заголовков письма и 
			GetBody("Предупреждение", ref addition, ref messageSubject, ref messageBody, e.priceCode, e.clientCode, e.FullName);

			if (e is RollbackFormalizeException)
			{
				var re = (RollbackFormalizeException)e;
				messageBody = String.Format(
					@"{0}

Формализованно : {1}
Неформализованно : {2}
Нулевых : {3}
Запрещенных : {4}",
					messageBody,
					re.FormCount,
					re.UnformCount,
					re.ZeroCount,
					re.ForbCount);
			}

			LogToDb(command => {
				if (-1 == e.priceCode) {
					command.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceItemId, Addition, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceItemId, ?Addition, ?ResultId, ?TotalSecs)";
					command.Parameters.Clear();
				}
				else {
					command.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceItemId, Addition,Form, Unform, Zero, Forb, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceItemId, ?Addition, ?Form, ?Unform, ?Zero, ?Forb, ?ResultId, ?TotalSecs)";
					command.Parameters.Clear();
					if (e is RollbackFormalizeException) {
						command.Parameters.AddWithValue("?Form", ((RollbackFormalizeException)e).FormCount);
						command.Parameters.AddWithValue("?Unform", ((RollbackFormalizeException)e).UnformCount);
						command.Parameters.AddWithValue("?Zero", ((RollbackFormalizeException)e).ZeroCount);
						command.Parameters.AddWithValue("?Forb", ((RollbackFormalizeException)e).ForbCount);
					}
					else {
						command.Parameters.AddWithValue("?Form", DBNull.Value);
						command.Parameters.AddWithValue("?Unform", DBNull.Value);
						command.Parameters.AddWithValue("?Zero", DBNull.Value);
						command.Parameters.AddWithValue("?Forb", DBNull.Value);
					}
				}
				command.Parameters.AddWithValue("?Host", Environment.MachineName);
				command.Parameters.AddWithValue("?PriceItemId", _processItem.PriceItemId);
				command.Parameters.AddWithValue("?Addition", addition);
				command.Parameters.AddWithValue("?ResultId", FormResults.Error);
				command.Parameters.AddWithValue("?TotalSecs", FormSecs);
				command.ExecuteNonQuery();
			});


			Mailer.SendToWarningList(messageSubject, messageBody);
		}

		private void SuccesGetBody(string mSubjPref, ref string mSubj, ref string mBody, long priceCode, long clientCode, string priceName)
		{
			if (-1 == priceCode)
			{
				mSubj = mSubjPref;
				mBody = String.Format(
					@"Файл         : {0}
Дата события : {1}",
					Path.GetFileName(_processItem.FilePath),
					DateTime.Now);
			}
			else
			{
				mSubj = String.Format("{0} {1}", mSubjPref, priceCode);
				mBody = String.Format(
					@"Код фирмы       : {0}
Код прайса      : {1}
Название прайса : {2}
Дата события    : {3}",
					clientCode,
					priceCode,
					priceName,
					DateTime.Now);
			}
		}

		private void GetBody(string mSubjPref, ref string add, ref string subj, ref string body, long priceCode, long clientCode, string priceName)
		{
			if (-1 == priceCode)
			{
				subj = mSubjPref;
				body = String.Format(
					@"Файл         : {0}
Дата события : {1}
Ошибка       : {2}",
					Path.GetFileName(_processItem.FilePath),
					DateTime.Now,
					add);
				add = body;
			}
			else
			{
				subj = String.Format("{0} {1}", mSubjPref, priceCode);
				body = String.Format(
					@"Код фирмы       : {0}
Код прайса      : {1}
Название прайса : {2}
Дата события    : {3}
Ошибка          : {4}",
					clientCode,
					priceCode,
					priceName,
					DateTime.Now,
					add);
			}
		}

		private void LogToDb(Action<MySqlCommand> action)
		{
			try
			{
				using (var connection = new MySqlConnection(Literals.ConnectionString()))
				{
					connection.Open();
					var command = new MySqlCommand("", connection);
					action(command);
				}
			}
			catch (Exception e)
			{
				_logger.Error("Ошибка логирования в базу", e);
			}
		}
	}
}