using System.Collections.Generic;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using LumiSoft.Net.Log;
using NHibernate.Mapping;

namespace Inforoom.PriceProcessor.Waybills.Rejects
{
	/// <summary>
	/// Базовый класс для всех парсеров отказов
	/// </summary>
	public abstract class RejectParser
	{
		/// <summary>
		/// Журнал для ошибок
		/// </summary>
		private ILog Logger;

		/// <summary>
		/// Неправильные строки, обнаруженные при проверке документа
		/// </summary>
		public List<RejectLine> BadLines;

		public RejectParser()
		{
			Logger = LogManager.GetLogger(GetType());
		}

		/// <summary>
		/// Создание отказа из лога приемки документа
		/// </summary>
		/// <param name="log">Объект лога документа</param>
		/// <returns></returns>
		public RejectHeader CreateReject(DocumentReceiveLog log)
		{
			BadLines = new List<RejectLine>();
			var rejectheader = new RejectHeader(log);
			var filename = log.GetFileName();
			Parse(rejectheader, filename);
			CheckRejectHeader(rejectheader);
			return rejectheader;
		}

		/// <summary>
		/// Проверяет отказ и удаляет невалидные строки
		/// </summary>
		/// <param name="rejectheader">Заголовок отказа</param>
		private void CheckRejectHeader(RejectHeader rejectheader)
		{
			var skipLines = new List<RejectLine>();
			foreach (var line in rejectheader.Lines) {
				if (line.Rejected == 0) {
					Logger.WarnFormat("Не удалось получить значение количества отказов по продукту '{0}' для лога документа {1}", line.Product, rejectheader.Log.Id);
					skipLines.Add(line);
					continue;
				}
				if (string.IsNullOrEmpty(line.Product))
					skipLines.Add(line);
			}
			foreach (var line in skipLines) {
				rejectheader.Lines.Remove(line);
				BadLines.Add(line);
			}
		}

		/// <summary>
		/// Функция разбора отказов. Заполняет заголовок отказа строками по продуктам.
		/// </summary>
		/// <param name="rejectHeader">Заголовок отказа</param>
		/// <param name="filename">Путь к файлу</param>
		public abstract void Parse(RejectHeader rejectHeader, string filename);
	}
}
