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

		public string CurrentErrorMessage { get; private set; }
		public long FormSecs { get; set; }

		public PriceProcessLogger(string prevErrorMessage, PriceProcessItem priceProcessItem)
		{
			_prevErrorMessage = prevErrorMessage;
			_processItem = priceProcessItem;
		}

		public void SuccesLog(IPriceFormalizer p)
		{
			string messageBody = "", messageSubject = "";
			//Формирование заголовков письма и
			SuccesGetBody("Прайс упешно формализован", ref messageSubject, ref messageBody, p != null ? p.Info : null);
			string downloadId = null;
			var fileName = Path.GetFileNameWithoutExtension(p.InputFileName);
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
			GetBody("Ошибка формализации", ref addition, ref messageSubject, ref messageBody, p != null ? p.Info : null);
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

		private void SuccesGetBody(string mSubjPref, ref string mSubj, ref string mBody, PriceFormalizationInfo info)
		{
			if (info == null) {
				mSubj = mSubjPref;
				mBody = String.Format(
					@"Файл         : {0}
Дата события : {1}",
					Path.GetFileName(_processItem.FilePath),
					DateTime.Now);
			}
			else {
				var name = String.Format("{0} ({1})", info.FirmShortName, info.PriceName);
				mSubj = String.Format("{0} {1}", mSubjPref, info.PriceCode);
				mBody = String.Format(
					@"Код фирмы       : {0}
Код прайса      : {1}
Название прайса : {2}
Дата события    : {3}",
					info.FirmCode,
					info.PriceCode,
					name,
					DateTime.Now);
			}
		}

		private void GetBody(string mSubjPref, ref string add, ref string subj, ref string body, long clientCode, string clientName, long priceCode, string priceName)
		{
			if (clientCode == -1) {
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
			else {
				var name = String.Format("{0} ({1})", clientName, priceName);
				subj = String.Format("{0} {1}", mSubjPref, priceCode);
				body = String.Format(
					@"Код фирмы       : {0}
Код прайса      : {1}
Название прайса : {2}
Дата события    : {3}
Ошибка          : {4}",
					clientCode,
					priceCode,
					name,
					DateTime.Now,
					add);
			}
		}


		private void GetBody(string mSubjPref, ref string add, ref string subj, ref string body, PriceFormalizationInfo info)
		{
			if (info == null) {
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
			else {
				var name = String.Format("{0} ({1})", info.FirmShortName, info.PriceName);
				subj = String.Format("{0} {1}", mSubjPref, info.PriceCode);
				body = String.Format(
					@"Код фирмы       : {0}
Код прайса      : {1}
Название прайса : {2}
Дата события    : {3}
Ошибка          : {4}",
					info.FirmCode,
					info.PriceCode,
					name,
					DateTime.Now,
					add);
			}
		}

		private void LogToDb(Action<MySqlCommand> action)
		{
			try {
				using (var connection = new MySqlConnection(ConnectionHelper.DefaultConnectionStringName)) {
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