using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using Inforoom.PriceProcessor.Waybills.Rejects;
using log4net;
using NHibernate;

namespace Inforoom.PriceProcessor.Waybills
{
	[ServiceContract]
	public interface IWaybillService
	{
		[OperationContract]
		uint[] ParseWaybill(uint[] uints);
	}

	[ServiceBehavior(IncludeExceptionDetailInFaults = true,
		InstanceContextMode = InstanceContextMode.PerCall)]
	public class WaybillService : IWaybillService
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (WaybillService));
		private static readonly ILog _infoLog = LogManager.GetLogger("InfoLog");

		public List<EMailSourceHandlerException> Exceptions = new List<EMailSourceHandlerException>();

		public uint[] ParseWaybill(uint[] ids)
		{
			try {
				using (var scope = new TransactionScope(OnDispose.Rollback)) {
					var result = ParseWaybills(DocumentReceiveLog.LoadByIds(ids))
						.Select(d => d.Id)
						.ToArray();

					scope.VoteCommit();
					return result;
				}
			} catch (Exception e) {
				_log.Error(string.Format("Ошибка при разборе накладных {0}", ids.Implode()), e);
			}
			return new uint[0];
		}

		public void Process(List<DocumentReceiveLog> logs)
		{
			try {
				//проверка документов должна производиться в отдельной транзакции тк если разбор
				//сломается логи должны быть записаны в базу
				using (var scope = new TransactionScope(OnDispose.Rollback)) {
					logs = CheckDocs(logs);

					scope.VoteCommit();
				}

				using (var scope = new TransactionScope(OnDispose.Rollback)) {
					ParseWaybills(logs);

					scope.VoteCommit();
				}
			} catch (Exception e) {
				_log.Error(string.Format("Ошибка при разборе накладных {0}", logs.Implode(x => x.Id)), e);
			}
		}

		private IEnumerable<Document> ParseWaybills(List<DocumentReceiveLog> logs)
		{
			var rejects = logs.Where(l => l.DocumentType == DocType.Reject).ToArray();
			foreach (var reject in rejects) {
				try {
					SessionHelper.WithSession(s => ProcessReject(s, reject));
				} catch (Exception e) {
					_log.Error(string.Format("Не удалось разобрать отказ {0}", reject.FileName), e);
				}
			}

			var docsForParsing = MultifileDocument.Merge(logs);
			var metaForRedmineErrorIssueList = new List<MetadataOfLog>();
			var docs = docsForParsing.Select(d => {
				try {
					var docToReturn = ProcessWaybill(d.DocumentLog, d.FileName);
					//если не получилось распарсить документ
					if (docToReturn == null && new FileInfo(d.FileName).Extension.ToLower() == ".dbf") {
						//создаем задачу на Redmine, прикрепляя файлы
						Redmine.CreateIssueForLog(ref metaForRedmineErrorIssueList, d.FileName, d.DocumentLog);
					}
					return docToReturn;
				} catch (Exception e) {
					var filename = d.FileName;
					var errorTitle = string.Format("Не удалось разобрать накладную {0}", filename);
					_log.Error(errorTitle, e);
					SaveWaybill(filename);

					if (new FileInfo(d.FileName).Extension.ToLower() == ".dbf")
						//создаем задачу на Redmine, прикрепляя файлы
						Redmine.CreateIssueForLog(ref metaForRedmineErrorIssueList, d.FileName, d.DocumentLog);
					return null;
				}
			}).Where(d => d != null).ToList();
			MultifileDocument.DeleteMergedFiles(docsForParsing);

			docs.Each(d => {
				if (d.Log.IsFake)
					d.Log.Save();
				d.Save();
				d.CreateCertificateTasks();
			});
			return docs;
		}

		private List<DocumentReceiveLog> CheckDocs(List<DocumentReceiveLog> logs)
		{
			var metaForRedmineErrorIssueList = new List<MetadataOfLog>();
			return logs.Select(l => {
				try {
					SessionHelper.WithSession(s => l.Check(s));
					l.SaveAndFlush();
					l.CopyDocumentToClientDirectory();
					return l;
				} catch (EMailSourceHandlerException e) {
					var errorTitle = string.Format("Не удалось разобрать накладную {0}", l.FileName);
					_infoLog.Info(errorTitle, e);
					Exceptions.Add(e);
					var rejectLog = new RejectWaybillLog(l);
					SessionHelper.WithSession(s => {
						s.Save(rejectLog);
						s.Flush();
					});

					if(new FileInfo(l.FileName).Extension.ToLower() == ".dbf")
						//создаем задачу на Redmine, прикрепляя файлы
						Redmine.CreateIssueForLog(ref metaForRedmineErrorIssueList, l.FileName, l);

					return null;
				}
			}).Where(l => l != null).ToList();
		}

		public static bool FitsMask(string fileName, string fileMask)
		{
			if (fileMask.LastIndexOf('*') != fileMask.Length)
				fileMask += "$";
			var mask = new Regex(fileMask.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."));
			return mask.IsMatch(fileName);
		}

		private static bool WaybillExcludeFile(DocumentReceiveLog log)
		{
			return SessionHelper.WithSession(s => {
				var supplier = s.Get<Supplier>(log.Supplier.Id);
				foreach (var waybillExcludeFile in supplier.ExcludeFiles) {
					if (FitsMask(log.FileName, waybillExcludeFile.Mask)) {
						log.Comment += string.IsNullOrEmpty(log.Comment) ? string.Empty : Environment.NewLine;
						log.Comment +=
							string.Format("Разбор накладной не произведен по причине несоответствия маски файла ({0}) для Поставщика",
								waybillExcludeFile.Mask);
						s.Save(new WaybillDirtyFile(log.Supplier, log.FileName, waybillExcludeFile.Mask));
						return true;
					}
				}
				return false;
			});
		}

		private static Document ProcessWaybill(DocumentReceiveLog log, string filename)
		{
			if (log.DocumentType == DocType.Reject)
				return null;

			var settings = WaybillSettings.Find(log.ClientCode.Value);
			if (WaybillExcludeFile(log))
				return null;

			if (log.DocumentSize == 0)
				return null;

			// ждем пока файл появится в удаленной директории
			if (!log.FileIsLocal())
				ShareFileHelper.WaitFile(filename, 5000);

			var doc = SessionHelper.WithSession(s => new WaybillFormatDetector().DetectAndParse(s, filename, log));
			// для мульти файла, мы сохраняем в источнике все файлы,
			// а здесь, если нужна накладная в dbf формате, то сохраняем merge-файл в dbf формате.
			if (doc != null)
				Exporter.ConvertIfNeeded(doc, settings);

			return doc;
		}

		/// <summary>
		///   Создание отказов для логов.
		/// </summary>
		/// <param name="session">Сессия Nhibernate</param>
		/// <param name="log">Лог, о получении документа</param>
		private static void ProcessReject(ISession session, DocumentReceiveLog log)
		{
			var parser = GetRejectParser(log);
			if (parser == null)
				return;
			var reject = parser.CreateReject(log);
			if (reject.Lines.Count > 0) {
				try {
					reject.Normalize(session);
				} catch (Exception e) {
					_log.Error(string.Format("Не удалось идентифицировать товары отказа {0}", log.GetFileName()), e);
				}
				session.Save(reject);
			}
		}

		/// <summary>
		///   Находит парсер отказов для отказа
		/// </summary>
		/// <param name="log">Лог, о получении документа</param>
		/// <returns>Парсер для отказа или null</returns>
		private static RejectParser GetRejectParser(DocumentReceiveLog log)
		{
			var parsername = log.Supplier.RejectParser;
			if (string.IsNullOrEmpty(parsername))
				return null;
			var assembly = Assembly.GetAssembly(typeof (DocumentReceiveLog));
			var parser = assembly.GetTypes().FirstOrDefault(i => i.Name == parsername);

			if (parser == null)
				throw new Exception(string.Format("Парсер {0} не был найден в сборке {1}", parsername, assembly.FullName));

			var obj = Activator.CreateInstance(parser);
			var rjparser = obj as RejectParser;
			if (rjparser == null)
				throw new Exception(
					string.Format("Не удалось привести тип. Скорее всего {0} не является наследником класса RejectParser", parsername));

			return rjparser;
		}

		public static void SaveWaybill(string filename)
		{
			if (!Directory.Exists(Settings.Default.DownWaybillsPath))
				Directory.CreateDirectory(Settings.Default.DownWaybillsPath);

			if (File.Exists(filename))
				File.Copy(filename, Path.Combine(Settings.Default.DownWaybillsPath, Path.GetFileName(filename)), true);
		}
	}
}