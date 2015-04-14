using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using Inforoom.PriceProcessor.Waybills.Parser;
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
		private static readonly ILog _log = LogManager.GetLogger(typeof(WaybillService));
		private static readonly ILog _infoLog = LogManager.GetLogger("InfoLog");

		public List<EMailSourceHandlerException> Exceptions = new List<EMailSourceHandlerException>();

		public uint[] ParseWaybill(uint[] ids)
		{
			try {
				using (var scope = new TransactionScope(OnDispose.Rollback)) {
					var result = ParseWaybills(DocumentReceiveLog.LoadByIds(ids), false)
						.Select(d => d.Id)
						.ToArray();

					scope.VoteCommit();
					return result;
				}
			}
			catch (Exception e) {
				_log.Error("Ошибка при разборе накладных", e);
			}
			return new uint[0];
		}

		public void Process(List<DocumentReceiveLog> logs)
		{
			try {
				using (var scope = new TransactionScope(OnDispose.Rollback)) {
					ParseWaybills(logs, true);

					scope.VoteCommit();
				}
			}
			catch (Exception e) {
				_log.Error("Ошибка при разборе накладных", e);
			}
		}

		private IEnumerable<Document> ParseWaybills(List<DocumentReceiveLog> logs, bool shouldCheckClientSettings)
		{
			if (shouldCheckClientSettings)
				logs = CheckDocs(logs);

			var rejects = logs.Where(l => l.DocumentType == DocType.Reject).ToArray();
			foreach (var reject in rejects) {
				try {
					SessionHelper.WithSession(s => ProcessReject(s, reject));
				}
				catch(Exception e) {
					_log.Error(String.Format("Не удалось разобрать отказ {0}", reject.FileName), e);
				}
			}

			var docsForParsing = MultifileDocument.Merge(logs);
			var docs = docsForParsing.Select(d => {
				try {
					return ProcessWaybill(d.DocumentLog, d.FileName);
				}
				catch (Exception e) {
					var filename = d.FileName;
					_log.Error(String.Format("Не удалось разобрать накладную {0}", filename), e);
					SaveWaybill(filename);
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
			return logs.Select(l => {
				try {
					SessionHelper.WithSession(s => l.Check(s));
					l.SaveAndFlush();
					l.CopyDocumentToClientDirectory();
					return l;
				}
				catch (EMailSourceHandlerException e) {
					_infoLog.Info(String.Format("Не удалось разобрать накладную {0}", l.FileName), e);
					Exceptions.Add(e);
					var rejectLog = new RejectWaybillLog(l);
					SessionHelper.WithSession(s => {
						s.Save(rejectLog);
						s.Flush();
					});
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
						log.Comment += string.Format("Разбор накладной не произведен по причине несоответствия маски файла ({0}) для Поставщика", waybillExcludeFile.Mask);
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

			var doc = new WaybillFormatDetector().DetectAndParse(filename, log);
			// для мульти файла, мы сохраняем в источнике все файлы,
			// а здесь, если нужна накладная в dbf формате, то сохраняем merge-файл в dbf формате.
			if (doc != null)
				Exporter.ConvertIfNeeded(doc, settings);

			return doc;
		}

		private static void ProcessReject(ISession session, DocumentReceiveLog log)
		{
			if (log.Supplier.RejectParser == "NadezhdaFarm") {
				var reject = RejectHeader.ReadReject(log, log.GetFileName());
				if (reject.Lines.Count > 0) {
					session.Save(reject);
				}
			}
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