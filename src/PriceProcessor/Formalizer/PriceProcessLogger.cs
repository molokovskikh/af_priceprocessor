using System;
using System.IO;
using Common.MySql;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.Formalizer
{
	public class PriceProcessLogger
	{
		private readonly string _prevErrorMessage = String.Empty;

		private readonly PriceProcessItem _processItem;

		private readonly ILog _logger = LogManager.GetLogger(typeof(PriceProcessThread));
		private DateTime begin;

		public string CurrentErrorMessage { get; private set; }
		public long FormSecs { get; set; }

		public PriceProcessLogger(string prevErrorMessage, PriceProcessItem priceProcessItem)
		{
			_prevErrorMessage = prevErrorMessage;
			_processItem = priceProcessItem;
			begin = DateTime.Now;
		}

		public void SuccesLog(IPriceFormalizer p, string filename)
		{
			FormSecs = Convert.ToInt64(DateTime.Now.Subtract(begin).TotalSeconds);
			string messageBody = "", messageSubject = "";
			//Формирование заголовков письма и
			SuccesGetBody("Прайс упешно формализован", ref messageSubject, ref messageBody, p?.Info);
			string downloadId = null;
			var fileName = Path.GetFileNameWithoutExtension(filename);
			if (fileName.IndexOf("_") > -1) {
				downloadId = fileName.Substring(fileName.IndexOf("_") + 1, fileName.Length - fileName.IndexOf("_") - 1);
				uint id;
				uint.TryParse(downloadId, out id);
				downloadId = id.ToString();
			}

			using(var session = SessionHelper.GetSessionFactory().OpenSession())
			using(var trx = session.BeginTransaction()) {
				p.Stat.Fix(downloadId, Settings.Default.MinRepeatTranCount);
				p.Stat.TotalSecs = (uint?)FormSecs;
				session.Save(p.Stat);
				trx.Commit();
			}

			if (_prevErrorMessage != String.Empty)
				Mailer.SendToWarningList(messageSubject, messageBody);
		}

		public void ErrodLog(IPriceFormalizer p, Exception ex)
		{
			FormSecs = Convert.ToInt64(DateTime.Now.Subtract(begin).TotalSeconds);
			string messageBody = "", messageSubject = "";
			if (ex is FormalizeException)
				CurrentErrorMessage = ex.Message;
			else
				CurrentErrorMessage = ex.ToString();
			var addition = CurrentErrorMessage;

			//Если предыдущее сообщение не отличается от текущего, то не логируем его
			if (_prevErrorMessage == CurrentErrorMessage)
				return;

			//Формирование заголовков письма и
			GetBody("Ошибка формализации", ref addition, ref messageSubject, ref messageBody, p?.Info);
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
			GetBody("Предупреждение", ref addition, ref messageSubject, ref messageBody, e.clientCode, e.clientName, e.priceCode, e.priceName);

			if (e is RollbackFormalizeException) {
				var re = (RollbackFormalizeException)e;
				messageBody = $@"{messageBody}

Формализованно : {re.FormCount}
Неформализованно : {re.UnformCount}
Нулевых : {re.ZeroCount}
Запрещенных : {re.ForbCount}";
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

		private void SuccesGetBody(string mSubjPref, ref string mSubj, ref string mBody, PriceFormalizationInfo info)
		{
			if (info == null) {
				mSubj = mSubjPref;
				mBody = $@"Файл         : {Path.GetFileName(_processItem.FilePath)}
Дата события : {DateTime.Now}";
			}
			else {
				var name = $"{info.FirmShortName} ({info.PriceName})";
				mSubj = $"{mSubjPref} {info.PriceCode}";
				mBody = $@"Код фирмы       : {info.FirmCode}
Код прайса      : {info.PriceCode}
Название прайса : {name}
Дата события    : {DateTime.Now}";
			}
		}

		private void GetBody(string mSubjPref, ref string add, ref string subj, ref string body, long clientCode, string clientName, long priceCode, string priceName)
		{
			if (clientCode == -1) {
				subj = mSubjPref;
				body = $@"Файл         : {Path.GetFileName(_processItem.FilePath)}
Дата события : {DateTime.Now}
Ошибка       : {add}";
				add = body;
			}
			else {
				var name = $"{clientName} ({priceName})";
				subj = $"{mSubjPref} {priceCode}";
				body = $@"Код фирмы       : {clientCode}
Код прайса      : {priceCode}
Название прайса : {name}
Дата события    : {DateTime.Now}
Ошибка          : {add}";
			}
		}


		private void GetBody(string mSubjPref, ref string add, ref string subj, ref string body, PriceFormalizationInfo info)
		{
			if (info == null) {
				subj = mSubjPref;
				body = $@"Файл         : {Path.GetFileName(_processItem.FilePath)}
Дата события : {DateTime.Now}
Ошибка       : {add}";
				add = body;
			}
			else {
				var name = $"{info.FirmShortName} ({info.PriceName})";
				subj = $"{mSubjPref} {info.PriceCode}";
				body = $@"Код фирмы       : {info.FirmCode}
Код прайса      : {info.PriceCode}
Название прайса : {name}
Дата события    : {DateTime.Now}
Ошибка          : {add}";
			}
		}

		private void LogToDb(Action<MySqlCommand> action)
		{
			try {
				using (var connection = new MySqlConnection(ConnectionHelper.GetConnectionString())) {
					connection.Open();
					var command = new MySqlCommand("", connection);
					action(command);
				}
			}
			catch (Exception e) {
				_logger.Error("Ошибка логирования в базу", e);
			}
		}
	}
}